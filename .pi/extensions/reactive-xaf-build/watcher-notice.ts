/**
 * reactive-xaf-build/watcher-notice — everything the background watcher SAYS:
 * giving up, the retry lines, and the grace that separates them.
 *
 * Split out of watcher.ts at the 400-line cap. The chain's polling, parsing and
 * decisions stayed there; what lives here is the voice — one ERROR (toast plus
 * a red steer) for a give-up, one informational line for a poll that found
 * nothing or could not be read, and the budget that decides which of the two a
 * chain gets. The classification itself is NOT decided here: `azdo.ts` prints
 * `STATUS=ERR;<auth|transient>;<message>` and `watcher.ts` switches on it, so
 * this module never sniffs prose.
 *
 * An empty poll is not a failure: a queued build takes a moment to appear on
 * the AzDO side, so the poll retries. It is not nothing either — the chain was
 * told to follow a build and none exists — which is why the retry has its own
 * grace. Past it the watcher says the true thing and stops, instead of
 * reporting "waiting…" once a minute until the two-hour deadline (2026-10-25).
 *
 * A REFUSED token is different from both: no retry clears it, and the agent
 * cannot renew a PAT, so it gives up on the first poll with the remedy in the
 * message. A transient failure keeps retrying on the same grace clock.
 */

import type { AzDoWatcherOptions, WatcherState } from "./watcher.js";
import { steerError } from "./report.js";

/** Three minutes of unreadable polls: long enough for a queued build to appear,
 *  short enough that a watcher started with nothing queued says so. */
const DEFAULT_EMPTY_GRACE_MS = 180_000;

const TOKEN_REMEDY = "Renew it (Azure DevOps → User settings → Personal access tokens), then queue the pipeline again with Publish.";

function graceMs(opts: AzDoWatcherOptions): number {
  return opts.emptyGraceMs ?? DEFAULT_EMPTY_GRACE_MS;
}

/** How long this stretch of unreadable polls has lasted, started on the first
 *  one. Cleared by watcher.ts the moment a build appears, so a later queue is
 *  never judged by an old clock. */
function failingMs(state: WatcherState): number {
  if (state.emptySince === null) state.emptySince = Date.now();
  return Date.now() - state.emptySince;
}

function minutes(ms: number): number {
  return Math.max(1, Math.round(ms / 60_000));
}

/** Give up: one warning the user sees (toast) and one the agent reads as an
 *  ERROR (`report.ts` steerError), then stop the watcher. The only refusal in
 *  this module — every give-up path goes through it, so a watcher never dies
 *  silently AND a condition the agent cannot repair (an expired token) arrives
 *  red instead of looking like one more line of chatter. */
export async function giveUp(ctx: any, state: WatcherState, msg: string, pi?: any): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  if (pi) steerError(pi, msg);
  state.stop();
}

/** The retry's line: informational, no steer, no model turn. */
async function notifyEmptyPoll(ctx: any, state: WatcherState): Promise<void> {
  const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
  await ctx.ui.notify(`AzDO: no build found yet (${elapsed} min) — waiting…`, "info");
}

/** An empty poll: inside the grace it keeps retrying (a queue takes a moment to
 *  appear), past it the watcher says the true thing and stops — the chain was
 *  asked to follow a build and none was queued. */
export async function emptyPoll(ctx: any, state: WatcherState, opts: AzDoWatcherOptions, pi: any): Promise<void> {
  const emptyMs = failingMs(state);
  if (emptyMs >= graceMs(opts)) {
    await giveUp(ctx, state, `AzDO: no build was queued for this chain (${minutes(emptyMs)} min of empty polls) — there is nothing to watch. Queue it with Publish or Build & Publish, then start the watcher again.`, pi);
    return;
  }
  await notifyEmptyPoll(ctx, state);
}

/** The call itself failed for a reason that is worth another poll (a 429, a
 *  network blip, a proxy hiccup). Same grace as an empty chain: a short outage
 *  is retried, a lasting one is reported instead of spinning. */
export async function transientPoll(ctx: any, state: WatcherState, opts: AzDoWatcherOptions, pi: any, message: string): Promise<void> {
  const failing = failingMs(state);
  if (failing >= graceMs(opts)) {
    await giveUp(ctx, state, `AzDO could not be read for ${minutes(failing)} min (${message}) — the pipeline may be fine; check the network and the agents, then start the watcher again.`, pi);
    return;
  }
  await ctx.ui.notify(`AzDO poll failed (${message}) — retrying…`, "info");
}

/** The token is refused or expired: the one failure no retry and no agent can
 *  fix, so the watch ends on the FIRST poll with the remedy the user needs. */
export async function refusedPoll(ctx: any, state: WatcherState, pi: any, message: string): Promise<void> {
  await giveUp(ctx, state, `AzDO refused the request: ${message} — the token is expired or refused, so nothing about this run can be read. ${TOKEN_REMEDY}`, pi);
}

/** A finished HEAD build of another version is not this run: nothing was queued
 *  for the version in `profile.versionFile`, so there is no point waiting on a
 *  deadline — say what was found, what was expected, and how to queue it. */
export async function wrongRun(ctx: any, state: WatcherState, pi: any, s: { id: number; buildNumber: string }, expected: string): Promise<void> {
  await giveUp(ctx, state, `AzDO ${s.id} ${s.buildNumber} is not this run — expected ${expected}, and nothing was queued for it. Queue it with Publish or Build & Publish, then start the watcher again.`, pi);
}
