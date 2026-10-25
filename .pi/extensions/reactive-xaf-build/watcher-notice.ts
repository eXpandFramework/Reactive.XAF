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

import type { AzDoStatus, BuildProgress } from "./azdo.js";
import type { AzDoWatcherOptions, WatcherState } from "./watcher.js";
import { steerError, steerWarning } from "./report.js";

/** Three minutes of unreadable polls: long enough for a queued build to appear,
 *  short enough that a watcher started with nothing queued says so. */
const DEFAULT_EMPTY_GRACE_MS = 180_000;

/** 45 minutes: below the 90-minute job timeout the pipeline enforces, and well
 *  above the longest healthy leg observed — `Tests ModelMapper` ran 75 minutes
 *  on 2026-10-25 and was writing the whole time. */
const DEFAULT_STALL_MS = 45 * 60_000;

/** Three in-flight polls with no readable progress is a report, not a guess. */
const BLIND_POLLS = 3;

/** A queue that sits this long never got an agent: 36111 went from queued to a
 *  running job in 12 seconds, 36112 in 7, so a wait measured in minutes is a
 *  starved pool rather than a slow pipeline. */
const DEFAULT_QUEUE_MS = 10 * 60_000;

/** The reading for a poll that carried no PROGRESS= line at all. */
const BLIND: BuildProgress = { kind: "blind" };

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

/** A build that is still running and whose active task has gone quiet: a
 *  WARNING, never a give-up. AzDO reads `inProgress` for a hung job too, so this
 *  line is the only early evidence of a hang; the pipeline's own job timeout
 *  ends the build, and the watch has to still be there to report that verdict
 *  (2026-10-25: the 120-minute deadline had already killed the watch 30 minutes
 *  before `Tests Workflow` was canceled at its 90-minute timeout). */
export async function stalledPoll(ctx: any, pi: any, s: { id: number; activeName: string; silentMs: number }): Promise<void> {
  const task = s.activeName || "the running task";
  const msg = `AzDO ${s.id} is still inProgress but ${task} has written nothing for ${minutes(s.silentMs)} min — it looks hung, and the pipeline's own job timeout will end it. Still watching: the verdict will be reported.`;
  await ctx.ui.notify(msg, "warning");
  if (pi) steerWarning(pi, msg);
}

/** Silence with a KNOWN clock: the live task opened no log of its own, so the
 *  newest change across every log container is the only watermark AzDO offers.
 *  A frozen watermark is output evidence all the same — a build that has written
 *  nothing to any stream for two hours is not unreadable, it is silent — so this
 *  is a stall, reported once per episode, never a blindness line (2026-10-25,
 *  build 36112: task `Build` carried `log: null` and every container had sat
 *  unchanged since 18:57:48 while the watch said it could not see the build). */
export async function frozenPoll(ctx: any, pi: any, s: { id: number; activeName: string; silentMs: number }): Promise<void> {
  const where = s.activeName || "the running phase";
  const msg = `AzDO ${s.id}: no log stream has changed for ${minutes(s.silentMs)} min — ${where} is in flight and writing nothing. Still watching: the pipeline's own timeout is what ends a silent job, and its verdict will be reported.`;
  await ctx.ui.notify(msg, "warning");
  if (pi) steerWarning(pi, msg);
}

/** A build that is queued and NOT picked up: no agent has it, so nothing exists
 *  to read and no stall clock can arm — without its own clock this is the one
 *  state the watch could never speak about. 36111 went from queued to a running
 *  job in 12 seconds, 36112 in 7, so a wait measured in minutes is a starved
 *  pool, not a slow pipeline. One warning per wait, keyed
 *  `<buildId>:queue:<queuedAt>` — a re-queued build is a new wait. */
export async function queuedPoll(ctx: any, pi: any, s: { id: number; queuedMs: number }): Promise<void> {
  const msg = `AzDO ${s.id} has been queued for ${minutes(s.queuedMs)} min and no agent has picked it up — the build has not started, so nothing can be read about it. Check that C11-C14 are Running and that no other build holds the pool. Still watching.`;
  await ctx.ui.notify(msg, "warning");
  if (pi) steerWarning(pi, msg);
}

/** The monitor cannot see: BLIND_POLLS in-flight polls carried no readable
 *  progress AT ALL (no timeline record to name, no log watermark to time), so
 *  nothing can be said about this build. Reported ONCE per build, as a warning —
 *  never as "hung", which would be a claim the evidence does not support. The
 *  watch stays alive: the pipeline's own timeout still ends the build, and that
 *  verdict has to reach the agent. This is the LAST resort: a build that is
 *  merely queued or whose task opened no log is read by `frozenPoll` instead. */
export async function blindPoll(ctx: any, pi: any, s: { id: number; polls: number }): Promise<void> {
  const msg = `AzDO ${s.id}: the build's progress cannot be read (${s.polls} polls carried no timeline/log line) — the monitor is blind about this build. Still watching its status; a job that stops writing will only surface when the pipeline's own timeout ends it.`;
  await ctx.ui.notify(msg, "warning");
  if (pi) steerWarning(pi, msg);
}

/** One in-flight poll's progress verdict: which voice speaks about it, and
 *  whether that voice has already spoken about THIS silence. The episode is
 *  keyed `<buildId>:<lastOutputAt>` — ONE key for both stall voices, so a
 *  reading that flips between the task log and the container watermark, or that
 *  moves to another record, is still the same silence and is announced once.
 *  Output that resumes advances the stamp, which re-arms the episode. */
export async function reportProgress(ctx: any, pi: any, state: WatcherState, opts: AzDoWatcherOptions, s: AzDoStatus): Promise<void> {
  const p = s.progress ?? BLIND;
  if (p.kind === "blind") return noteBlind(ctx, pi, state, s.id);
  state.blindPolls = 0;
  state.blindBuildId = null;
  if (p.kind === "queue") return noteQueue(ctx, pi, state, opts, s.id, p.queuedAt);
  await noteSilence(ctx, pi, state, opts, s, p);
}

/** A poll the script could not read: counted per build, spoken once. */
async function noteBlind(ctx: any, pi: any, state: WatcherState, id: number): Promise<void> {
  state.stallKey = null;
  state.blindPolls = state.blindBuildId === id ? state.blindPolls + 1 : 1;
  state.blindBuildId = id;
  if (state.blindPolls === BLIND_POLLS) await blindPoll(ctx, pi, { id, polls: state.blindPolls });
}

/** A queued build: quiet until the wait itself is the story. */
async function noteQueue(ctx: any, pi: any, state: WatcherState, opts: AzDoWatcherOptions, id: number, queuedAt: number | null): Promise<void> {
  const queuedMs = queuedAt === null ? 0 : Date.now() - queuedAt;
  if (queuedMs < (opts.queueMs ?? DEFAULT_QUEUE_MS)) {
    state.stallKey = null;
    return;
  }
  const key = `${id}:queue:${queuedAt}`;
  if (state.stallKey === key) return;
  state.stallKey = key;
  await queuedPoll(ctx, pi, { id, queuedMs });
}

/** A running build: the task's own log names it, the container watermark is all
 *  there is when the task opened no log, and only a poll the script could not
 *  read is blindness. */
async function noteSilence(ctx: any, pi: any, state: WatcherState, opts: AzDoWatcherOptions, s: AzDoStatus, p: Extract<BuildProgress, { kind: "running" }>): Promise<void> {
  const silentMs = Date.now() - p.lastOutputAt;
  if (silentMs < (opts.stallMs ?? DEFAULT_STALL_MS)) {
    state.stallKey = null;
    return;
  }
  const key = `${s.id}:${p.lastOutputAt}`;
  if (state.stallKey === key) return;
  state.stallKey = key;
  if (p.source === "logs") await frozenPoll(ctx, pi, { id: s.id, activeName: p.activeName, silentMs });
  else await stalledPoll(ctx, pi, { id: s.id, activeName: p.activeName, silentMs });
}

/** A finished HEAD build of another version is not this run: nothing was queued
 *  for the version in `profile.versionFile`, so there is no point waiting on a
 *  deadline — say what was found, what was expected, and how to queue it. */
export async function wrongRun(ctx: any, state: WatcherState, pi: any, s: { id: number; buildNumber: string }, expected: string): Promise<void> {
  await giveUp(ctx, state, `AzDO ${s.id} ${s.buildNumber} is not this run — expected ${expected}, and nothing was queued for it. Queue it with Publish or Build & Publish, then start the watcher again.`, pi);
}
