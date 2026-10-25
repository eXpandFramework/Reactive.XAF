/**
 * reactive-xaf-build/run — the build run: its lifecycle and its watch.
 *
 * Lifecycle: a per-run temp dir (`runPaths`) holding a supervisor script that
 * runs the build in a NESTED pwsh, so an `exit` inside the build cannot skip
 * the exit-code write, and drops that code into the run's transient marker.
 * The env the script starts with is the caller's: this module owns no build
 * policy, the profile does. Those artifacts live in `run-script.ts` (the
 * 400-line cap) and are re-exported below; this file owns the run's state and
 * its watch.
 *
 * Watch: /devexpress hands the pane the supervisor line and returns; this
 * module then watches the run out of band, reading its signals in this order:
 *   1. the exit marker (consume-on-read, the primary signal),
 *   2. pane death (the pane is gone and no marker arrived),
 *   3. a stall backstop: no new pane output AND no CPU progress,
 *   4. an overrun report past the expected build duration.
 * The watch only reports. Nothing here kills a build: the user's abort closes
 * the pane, nothing else does.
 *
 * State lives on globalThis (duplicate-instance safe, like the AzDO watcher);
 * every side effect goes through an injectable seam, so the test suite never
 * touches psmux or a real build.
 */

import * as fs from "node:fs";
import type { CpuSampler, PaneCapturer, PaneCloser, PaneProber } from "./pane.js";

export interface RunCadence {
  /** how often a tick runs (the marker check and the cadence gate) */
  signalMs: number;
  /** pane liveness plus output capture */
  probeMs: number;
  /** CPU sampling of the build host */
  cpuMs: number;
  /** no output and no CPU progress for this long → stall report */
  stallMs: number;
  /** running longer than this → overrun report */
  overrunMs: number;
}

/** A local Lab build skips the tests the pipeline runs, so it is far shorter
 *  than the ~39-minute AzDO figure. 10 minutes of complete silence and 20
 *  minutes of runtime are the two report thresholds. */
export const RUN_CADENCE: RunCadence = {
  signalMs: 2000,
  probeMs: 10_000,
  cpuMs: 30_000,
  stallMs: 600_000,
  overrunMs: 1_200_000,
};

/** A run id, its temp dir, the supervisor script and the prune live in
 *  run-script.ts (the 400-line cap): re-exported below so every consumer keeps
 *  importing the run from this one module. */
export {
  newRunId, runPaths, supervisorScript, trackedWrite, writeRunScript, supervisorCommand, pruneRunDirs,
} from "./run-script.js";
export type { RunPaths } from "./run-script.js";

/** The supervisor script itself: `run-script.ts`'s `supervisorScript`, which
 *  runs the build in a nested pwsh and reports the code both as a marker file
 *  and as a line echoed into the pane. */

export type RunEvent =
  | { kind: "done"; code: number; tail: string; note?: string }
  | { kind: "died"; tail: string }
  | { kind: "stall"; silentMs: number; limitMs: number }
  | { kind: "overrun"; elapsedMs: number; limitMs: number };

export type RunReporter = (event: RunEvent) => void | Promise<void>;

export interface BuildRunSeams {
  probe?: PaneProber;
  capture?: PaneCapturer;
  close?: PaneCloser;
  sampleCpu?: CpuSampler;
  cadence?: Partial<RunCadence>;
}

export interface BuildRunHandle {
  runId: string;
  pane: string | null;
  marker: string;
  startedAt: number;
}

const RUN_KEY = Symbol.for("reactive-xaf-build.run");

interface RunState {
  handle: BuildRunHandle;
  timer: ReturnType<typeof setInterval> | null;
  lastCapture: string;
  lastOutputAt: number;
  lastCpu: number | null;
  lastCpuAt: number;
  panePid: number | null;
  /** The last real answer to "is the pane there?" — null until one arrives, so
   *  a missing answer never reads as a dead pane. */
  paneAlive: boolean | null;
  /** A pane read in flight since this moment, or null when none is. */
  paneReadAt: number | null;
  /** A CPU sample in flight since this moment, or null when none is. */
  cpuReadAt: number | null;
  lastProbeAt: number;
  lastCpuSampleAt: number;
  stalled: boolean;
  overran: boolean;
  /** The code the pane's sentinel line carried, once one has been seen. */
  sentinel: number | null;
  /** Consecutive polls whose capture equalled the previous one: the stillness
   *  that makes a sentinel trustworthy. */
  idlePolls: number;
  /** What the previous poll saw, for that comparison. */
  prevCapture: string;
  polling: boolean;
  stopped: boolean;
}

function runState(): RunState | undefined {
  return (globalThis as any)[RUN_KEY];
}

/** Is a build run being watched right now? */
export function isBuildRunActive(): boolean {
  const s = runState();
  return !!s && !s.stopped;
}

/** The run in flight, for the menu's abort entry and its elapsed line. */
export function activeBuildRun(): BuildRunHandle | null {
  const s = runState();
  return s && !s.stopped ? s.handle : null;
}

/** Stop watching (abort, or a reload). Never touches the build itself. */
export function stopBuildRun(): boolean {
  const s = runState();
  if (!s) return false;
  s.stopped = true;
  if (s.timer) clearInterval(s.timer);
  if ((globalThis as any)[RUN_KEY] === s) delete (globalThis as any)[RUN_KEY];
  return true;
}

/** Abort: stop watching and close the pane, the only thing allowed to kill a
 *  build. Reports nothing — the caller owns the message. */
export async function abortBuildRun(close?: PaneCloser): Promise<BuildRunHandle | null> {
  const handle = activeBuildRun();
  if (!handle) return null;
  stopBuildRun();
  if (handle.pane && close) await close(handle.pane);
  return handle;
}

/** The state a start installs. The pane path adds a poll timer to it; the
 *  in-process path has nothing to poll, only a promise to settle. */
function newRunState(handle: BuildRunHandle): RunState {
  const now = Date.now();
  return {
    handle, timer: null, lastCapture: "", lastOutputAt: now, lastCpu: null, lastCpuAt: now,
    panePid: null, paneAlive: null, paneReadAt: null, cpuReadAt: null,
    lastProbeAt: 0, lastCpuSampleAt: 0, stalled: false, overran: false,
    sentinel: null, idlePolls: 0, prevCapture: "",
    polling: false, stopped: false,
  };
}

export function startBuildRun(handle: BuildRunHandle, seams: BuildRunSeams, report: RunReporter): boolean {
  if (isBuildRunActive()) return false;
  const cadence = { ...RUN_CADENCE, ...seams.cadence };
  const state: RunState = newRunState(handle);
  (globalThis as any)[RUN_KEY] = state;
  const tick = () => void pollRun(state, seams, cadence, report);
  state.timer = setInterval(tick, cadence.signalMs);
  void tick();
  return true;
}

/** A build with no pane (the fallback): the promise is the signal, and the
 *  outcome still arrives as a report instead of an await inside the command.
 *
 *  It is a RUN like any other, and the handle is REQUIRED, not optional: it is
 *  how `activeBuildRun()` and `isBuildRunActive()` answer for it, so a second
 *  start is refused instead of colliding on obj/, bin/, Build/nuspec/* and
 *  src/Common/AssemblyInfoVersion.cs. An optional parameter let a caller create
 *  a run nothing could see, which is the collision this guard exists to stop.
 *  The state is released when the build settles; an abort (which cannot kill an
 *  in-process build) stops the watch and reports nothing. */
export function watchInProcessRun(
  build: Promise<{ code: number; stdout: string }>, report: RunReporter, handle: BuildRunHandle,
): void {
  const state = isBuildRunActive() ? null : newRunState(handle);
  if (state) (globalThis as any)[RUN_KEY] = state;
  const mine = () => state === null || (globalThis as any)[RUN_KEY] === state;
  const settle = (event: RunEvent) => {
    if (!mine()) return; // an abort already stopped the watch: report nothing
    if (state) stopBuildRun();
    return report(event);
  };
  build
    .then((res) => settle({ kind: "done", code: res.code, tail: res.stdout }))
    .catch((err) => settle({ kind: "done", code: -1, tail: String(err) }));
}

/** Consume-on-read the supervisor's exit marker. undefined = not written yet,
 *  a number = the code, null = written but not a number (still terminal, so a
 *  corrupt marker can never leave the run unwatched). */
export function readMarker(marker: string): number | null | undefined {
  if (!fs.existsSync(marker)) return undefined;
  let raw = "";
  try { raw = fs.readFileSync(marker, "utf-8").trim(); } catch { return undefined; }
  if (!raw) return undefined; // mid-write: read it next tick, never report a blank as success
  fs.rmSync(marker, { force: true });
  const code = Number(raw);
  return Number.isFinite(code) ? code : null;
}

/** How long a read may stay in flight before it is written off: three ticks'
 *  worth, never under a quarter second. A written-off read has answered nothing,
 *  and its late reply is ignored — the next tick asks again. */
function readBudgetMs(cadence: RunCadence): number {
  return Math.max(cadence.signalMs * 3, 250);
}

/** Ask the pane how it is WITHOUT waiting for the answer. The reply lands in
 *  the run's state when it arrives, a read still in flight past its budget is
 *  written off, and the next tick asks again. The tick itself never awaits a
 *  seam: the exit marker, the signal the user is waiting for, must never queue
 *  behind a pane that stopped answering. Nothing runs in parallel here — one
 *  pane read is in flight at a time, and its answer is the only writer of
 *  `paneAlive`, `panePid` and the capture clock. */
function readPane(state: RunState, seams: BuildRunSeams, cadence: RunCadence): void {
  const now = Date.now();
  if (state.paneReadAt !== null && now - state.paneReadAt > readBudgetMs(cadence)) state.paneReadAt = null;
  if (state.paneReadAt !== null) return;
  if (!seams.probe || !state.handle.pane) return;
  if (now - state.lastProbeAt < cadence.probeMs) return;
  state.lastProbeAt = now;
  state.paneReadAt = now;
  void seams.probe(state.handle.pane)
    .then(async (paneState) => {
      state.paneReadAt = null;
      state.paneAlive = paneState?.alive !== false;
      state.panePid = paneState?.pid ?? state.panePid;
      if (!state.paneAlive || !seams.capture) return;
      const text = await seams.capture(state.handle.pane as string);
      if (text.trim() !== state.lastCapture.trim()) {
        state.lastCapture = text;
        state.lastOutputAt = Date.now();
      }
    })
    .catch(() => { state.paneReadAt = null; });
}

/** Sample the build host's CPU the same way: fired, never awaited. Output
 *  silence plus a CPU clock that does not move is a hang; a quiet compile still
 *  burns CPU, so it never reads as one. Without a sampler the stall rule falls
 *  back to output silence alone. */
function readCpu(state: RunState, seams: BuildRunSeams, cadence: RunCadence): void {
  const now = Date.now();
  if (!seams.sampleCpu || !state.panePid) return;
  if (state.cpuReadAt !== null && now - state.cpuReadAt > readBudgetMs(cadence)) state.cpuReadAt = null;
  if (state.cpuReadAt !== null) return;
  if (now - state.lastCpuSampleAt < cadence.cpuMs) return;
  state.lastCpuSampleAt = now;
  state.cpuReadAt = now;
  void seams.sampleCpu(state.panePid)
    .then((cpu) => {
      state.cpuReadAt = null;
      if (cpu === null) return;
      if (state.lastCpu !== null && cpu - state.lastCpu > 0.1) state.lastCpuAt = Date.now();
      state.lastCpu = cpu;
    })
    .catch(() => { state.cpuReadAt = null; });
}

async function finish(state: RunState, report: RunReporter, event: RunEvent): Promise<void> {
  stopBuildRun();
  await report(event);
}

/** The two report-only backstops, each fired at most once per run. */
function backstopTick(state: RunState, cadence: RunCadence, report: RunReporter): void {
  const now = Date.now();
  const quietSince = Math.max(state.lastOutputAt, state.lastCpuAt);
  if (!state.stalled && now - quietSince >= cadence.stallMs) {
    state.stalled = true;
    void report({ kind: "stall", silentMs: now - quietSince, limitMs: cadence.stallMs });
  }
  if (!state.overran && now - state.handle.startedAt >= cadence.overrunMs) {
    state.overran = true;
    void report({ kind: "overrun", elapsedMs: now - state.handle.startedAt, limitMs: cadence.overrunMs });
  }
}

/** How many consecutive polls the pane must hold still after its sentinel
 *  line before the code is trusted: the line lands while the build's own last
 *  output is still draining, and stillness is what proves nothing is still
 *  being written. */
export const SENTINEL_IDLE_POLLS = 2;

/** The LAST sentinel the pane printed, or null. A run that reused a pane would
 *  print more than one, and the newest is the one that finished. */
function sentinelCode(text: string): number | null {
  const hits = [...text.matchAll(/RXAF-RUN-DONE code=(-?\d+)/g)];
  const last = hits[hits.length - 1];
  return last ? Number(last[1]) : null;
}

/** Track the pane's stillness and its sentinel, and answer the code once the
 *  sentinel has been seen AND the pane stopped moving for SENTINEL_IDLE_POLLS
 *  polls. This is the SECOND completion signal, for the runs whose exit marker
 *  never lands. */
function sentinelDone(state: RunState): number | null {
  const seen = sentinelCode(state.lastCapture);
  if (seen !== null) state.sentinel = seen;
  state.idlePolls = state.lastCapture === state.prevCapture ? state.idlePolls + 1 : 0;
  state.prevCapture = state.lastCapture;
  if (state.sentinel === null || state.idlePolls < SENTINEL_IDLE_POLLS) return null;
  return state.sentinel;
}

async function pollRun(state: RunState, seams: BuildRunSeams, cadence: RunCadence, report: RunReporter): Promise<void> {
  if (state.stopped || state.polling) return;
  state.polling = true;
  try {
    const code = readMarker(state.handle.marker);
    if (code !== undefined) {
      const note = code === null ? "the exit marker held no readable code" : undefined;
      await finish(state, report, { kind: "done", code: code ?? -1, tail: state.lastCapture, note });
      return;
    }
    readPane(state, seams, cadence);
    const sentinel = sentinelDone(state);
    if (sentinel !== null) {
      await finish(state, report, {
        kind: "done",
        code: sentinel,
        tail: state.lastCapture,
        note: "no exit marker was read — the code came from the run's own sentinel line",
      });
      return;
    }
    if (state.paneAlive === false) {
      await finish(state, report, { kind: "died", tail: state.lastCapture });
      return;
    }
    readCpu(state, seams, cadence);
    backstopTick(state, cadence, report);
  } finally {
    state.polling = false;
  }
}
