/**
 * reactive-xaf-build/watcher — background AzDO build watcher.
 *
 * After prx queues a build, monitorPhase starts a watcher instead of blocking
 * the agent turn: a timer polls the newest build via azdoStatusScript and
 * NOTIFIES ON EVERY CHECK (the user asked for a toast each time the build is
 * looked at — same-type notifies self-replace, so the toast acts as a live
 * status line). On a terminal outcome the watcher stops itself; a failed
 * build delivers a steer (pi.sendUserMessage, deliverAs "steer" — the same
 * turn-independent failure path steerFailure uses) so the agent plans a fix
 * without holding the chat hostage for the build's duration.
 *
 * Registry lives on globalThis (Symbol key) — module-level state would
 * duplicate on Windows path-casing double loads. One watcher at a time:
 * starting a new one stops the previous.
 */

import { azdoStatusScript, parseStatus, AZDO_BUILD_URL } from "./azdo.js";
import type { BuildSeams } from "./build.js";

export interface AzDoWatcherOptions {
  /** Poll interval (default 60 s). */
  intervalMs?: number;
  /** Give-up deadline (default 2 h). */
  maxMs?: number;
}

export interface AzDoWatcherHandle {
  stop: () => void;
  active: () => boolean;
  lastBuildId: () => number | null;
}

export type AzDoWatcherStarter = (pi: any, ctx: any, seams: BuildSeams, opts?: AzDoWatcherOptions) => AzDoWatcherHandle;

const WATCHER_KEY = Symbol.for("reactive-xaf-build.azdo-watcher");

interface WatcherState {
  timer: ReturnType<typeof setInterval> | null;
  startedAt: number;
  lastId: number | null;
  stopped: boolean;
  stop: () => void;
}

function watcherState(): WatcherState | undefined {
  return (globalThis as any)[WATCHER_KEY];
}

/** Stop the active watcher, if any. Returns true when one was running. */
export function stopAzDoWatcher(): boolean {
  const state = watcherState();
  if (!state) return false;
  state.stop();
  return true;
}

export function isAzDoWatcherActive(): boolean {
  const state = watcherState();
  return !!state && !state.stopped;
}

/** Stop the watcher with a warning toast. */
async function giveUp(ctx: any, state: WatcherState, msg: string): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  state.stop();
}

/** Terminal message for a completed build; failed builds carry the reason
 *  and the FAILED label (same convention as status.ts). */
function terminalMessage(s: { id: number; result: string; reason: string }): { msg: string; failed: boolean } {
  const failed = s.result === "failed";
  const label = failed ? "FAILED" : s.result;
  const reason = failed ? ` — ${s.reason || "no error lines"}` : "";
  return { msg: `AzDO build ${s.id} ${label}${reason} — ${AZDO_BUILD_URL}`, failed };
}

/** One poll tick: query, toast, stop on terminal. */
async function pollTick(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, maxMs: number): Promise<void> {
  if (state.stopped) return;
  if (Date.now() - state.startedAt > maxMs) {
    await giveUp(ctx, state, `AzDO watcher gave up after ${Math.round(maxMs / 60_000)} min — check /devexpress status.`);
    return;
  }
  let res;
  try {
    res = await seams.run(azdoStatusScript(), { timeoutMs: 60_000 });
  } catch (err) {
    await giveUp(ctx, state, `AzDO watcher failed: ${err instanceof Error ? err.message : String(err)} — check /devexpress status.`);
    return;
  }
  const s = parseStatus(res.stdout);
  if (!s || s.id === 0) {
    const err = (res.stderr || "no STATUS= line").trim().slice(-200);
    await giveUp(ctx, state, `AzDO watcher: ${err} — check /devexpress status.`);
    return;
  }
  state.lastId = s.id;
  if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
    const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
    await ctx.ui.notify(`AzDO ${s.id}: ${s.status} (${elapsed} min) — ${AZDO_BUILD_URL}`, "info");
    return;
  }
  const { msg, failed } = terminalMessage(s);
  await ctx.ui.notify(msg, failed ? "warning" : "info");
  state.stop();
  if (failed) pi.sendUserMessage(msg, { deliverAs: "steer" });
}

/** Start the background watcher. The previous watcher, if any, is stopped. */
export function startAzDoWatcher(pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions = {}): AzDoWatcherHandle {
  stopAzDoWatcher();
  const intervalMs = opts.intervalMs ?? 60_000;
  const maxMs = opts.maxMs ?? 7_200_000;
  const state: WatcherState = {
    timer: null,
    startedAt: Date.now(),
    lastId: null,
    stopped: false,
    stop: () => {
      if (state.stopped) return;
      state.stopped = true;
      if (state.timer) clearInterval(state.timer);
      if ((globalThis as any)[WATCHER_KEY] === state) delete (globalThis as any)[WATCHER_KEY];
    },
  };
  (globalThis as any)[WATCHER_KEY] = state;
  const tick = () => pollTick(state, pi, ctx, seams, maxMs);
  state.timer = setInterval(() => void tick(), intervalMs);
  void tick();
  return {
    stop: () => state.stop(),
    active: () => !state.stopped,
    lastBuildId: () => state.lastId,
  };
}
