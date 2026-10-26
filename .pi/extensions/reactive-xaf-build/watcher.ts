/**
 * reactive-xaf-build/watcher — background AzDO watcher for the publish chain.
 *
 * Chain, nuget id, version file, GitHub repo and on-success action come
 * from RepoProfile. RX is the default. One watcher at a time. A finished
 * HEAD build whose buildNumber version does not match versionFile is not
 * this run (wait, then give-up steers); the downstream pipelines report
 * date numbers and are fenced by the build-id baseline. Failed polls use
 * extractFailReason.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { azdoStatusScript, parsePoll, azdoBuildUrl, defaultGhFetch, extractFailReason, failLogFromStdout } from "./azdo.js";
import type { AzDoStatus } from "./azdo.js";
import { pollTestFails, newTestWatch, type TestWatch } from "./test-watch.js";
import { defaultDelegateWindow } from "./delegate.js";
import type { WindowSpawner } from "./delegate.js";
import type { BuildSeams } from "./build.js";
import { sleep } from "./pane.js";
import {
  rxProfile, nugetAssertUrl, nugetOrgNuspecUrl, compareVersions,
} from "./profile.js";
import type { RepoProfile, Choice, ChainStep } from "./profile.js";
import { publishRelease } from "./github-release.js";
import { emptyPoll, giveUp, refusedPoll, reportProgress, transientPoll, wrongRun } from "./watcher-notice.js";

function profileOf(seams: BuildSeams): RepoProfile {
  return seams.profile ?? rxProfile;
}

export interface AzDoWatcherOptions {
  intervalMs?: number;
  /** A BACKSTOP only, and only when a caller asks for one: by default the watch
   *  has no wall-clock deadline at all (checkDeadline). */
  maxMs?: number;
  /** How long the in-flight task may write nothing before the watch says it
   *  looks hung. Sits below the 90-minute job timeout the pipeline enforces. */
  stallMs?: number;
  /** How long a build may sit QUEUED with no agent before the watch says so.
   *  A healthy queue turns into a running job in seconds (`watcher-notice.md`). */
  queueMs?: number;
  /** How long an all-empty chain keeps retrying before the watcher says there
   *  is nothing to watch. Its own budget: the maxMs deadline belongs to a chain
   *  that has a build in flight. */
  emptyGraceMs?: number;
  followNugets?: boolean;
  /** Scan the logs of a running build's tests and offer ONE delegation of the
   *  failures to `testRole`; `spawnWindow` opens that window. */
  watchTests?: boolean;
  testRole?: string;
  spawnWindow?: WindowSpawner;
  choice?: "Lab" | "Release";
  ghToken?: string;
  repoRoot?: string;
  ghRetries?: number;
  ghRetryMs?: number;
  nugetRetries?: number;
  nugetRetryMs?: number;
}

export interface AzDoWatcherHandle {
  stop: () => void;
  active: () => boolean;
  lastBuildId: () => number | null;
}

export type AzDoWatcherStarter = (pi: any, ctx: any, seams: BuildSeams, opts?: AzDoWatcherOptions) => AzDoWatcherHandle;

const WATCHER_KEY = Symbol.for("reactive-xaf-build.azdo-watcher");
/** The in-flight thresholds (`stallMs`, `queueMs`, `BLIND_POLLS`) live with the
 *  voices in `watcher-notice.ts`: they decide what is SAID, not how the chain
 *  advances. */
const NUGET_VERSION_RE = /Version='([^']+)'/g;
const VERSION_RE = /Version\s*=\s*"([^"]+)"/;

function chainFor(choice: Choice, seams: BuildSeams): ChainStep[] {
  return profileOf(seams).chain(choice);
}

export interface WatcherState {
  timer: ReturnType<typeof setInterval> | null;
  startedAt: number;
  step: number;
  chain: ChainStep[];
  chainLength: number;
  minId: number;
  lastId: number | null;
  /** When the first empty poll of this stretch happened, or null while the
   *  chain has a build. Cleared the moment a build appears, so a later queue is
   *  never judged by an old clock. */
  emptySince: number | null;
  /** The stall episode already reported (`<buildId>:<task>`), so one silence is
   *  one message. Cleared when the task writes again or the chain advances. */
  stallKey: string | null;
  /** Consecutive in-flight polls that carried no readable progress, per build:
   *  the monitor's own blindness, reported once at BLIND_POLLS. */
  blindPolls: number;
  blindBuildId: number | null;
  /** What this build's failing tests have already been told, and the one
   *  delegation already offered (`test-watch.ts`). */
  tests: TestWatch;
  stopped: boolean;
  stop: () => void;
}

function watcherState(): WatcherState | undefined {
  return (globalThis as any)[WATCHER_KEY];
}

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

/* The give-up path, the empty-poll retry and the grace between them live in
 * watcher-notice.ts (the 400-line cap): `giveUp` and `emptyPoll` are imported
 * at the top of this module. */

function terminalMessage(s: { id: number; result: string; reason: string }, definition: string): { msg: string; failed: boolean } {
  const failed = s.result === "failed";
  const label = failed ? "FAILED" : s.result;
  const reason = failed ? ` — ${s.reason || "no error lines"}` : "";
  return { msg: `AzDO build ${s.id} ${label}${reason} — ${azdoBuildUrl(definition)}`, failed };
}

function publishedVersion(repoRoot: string | undefined, versionFile: string): string | null {
  if (!repoRoot) return null;
  try {
    const text = fs.readFileSync(path.join(repoRoot, versionFile), "utf-8");
    return text.match(VERSION_RE)?.[1] ?? null;
  } catch {
    return null;
  }
}

function normalizeNugetVersion(version: string): string {
  const parts = version.split(".");
  while (parts.length > 2 && parts[parts.length - 1] === "0") parts.pop();
  return parts.join(".");
}

async function tryAssertNugets(seams: BuildSeams, p: RepoProfile, choice: "Lab" | "Release", version: string): Promise<{ ok: boolean; detail: string }> {
  if (p.nugetFeed(choice) === "nuget.org") {
    const normalized = normalizeNugetVersion(version);
    try {
      await seams.fetchFeed(nugetOrgNuspecUrl(p.nugetId, normalized));
      return { ok: true, detail: `${p.nugetId} ${normalized} found on nuget.org` };
    } catch {
      return { ok: false, detail: `${p.nugetId} ${normalized} NOT found on nuget.org` };
    }
  }
  try {
    const text = await seams.fetchFeed(nugetAssertUrl(p.nugetId));
    const versions = [...text.matchAll(NUGET_VERSION_RE)].map((m) => m[1]);
    const found = versions.some((v) => compareVersions(v, version) === 0);
    return { ok: found, detail: `${p.nugetId} ${version} ${found ? "found" : "NOT found"} on the eXpand nuget server` };
  } catch (err) {
    return { ok: false, detail: `feed query failed: ${err instanceof Error ? err.message : String(err)}` };
  }
}

async function assertNugets(ctx: any, seams: BuildSeams, repoRoot: string | undefined, choice: "Lab" | "Release", retries: number, retryMs: number): Promise<{ ok: boolean; detail: string }> {
  const p = profileOf(seams);
  const version = publishedVersion(repoRoot, p.versionFile);
  if (!version) {
    return { ok: false, detail: `no version read from ${p.versionFile}` };
  }
  let last: { ok: boolean; detail: string } | null = null;
  for (let i = 0; i < retries; i++) {
    last = await tryAssertNugets(seams, p, choice, version);
    if (last.ok) return last;
    if (i < retries - 1) await sleep(retryMs);
  }
  return last!;
}

/** The chain's last step. The release attempt itself (token, draft PATCH, retry
 *  loop) lives in github-release.ts; this is the watcher's half — which repo,
 *  which policy, and where the version comes from. */
async function publishGitHubRelease(ctx: any, seams: BuildSeams, repoRoot: string | undefined, opts: AzDoWatcherOptions): Promise<{ ok: boolean; detail: string; version: string | null }> {
  const p = profileOf(seams);
  const choice: Choice = opts.choice === "Release" ? "Release" : "Lab";
  const version = publishedVersion(repoRoot, p.versionFile);
  if (!version) return { ok: false, detail: `no version read from ${p.versionFile}`, version: null };
  const out = await publishRelease({
    gh: seams.ghFetch ?? defaultGhFetch,
    repo: p.githubRepo(choice),
    onSuccess: p.githubOnSuccess(choice),
    prerelease: choice !== "Release",
    version,
    token: opts.ghToken ?? process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN,
    attempts: opts.ghRetries ?? 6,
    delayMs: opts.ghRetryMs ?? 30_000,
  });
  return { ok: out.kind === "published", detail: out.detail, version };
}

async function advanceStep(state: WatcherState, ctx: any, s: { id: number; result: string }, next: ChainStep): Promise<void> {
  const previous = state.chain[state.step];
  state.minId = s.id;
  state.step++;
  state.startedAt = Date.now();
  state.stallKey = null;
  state.blindPolls = 0;
  state.blindBuildId = null;
  await ctx.ui.notify(`${previous.label} ${s.id} succeeded — watching the ${next.label}…`, "info");
}

function failReason(s: { reason: string }, stdout: string): string {
  return s.reason || extractFailReason(failLogFromStdout(stdout));
}

async function handleSucceeded(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, s: { id: number; result: string }): Promise<void> {
  const step = state.chain[state.step];
  if (state.step < state.chainLength - 1) {
    if (step.assertNugets) {
      const assert = await assertNugets(ctx, seams, opts.repoRoot, opts.choice ?? "Lab", opts.nugetRetries ?? 10, opts.nugetRetryMs ?? 30_000);
      if (assert.ok) {
        await ctx.ui.notify(`Nugets published: ${assert.detail}`, "info");
      } else {
        const msg = `Nugets NOT confirmed: ${assert.detail}`;
        await ctx.ui.notify(msg, "warning");
        pi.sendUserMessage(msg, { deliverAs: "steer" });
      }
    }
    await advanceStep(state, ctx, s, state.chain[state.step + 1]);
    return;
  }
  const gh = await publishGitHubRelease(ctx, seams, opts.repoRoot, opts);
  if (gh.ok) {
    await ctx.ui.notify(`${step.label} ${s.id} succeeded — ${gh.detail} — chain complete.`, "info");
  } else {
    const msg = `${step.label} ${s.id} succeeded but the GitHub release was NOT confirmed: ${gh.detail}`;
    await ctx.ui.notify(msg, "warning");
    pi.sendUserMessage(msg, { deliverAs: "steer" });
  }
  state.stop();
}

async function handleTerminal(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, s: { id: number; result: string; reason: string }, stdout: string): Promise<void> {
  if (s.result === "succeeded") {
    await handleSucceeded(state, pi, ctx, seams, opts, s);
    return;
  }
  const step = state.chain[state.step];
  const { msg, failed } = terminalMessage({ ...s, reason: failReason(s, stdout) }, step.definition);
  await ctx.ui.notify(msg, failed ? "warning" : "info");
  state.stop();
  if (failed) pi.sendUserMessage(msg, { deliverAs: "steer" });
}

/** The head pipeline is named `<version>-<dxVersion>` (4.261.3.1-26.1.3) —
 *  only its leading version token is the package version. */
function buildVersion(buildNumber: string): string {
  return buildNumber.match(/^\d+(?:\.\d+)+/)?.[0] ?? buildNumber;
}

function thisRun(s: { buildNumber: string }, expected: string | null): boolean {
  if (!s.buildNumber || !expected) return true;
  return compareVersions(buildVersion(s.buildNumber), expected) === 0;
}

/** The in-flight poll's line: informational, once per poll, no steer. It names
 *  the task the build is inside, because that is the thing a reader needs when
 *  a run outlives its estimate. */
async function notifyRunning(ctx: any, state: WatcherState, s: AzDoStatus, step: ChainStep): Promise<void> {
  const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
  state.lastId = s.id;
  const where = s.progress?.kind === "running" ? ` — ${s.progress.activeName}` : "";
  await ctx.ui.notify(`AzDO ${s.id}: ${s.status} (${elapsed} min)${where} — ${azdoBuildUrl(step.definition)}`, "info");
}

/** The in-flight progress verdict is judged in `watcher-notice.ts`
 *  (`reportProgress`): the voices and the once-per-silence key belong with the
 *  voices, and `applyPoll` below only hands it the poll. */

/** The wall clock is a BACKSTOP, and only when a caller asks for one. The
 *  default is NO deadline: a chain whose head job may legally run 90 minutes
 *  outlives any number this module invents, and on 2026-10-25 the 120-minute
 *  default ended the watch 30 minutes before the pipeline's own verdict. What
 *  says a build is alive is progress (reportStall), not elapsed time. */
async function checkDeadline(ctx: any, state: WatcherState, opts: AzDoWatcherOptions, pi: any): Promise<boolean> {
  const maxMs = opts.maxMs;
  if (typeof maxMs !== "number") return false;
  if (Date.now() - state.startedAt <= maxMs) return false;
  await giveUp(ctx, state, `AzDO watcher gave up after ${Math.max(1, Math.round(maxMs / 60_000))} min — check /devexpress status.`, pi);
  return true;
}

/** The in-flight test poll. Detached from `applyPoll` so the chain's verdict
 *  path stays one function with one job. The watcher hands over PORTS — a shell
 *  runner and a window spawner — and `test-watch.ts` builds the whole scan. */
async function pollTests(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, s: AzDoStatus, step: ChainStep): Promise<void> {
  if (opts.watchTests === false || state.stopped) return;
  const spawnWindow = opts.spawnWindow ?? ((repo, task, launch) => defaultDelegateWindow(repo, task, launch ? { launch } : {}));
  const run = (cmd: string) => seams.run(cmd, { timeoutMs: 60_000 });
  await pollTestFails({ watch: state.tests, buildId: s.id, definition: step.definition, testRole: opts.testRole, repoRoot: opts.repoRoot, ctx, pi, stopped: () => state.stopped, spawnWindow, run });
}

async function pollTick(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions): Promise<void> {
  if (state.stopped) return;
  if (await checkDeadline(ctx, state, opts, pi)) return;
  const step = state.chain[state.step];
  let res;
  try {
    res = await seams.run(azdoStatusScript(step.definition, state.minId), { timeoutMs: 60_000 });
  } catch (err) {
    await giveUp(ctx, state, `AzDO watcher failed: ${err instanceof Error ? err.message : String(err)} — check /devexpress status.`, pi);
    return;
  }
  await applyPoll(state, pi, ctx, seams, opts, step, res);
}

async function applyPoll(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, step: ChainStep, res: { stdout: string; stderr?: string }): Promise<void> {
  const poll = parsePoll(res.stdout);
  if (!poll) {
    const err = (res.stderr || "no STATUS= line").trim().slice(-200);
    await giveUp(ctx, state, `AzDO watcher: ${err} — check /devexpress status.`, pi);
    return;
  }
  if (poll.kind === "error") {
    if (poll.failure === "auth") await refusedPoll(ctx, state, pi, poll.message);
    else await transientPoll(ctx, state, opts, pi, poll.message);
    return;
  }
  if (poll.kind === "empty") {
    await emptyPoll(ctx, state, opts, pi);
    return;
  }
  const s = poll.build;
  state.emptySince = null;
  if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
    await notifyRunning(ctx, state, s, step);
    await reportProgress(ctx, pi, state, opts, s);
    await pollTests(state, pi, ctx, seams, opts, s, step);
    return;
  }
  // Only the chain head is queued by this run, so only it can be a stale
  // finished build; the downstream date-numbered builds are fenced by the
  // minId baseline (the newest build with id > the previous step's id).
  const expected = publishedVersion(opts.repoRoot, profileOf(seams).versionFile);
  if (state.step === 0 && !thisRun(s, expected)) {
    await wrongRun(ctx, state, pi, s, expected!);
    return;
  }
  state.lastId = s.id;
  await handleTerminal(state, pi, ctx, seams, opts, s, res.stdout);
}

export function startAzDoWatcher(pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions = {}): AzDoWatcherHandle {
  stopAzDoWatcher();
  const intervalMs = opts.intervalMs ?? 60_000;
  const chain = chainFor(opts.choice ?? "Lab", seams);
  const state: WatcherState = {
    timer: null,
    startedAt: Date.now(),
    step: 0,
    chain,
    chainLength: opts.followNugets ? chain.length : 1,
    minId: 0,
    lastId: null,
    emptySince: null,
    stallKey: null,
    blindPolls: 0,
    blindBuildId: null,
    tests: newTestWatch(),
    stopped: false,
    stop: () => {
      if (state.stopped) return;
      state.stopped = true;
      if (state.timer) clearInterval(state.timer);
      if ((globalThis as any)[WATCHER_KEY] === state) delete (globalThis as any)[WATCHER_KEY];
    },
  };
  (globalThis as any)[WATCHER_KEY] = state;
  const tick = () => pollTick(state, pi, ctx, seams, opts);
  state.timer = setInterval(() => void tick(), intervalMs);
  void tick();
  return {
    stop: () => state.stop(),
    active: () => !state.stopped,
    lastBuildId: () => state.lastId,
  };
}
