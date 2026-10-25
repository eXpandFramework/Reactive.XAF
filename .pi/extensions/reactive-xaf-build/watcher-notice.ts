/**
 * reactive-xaf-build/watcher-notice — everything the background watcher SAYS:
 * giving up, the empty-poll retry, and the grace that separates the two.
 *
 * Split out of watcher.ts at the 400-line cap. The chain's polling and its
 * decisions stayed there; what lives here is the voice — one warning (toast +
 * steer) for a giveaway, one informational line for a poll that found nothing,
 * and the budget that decides which of the two an empty chain gets.
 *
 * An empty poll is not a failure: a queued build takes a moment to appear on
 * the AzDO side, so the poll retries. It is not nothing either — the chain was
 * told to follow a build and none exists — which is why the retry has its own
 * grace. Past it the watcher says the true thing and stops, instead of
 * reporting "waiting…" once a minute until the two-hour deadline (2026-10-25).
 */

import type { AzDoWatcherOptions, WatcherState } from "./watcher.js";

/** Three minutes of empty polls: long enough for a queued build to appear,
 *  short enough that a watcher started with nothing queued says so. */
const DEFAULT_EMPTY_GRACE_MS = 180_000;

/** Give up: one warning the user sees (toast) and one the agent reads (steer),
 *  then stop the watcher. The only refusal in this module — every give-up path
 *  goes through it, so a watcher never dies silently. */
export async function giveUp(ctx: any, state: WatcherState, msg: string, pi?: any): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  if (pi) pi.sendUserMessage(msg, { deliverAs: "steer" });
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
  if (state.emptySince === null) state.emptySince = Date.now();
  const emptyMs = Date.now() - state.emptySince;
  const graceMs = opts.emptyGraceMs ?? DEFAULT_EMPTY_GRACE_MS;
  if (emptyMs >= graceMs) {
    const mins = Math.max(1, Math.round(emptyMs / 60_000));
    await giveUp(ctx, state, `AzDO: no build was queued for this chain (${mins} min of empty polls) — there is nothing to watch. Queue it with Publish or Build & Publish, then start the watcher again.`, pi);
    return;
  }
  await notifyEmptyPoll(ctx, state);
}
