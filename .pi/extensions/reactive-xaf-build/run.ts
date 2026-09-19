/**
 * reactive-xaf-build/run — the build run: its lifecycle and its watch.
 *
 * Lifecycle: a per-run temp dir (`runPaths`) holding a supervisor script that
 * runs the build in a NESTED pwsh, so an `exit` inside the build cannot skip
 * the exit-code write, and drops that code into the run's transient marker.
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
import * as os from "node:os";
import * as path from "node:path";
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

/** A run id unique across sessions: two pi windows building in the same
 *  millisecond must never share a run dir (one would overwrite the other's
 *  supervisor script and marker). */
export function newRunId(): string {
  return `${Date.now()}-${process.pid}`;
}

/** The per-run temp artifacts: the supervisor script and its exit marker. */
export interface RunPaths {
  runId: string;
  dir: string;
  script: string;
  marker: string;
}

/** One run's temp artifacts. A fresh dir per run, so a stale marker can
 *  never report a later build. */
export function runPaths(runId: string): RunPaths {
  const dir = path.join(os.tmpdir(), `rxaf-build-${runId}`);
  return { runId, dir, script: path.join(dir, "run.ps1"), marker: path.join(dir, "exit.code") };
}

/** The supervisor the pane runs. The build goes into a NESTED pwsh, so an
 *  `exit` (or a crash) inside the build cannot skip the marker write. */
export function supervisorScript(buildCmd: string, marker: string): string {
  const quotedMarker = marker.replace(/'/g, "''");
  const quotedCmd = buildCmd.replace(/'/g, "''");
  return [
    `$marker = '${quotedMarker}'`,
    "$code = 1",
    "try {",
    `  & pwsh -NoLogo -Command '${quotedCmd}'`,
    "  if ($null -ne $LASTEXITCODE) { $code = $LASTEXITCODE } elseif ($?) { $code = 0 } else { $code = 1 }",
    "} catch { $code = 1 }",
    "Set-Content -LiteralPath $marker -Value $code",
    "",
  ].join("\n");
}

/** Every file write rides pi-dev's tracked seam (a load-time global), the same
 *  one the flow uses for repo files; a missing seam is a loud error. */
export function trackedWrite(file: string, data: string): void {
  const seam = (globalThis as any).__writeFileSync;
  if (typeof seam !== "function") throw new Error("__writeFileSync seam missing — pi-dev not loaded");
  seam(file, data);
}

/** Write a run's supervisor script; returns the same paths for chaining. */
export function writeRunScript(paths: RunPaths, buildCmd: string): RunPaths {
  fs.mkdirSync(paths.dir, { recursive: true });
  trackedWrite(paths.script, supervisorScript(buildCmd, paths.marker));
  return paths;
}

/** The short line typed into the pane. The logic lives in the script file,
 *  so a partially delivered keystroke cannot lose the exit code. */
export function supervisorCommand(paths: RunPaths): string {
  return `pwsh -NoLogo -File "${paths.script}"`;
}

function mtimeOf(dir: string): number {
  try { return fs.statSync(dir).mtimeMs; } catch { return 0; }
}

/** Drop run dirs older than the TTL (best-effort). Age-based on purpose: a
 *  "newest N" rule would delete a live build's dir, including another
 *  session's concurrent run whose marker this process never reads. */
export function pruneRunDirs(ttlMs = 86_400_000): void {
  let names: string[];
  try { names = fs.readdirSync(os.tmpdir()).filter((n) => n.startsWith("rxaf-build-")); } catch { return; }
  const cutoff = Date.now() - ttlMs;
  for (const name of names) {
    const dir = path.join(os.tmpdir(), name);
    if (mtimeOf(dir) >= cutoff) continue;
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

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
  lastProbeAt: number;
  lastCpuSampleAt: number;
  stalled: boolean;
  overran: boolean;
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

export function startBuildRun(handle: BuildRunHandle, seams: BuildRunSeams, report: RunReporter): boolean {
  if (isBuildRunActive()) return false;
  const cadence = { ...RUN_CADENCE, ...seams.cadence };
  const now = Date.now();
  const state: RunState = {
    handle, timer: null, lastCapture: "", lastOutputAt: now, lastCpu: null, lastCpuAt: now,
    panePid: null, lastProbeAt: 0, lastCpuSampleAt: 0, stalled: false, overran: false,
    polling: false, stopped: false,
  };
  (globalThis as any)[RUN_KEY] = state;
  const tick = () => void pollRun(state, seams, cadence, report);
  state.timer = setInterval(tick, cadence.signalMs);
  void tick();
  return true;
}

/** A build with no pane (the fallback): the promise is the signal, and the
 *  outcome still arrives as a report instead of an await inside the command. */
export function watchInProcessRun(build: Promise<{ code: number; stdout: string }>, report: RunReporter): void {
  build
    .then((res) => report({ kind: "done", code: res.code, tail: res.stdout }))
    .catch((err) => report({ kind: "done", code: -1, tail: String(err) }));
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

async function captureTail(state: RunState, seams: BuildRunSeams): Promise<string> {
  if (!seams.capture || !state.handle.pane) return "";
  try { return await seams.capture(state.handle.pane); } catch { return ""; }
}

async function finish(state: RunState, report: RunReporter, event: RunEvent): Promise<void> {
  stopBuildRun();
  await report(event);
}

/** Probe the pane: true when it is gone and no marker has arrived. A live
 *  probe also refreshes the output clock from the captured pane text. */
async function probeTick(state: RunState, seams: BuildRunSeams, cadence: RunCadence): Promise<boolean> {
  const probe = seams.probe;
  if (!probe || !state.handle.pane) return false;
  const now = Date.now();
  if (now - state.lastProbeAt < cadence.probeMs) return false;
  state.lastProbeAt = now;
  let paneState;
  try { paneState = await probe(state.handle.pane); } catch { return false; }
  state.panePid = paneState.pid ?? state.panePid;
  if (!paneState.alive) return true;
  if (!seams.capture) return false;
  try {
    const text = await seams.capture(state.handle.pane);
    if (text.trim() !== state.lastCapture.trim()) {
      state.lastCapture = text;
      state.lastOutputAt = Date.now();
    }
  } catch { /* no pane output to read — the marker and the probe still speak */ }
  return false;
}

/** Sample the build host's CPU. Output silence plus a CPU clock that does not
 *  move is a hang; a quiet compile still burns CPU, so it never reads as one.
 *  The sampler is optional: without it the stall rule falls back to output
 *  silence alone. */
async function cpuTick(state: RunState, seams: BuildRunSeams, cadence: RunCadence): Promise<void> {
  if (!seams.sampleCpu || !state.panePid) return;
  const now = Date.now();
  if (now - state.lastCpuSampleAt < cadence.cpuMs) return;
  state.lastCpuSampleAt = now;
  let cpu: number | null = null;
  try { cpu = await seams.sampleCpu(state.panePid); } catch { cpu = null; }
  if (cpu === null) return;
  if (state.lastCpu !== null && cpu - state.lastCpu > 0.1) state.lastCpuAt = Date.now();
  state.lastCpu = cpu;
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

async function pollRun(state: RunState, seams: BuildRunSeams, cadence: RunCadence, report: RunReporter): Promise<void> {
  if (state.stopped || state.polling) return;
  state.polling = true;
  try {
    const code = readMarker(state.handle.marker);
    if (code !== undefined) {
      const tail = await captureTail(state, seams);
      const note = code === null ? "the exit marker held no readable code" : undefined;
      await finish(state, report, { kind: "done", code: code ?? -1, tail, note });
      return;
    }
    if (await probeTick(state, seams, cadence)) {
      await finish(state, report, { kind: "died", tail: await captureTail(state, seams) });
      return;
    }
    await cpuTick(state, seams, cadence);
    backstopTick(state, cadence, report);
  } finally {
    state.polling = false;
  }
}
