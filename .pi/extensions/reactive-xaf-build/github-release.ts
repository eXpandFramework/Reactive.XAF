/**
 * reactive-xaf-build/github-release — getting the GitHub release for the built
 * version actually PUBLISHED, and remembering that it happened.
 *
 * Split out of watcher.ts at the 400-line cap. The pipeline's own release step
 * creates the release as a DRAFT on a master build
 * (`DevExpress.XAF-GitHub-Release.ps1:121`, `Xpand-GitHub-Release.ps1:150`), so
 * unless something publishes it the release stays invisible until a human
 * visits GitHub — 4.261.6.0 and 4.261.4.0 were both published by hand, minutes
 * after their pipelines finished.
 *
 * Three doors, one attempt underneath:
 *   - `publishRelease` — look at the repo's releases, then publish the draft for
 *     that version (or say why it is already done / not there yet).
 *   - `startReleaseWatch` — the same attempt, repeated until the draft appears.
 *     OWNED BY THE FLOW, never by the chain watcher: the release still happens
 *     when the watch dies, a second Publish replaces it, or the release
 *     pipeline runs two hours after the queue.
 *   - `releasePhase` — the menu's own door, for a release that was missed.
 *
 * Every outcome goes to the store (`stamp.ts` `recordRelease`), so "did the
 * release happen?" is answered later by "Last build status" instead of by
 * opening GitHub — which is what the hand-published drafts never had. The
 * token is never logged.
 */

import { githubHtmlUrl, githubReleasesUrl, githubReleaseUrl, profileOf, publishedVersion } from "./profile.js";
import type { Choice } from "./profile.js";
import { recordRelease } from "./release-store.js";
import type { ReleaseStatus } from "./release-store.js";
import { steerError, steerWarning } from "./report.js";
import { defaultGhFetch } from "./azdo.js";
import { sleep } from "./pane.js";
import type { BuildSeams } from "./build.js";

export type GhFetch = (url: string, opts?: { method?: string; body?: string }) => Promise<{ ok: boolean; status: number; text: string }>;

/** Everything one release attempt needs, resolved from the profile and the
 *  ports — nothing here reads the environment for policy. */
export interface ReleaseTarget {
  gh: GhFetch;
  repo: string;
  prerelease: boolean;
  version: string;
  token: string | undefined;
  /** How many looks ONE call makes. The chain's last step retries, because the
   *  index lags a nuget push; the watch makes one look per poll and owns the
   *  rest of the waiting. */
  attempts: number;
  delayMs: number;
}

/** How a release attempt ended. `no-draft` is NOT a failure: the pipeline that
 *  creates the draft may still be running, which is why the watch waits. */
export type ReleaseOutcome =
  | { kind: "published"; detail: string }
  | { kind: "already-published"; detail: string }
  | { kind: "no-draft"; detail: string }
  | { kind: "no-token"; detail: string }
  | { kind: "failed"; detail: string };

function releaseKind(prerelease: boolean): string {
  return prerelease ? "pre-release" : "release";
}

function message(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** The one place a target is built: which repo, which kind, which token. */
export function releaseTarget(
  seams: BuildSeams, choice: Choice, version: string, token?: string,
  retry: { attempts?: number; delayMs?: number } = {},
): ReleaseTarget {
  return {
    gh: seams.ghFetch ?? defaultGhFetch,
    repo: profileOf(seams).githubRepo(choice),
    prerelease: choice !== "Release",
    version,
    token: token ?? seams.ghToken ?? process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN,
    attempts: retry.attempts ?? 1,
    delayMs: retry.delayMs ?? 0,
  };
}

/** The store's status word for an outcome. */
export function statusOf(out: ReleaseOutcome): ReleaseStatus {
  switch (out.kind) {
    case "published": return "published";
    case "already-published": return "already-published";
    case "no-draft": return "no-draft";
    case "no-token": return "no-token";
    default: return "failed";
  }
}

/** Nothing to publish yet, or the call itself failed: the watch keeps looking
 *  until its budget runs out before calling either a failure. */
export function retryable(out: ReleaseOutcome): boolean {
  return out.kind === "no-draft" || out.kind === "failed";
}

interface Found {
  draftId?: number;
  published?: boolean;
  prerelease?: boolean;
}

function findRelease(
  releases: Array<{ id?: number; tag_name?: string; draft?: boolean; prerelease?: boolean }>, version: string,
): Found {
  for (const r of releases) {
    if (r.tag_name !== version) continue;
    if (r.draft === true && typeof r.id === "number") return { draftId: r.id };
    if (r.draft === false) return { published: true, prerelease: r.prerelease === true };
  }
  return {};
}

type Look = { outcome: ReleaseOutcome } | { found: Found };

async function lookUp(t: ReleaseTarget): Promise<Look> {
  let res;
  try {
    res = await t.gh(githubReleasesUrl(t.repo));
  } catch (err) {
    return { outcome: { kind: "failed", detail: `GitHub query failed: ${message(err)}` } };
  }
  if (!res.ok) return { outcome: { kind: "failed", detail: `GitHub query failed: HTTP ${res.status}` } };
  let list: unknown;
  try {
    list = JSON.parse(res.text);
  } catch {
    return { outcome: { kind: "failed", detail: "GitHub returned no readable release list" } };
  }
  if (!Array.isArray(list)) return { outcome: { kind: "failed", detail: "GitHub returned no release list" } };
  const found = findRelease(list, t.version);
  if (found.draftId !== undefined || found.published) return { found };
  return { outcome: { kind: "no-draft", detail: `GitHub has no release ${t.version} in ${t.repo} yet` } };
}

async function publishDraft(t: ReleaseTarget, id: number): Promise<ReleaseOutcome> {
  let res;
  try {
    res = await t.gh(githubReleaseUrl(t.repo, id), {
      method: "PATCH",
      body: JSON.stringify({ draft: false, prerelease: t.prerelease }),
    });
  } catch (err) {
    return { kind: "failed", detail: `GitHub publish failed: ${message(err)}` };
  }
  if (!res.ok) return { kind: "failed", detail: `GitHub publish failed: HTTP ${res.status}` };
  return { kind: "published", detail: `GitHub ${releaseKind(t.prerelease)} ${t.version} published from draft` };
}

async function attempt(t: ReleaseTarget): Promise<ReleaseOutcome> {
  const look = await lookUp(t);
  if ("outcome" in look) return look.outcome;
  if (look.found.published) {
    const kind = releaseKind(look.found.prerelease === true);
    return { kind: "already-published", detail: `GitHub ${kind} ${t.version} already published` };
  }
  return publishDraft(t, look.found.draftId!);
}

/** Publish the release for `version`. Five outcomes, and `no-token` is checked
 *  BEFORE anything is sent: an unauthenticated draft is invisible to a GET, so
 *  asking GitHub without a token would read as "no such release". */
export async function publishRelease(t: ReleaseTarget): Promise<ReleaseOutcome> {
  if (!t.token) {
    return { kind: "no-token", detail: `GH_TOKEN is not set — publish the draft ${t.version} by hand: ${githubHtmlUrl(t.repo)}` };
  }
  const tries = Math.max(1, t.attempts);
  let last: ReleaseOutcome = { kind: "no-draft", detail: `GitHub has no release ${t.version} in ${t.repo} yet` };
  for (let i = 0; i < tries; i++) {
    last = await attempt(t);
    if (!retryable(last)) return last;
    if (i < tries - 1) await sleep(t.delayMs);
  }
  if (last.kind !== "no-draft") return last;
  return { kind: "no-draft", detail: `GitHub release ${t.version} NOT found after ${tries} tries` };
}

/** Say the outcome where the user meets it: the toast, plus a steer when the
 *  release did NOT happen (silence is what made a hand-publish necessary). */
async function announce(pi: any, ctx: any, out: ReleaseOutcome, storeReason: string | null): Promise<void> {
  const detail = storeReason ? `${out.detail} (the outcome could not be recorded: ${storeReason})` : out.detail;
  const bad = out.kind !== "published" && out.kind !== "already-published";
  if (ctx?.ui?.notify) await ctx.ui.notify(detail, bad ? "warning" : "info");
  if (!bad || !pi) return;
  if (out.kind === "no-draft") steerWarning(pi, detail);
  else steerError(pi, detail);
}

async function record(
  seams: BuildSeams, t: { version: string; repo: string }, choice: Choice, status: ReleaseStatus, detail: string,
): Promise<string | null> {
  return recordRelease(seams, { version: t.version, mode: choice, repo: t.repo, status, detail });
}

/** The ONE voice for a release that cannot even start. The user asked the flow
 *  for a release and it did not run: that is exactly the silence that made a
 *  hand-publish look like a flow with no release step at all, so this warns
 *  once, steers once and records the row. Both doors — the watch's queue-time
 *  start and the menu — go through it, and neither adds a second line. */
async function failNoVersion(pi: any, ctx: any, seams: BuildSeams, repo: string, choice: Choice): Promise<string> {
  const p = profileOf(seams);
  const detail = `No GitHub release attempt: no version could be read from ${p.versionFile} in ${repo}`;
  const reason = await record(seams, { version: "", repo: p.githubRepo(choice) }, choice, "failed", detail);
  await announce(pi, ctx, { kind: "failed", detail }, reason);
  return detail;
}

const WATCH_KEY = "__rxafReleaseWatch";
/** One look a minute: the draft appears within seconds of the pipeline's last
 *  task, and GitHub's list endpoint is cheap. */
const DEFAULT_INTERVAL_MS = 60_000;
/** Two hours after the queue the release pipeline has either run or never will;
 *  the old code waited on the chain watcher for as long as it lived. */
const DEFAULT_DEADLINE_MS = 3 * 60 * 60_000;

export interface ReleaseWatchOptions {
  repo: string;
  choice: Choice;
  /** Omitted: read from the profile's version file. */
  version?: string | null;
  intervalMs?: number;
  deadlineMs?: number;
  ghToken?: string;
}

export interface ReleaseWatchHandle {
  version: string;
  stop(): void;
  active(): boolean;
}

interface WatchState {
  handle: ReleaseWatchHandle;
  stopped: boolean;
  timer: ReturnType<typeof setTimeout> | null;
  choice: Choice;
  version: string;
  deadline: number;
  intervalMs: number;
  token: string | undefined;
}

function stateOf(): WatchState | null {
  return (globalThis as any)[WATCH_KEY] ?? null;
}

export function activeReleaseWatch(): ReleaseWatchHandle | null {
  return stateOf()?.handle ?? null;
}

export function stopReleaseWatch(): void {
  stateOf()?.handle.stop();
}

function stopState(state: WatchState): void {
  state.stopped = true;
  if (state.timer) clearTimeout(state.timer);
  state.timer = null;
  if (stateOf() === state) delete (globalThis as any)[WATCH_KEY];
}

function makeState(seams: BuildSeams, opts: ReleaseWatchOptions, version: string): WatchState {
  const intervalMs = opts.intervalMs ?? seams.releaseIntervalMs ?? DEFAULT_INTERVAL_MS;
  const deadlineMs = opts.deadlineMs ?? seams.releaseDeadlineMs ?? DEFAULT_DEADLINE_MS;
  const state = {
    stopped: false,
    timer: null,
    choice: (opts.choice === "Release" ? "Release" : "Lab") as Choice,
    version,
    deadline: Date.now() + deadlineMs,
    intervalMs,
    token: opts.ghToken ?? seams.ghToken,
    handle: { version, stop: () => {}, active: () => false },
  } as WatchState;
  state.handle = { version, stop: () => stopState(state), active: () => !state.stopped };
  return state;
}

function gaveUp(out: ReleaseOutcome, target: ReleaseTarget): ReleaseOutcome {
  const detail = `${out.detail} — not published within the watch budget; publish it by hand: ${githubHtmlUrl(target.repo)}`;
  return out.kind === "failed" ? { kind: "failed", detail } : { kind: "no-draft", detail };
}

function schedule(state: WatchState, pi: any, ctx: any, seams: BuildSeams): void {
  state.timer = setTimeout(() => void tick(state, pi, ctx, seams), state.intervalMs);
  (state.timer as { unref?: () => void } | null)?.unref?.();
}

async function tick(state: WatchState, pi: any, ctx: any, seams: BuildSeams): Promise<void> {
  if (state.stopped) return;
  const target = releaseTarget(seams, state.choice, state.version, state.token);
  const out = await publishRelease(target);
  if (retryable(out) && Date.now() < state.deadline) {
    await record(seams, target, state.choice, "waiting", out.detail);
    if (!state.stopped) schedule(state, pi, ctx, seams);
    return;
  }
  const final = retryable(out) ? gaveUp(out, target) : out;
  const reason = await record(seams, target, state.choice, statusOf(final), final.detail);
  await announce(pi, ctx, final, reason);
  state.handle.stop();
}

/** Watch for the release of one version until it is published or the budget
 *  runs out. Synchronous by design: the caller is a flow step, never a wait. */
export function startReleaseWatch(pi: any, ctx: any, seams: BuildSeams, opts: ReleaseWatchOptions): ReleaseWatchHandle {
  const p = profileOf(seams);
  const version = opts.version ?? publishedVersion(opts.repo, p.versionFile);
  const running = stateOf();
  if (running && version === running.version && !running.stopped) return running.handle;
  if (running) running.handle.stop();
  if (!version) {
    // This function is synchronous by contract, so the report cannot be awaited
    // here — but its promise is HANDLED: a store or toast failure becomes its
    // own steer instead of a rejection nobody ever sees.
    void failNoVersion(pi, ctx, seams, opts.repo, opts.choice).catch((err) => {
      if (pi) steerError(pi, `No GitHub release attempt, and the reason could not be reported: ${message(err)}`);
    });
    return { version: "", stop: () => {}, active: () => false };
  }
  const state = makeState(seams, opts, version);
  (globalThis as any)[WATCH_KEY] = state;
  schedule(state, pi, ctx, seams);
  return state.handle;
}

/** The menu door: publish now, or start the watch when there is nothing to
 *  publish yet. Answers the line the flow reports. */
export async function releasePhase(
  ctx: any, seams: BuildSeams, repo: string, choice: Choice, pi?: any,
): Promise<string> {
  const version = publishedVersion(repo, profileOf(seams).versionFile);
  if (!version) return failNoVersion(pi, ctx, seams, repo, choice);
  const target = releaseTarget(seams, choice, version);
  const out = await publishRelease(target);
  if (retryable(out)) {
    const reason = await record(seams, target, choice, "waiting", out.detail);
    startReleaseWatch(pi, ctx, seams, { repo, choice, version });
    const msg = `${out.detail} — watching for it; the outcome is recorded and reported.${reason ? ` (not recorded: ${reason})` : ""}`;
    if (ctx?.ui?.notify) await ctx.ui.notify(msg, "info");
    return msg;
  }
  const reason = await record(seams, target, choice, statusOf(out), out.detail);
  await announce(pi, ctx, out, reason);
  return out.detail;
}
