/**
 * reactive-xaf-build/watcher — background AzDO watcher for the publish chain.
 *
 * Chain, nuget id, version file, GitHub repo and on-success action come
 * from RepoProfile. RX is the default. One watcher at a time. A finished
 * build whose buildNumber does not match versionFile is not this run
 * (wait, then give-up steers). Failed polls use extractFailReason.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { setTimeout as sleep } from "node:timers/promises";
import { azdoStatusScript, parseStatus, azdoBuildUrl, defaultGhFetch, extractFailReason, failLogFromStdout } from "./azdo.js";
import type { BuildSeams } from "./build.js";
import {
  rxProfile, nugetAssertUrl, nugetOrgNuspecUrl, githubReleasesUrl, githubReleaseUrl, compareVersions,
} from "./profile.js";
import type { RepoProfile, Choice, ChainStep, GithubOnSuccess } from "./profile.js";

function profileOf(seams: BuildSeams): RepoProfile {
  return seams.profile ?? rxProfile;
}

export interface AzDoWatcherOptions {
  intervalMs?: number;
  maxMs?: number;
  followNugets?: boolean;
  choice?: "Lab" | "Release";
  ghToken?: string;
  repoRoot?: string;
  ghRetries?: number;
  ghRetryMs?: number;
}

export interface AzDoWatcherHandle {
  stop: () => void;
  active: () => boolean;
  lastBuildId: () => number | null;
}

export type AzDoWatcherStarter = (pi: any, ctx: any, seams: BuildSeams, opts?: AzDoWatcherOptions) => AzDoWatcherHandle;

const WATCHER_KEY = Symbol.for("reactive-xaf-build.azdo-watcher");
const NUGET_VERSION_RE = /Version='([^']+)'/g;
const VERSION_RE = /Version\s*=\s*"([^"]+)"/;

function chainFor(choice: Choice, seams: BuildSeams): ChainStep[] {
  return profileOf(seams).chain(choice);
}

interface WatcherState {
  timer: ReturnType<typeof setInterval> | null;
  startedAt: number;
  step: number;
  chain: ChainStep[];
  chainLength: number;
  minId: number;
  lastId: number | null;
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

async function giveUp(ctx: any, state: WatcherState, msg: string, pi?: any): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  if (pi) pi.sendUserMessage(msg, { deliverAs: "steer" });
  state.stop();
}

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

async function assertNugets(ctx: any, seams: BuildSeams, repoRoot: string | undefined, choice: "Lab" | "Release"): Promise<{ ok: boolean; detail: string }> {
  const p = profileOf(seams);
  const version = publishedVersion(repoRoot, p.versionFile);
  if (!version) {
    return { ok: false, detail: `no version read from ${p.versionFile}` };
  }
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

type GhFetch = (url: string, opts?: { method?: string; body?: string }) => Promise<{ ok: boolean; status: number; text: string }>;

function ghToken(opts: AzDoWatcherOptions): string | undefined {
  return opts.ghToken ?? process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN;
}

function findRelease(releases: Array<{ id?: number; tag_name?: string; draft?: boolean }>, version: string): { draftId?: number; published?: boolean } {
  for (const r of releases) {
    if (r.tag_name !== version) continue;
    if (r.draft === true && typeof r.id === "number") return { draftId: r.id };
    if (r.draft === false) return { published: true };
  }
  return {};
}

async function publishDraft(gh: GhFetch, repo: string, id: number, prerelease: boolean): Promise<{ ok: boolean; detail?: string }> {
  const patch = await gh(githubReleaseUrl(repo, id), {
    method: "PATCH",
    body: JSON.stringify({ draft: false, prerelease }),
  });
  if (patch.ok) return { ok: true };
  return { ok: false, detail: `GitHub publish failed: HTTP ${patch.status}` };
}

type AttemptOutcome =
  | { kind: "publishedDraft"; detail: string }
  | { kind: "published"; detail: string }
  | { kind: "http"; detail: string }
  | { kind: "retry" };

async function githubAttempt(gh: GhFetch, repo: string, version: string, prerelease: boolean, onSuccess: GithubOnSuccess): Promise<AttemptOutcome> {
  const res = await gh(githubReleasesUrl(repo));
  if (!res.ok) return { kind: "http", detail: `GitHub query failed: HTTP ${res.status}` };
  const found = findRelease(JSON.parse(res.text), version);
  if (found.draftId !== undefined) {
    if (onSuccess === "assertPublished") return { kind: "retry" };
    const pub = await publishDraft(gh, repo, found.draftId, prerelease);
    if (pub.ok) {
      const kind = prerelease ? "pre-release" : "release";
      return { kind: "publishedDraft", detail: `GitHub ${kind} ${version} published from draft` };
    }
    return { kind: "http", detail: pub.detail ?? "GitHub publish failed" };
  }
  if (found.published) return { kind: "published", detail: `GitHub release ${version} already published` };
  return { kind: "retry" };
}

async function publishGitHubRelease(ctx: any, seams: BuildSeams, repoRoot: string | undefined, opts: AzDoWatcherOptions): Promise<{ ok: boolean; detail: string; version: string | null }> {
  const p = profileOf(seams);
  const choice: Choice = opts.choice === "Release" ? "Release" : "Lab";
  const version = publishedVersion(repoRoot, p.versionFile);
  if (!version) {
    return { ok: false, detail: `no version read from ${p.versionFile}`, version: null };
  }
  const token = ghToken(opts);
  if (!token) {
    return { ok: false, detail: `GH_TOKEN is not set — the draft release ${version} must be published manually on GitHub`, version };
  }
  const attempts = opts.ghRetries ?? 6;
  const delayMs = opts.ghRetryMs ?? 30_000;
  const gh = seams.ghFetch ?? defaultGhFetch;
  const prerelease = choice !== "Release";
  const repo = p.githubRepo(choice);
  const onSuccess = p.githubOnSuccess(choice);
  for (let i = 0; i < attempts; i++) {
    try {
      const out = await githubAttempt(gh, repo, version, prerelease, onSuccess);
      if (out.kind === "publishedDraft" || out.kind === "published") return { ok: true, detail: out.detail, version };
      if (out.kind === "http" && i === attempts - 1) return { ok: false, detail: out.detail, version };
    } catch (err) {
      if (i === attempts - 1) {
        return { ok: false, detail: `GitHub publish failed: ${err instanceof Error ? err.message : String(err)}`, version };
      }
    }
    if (i < attempts - 1) await sleep(delayMs);
  }
  return { ok: false, detail: `GitHub release ${version} NOT found after ${attempts} tries`, version };
}

async function advanceStep(state: WatcherState, ctx: any, s: { id: number; result: string }, next: ChainStep): Promise<void> {
  const previous = state.chain[state.step];
  state.minId = s.id;
  state.step++;
  state.startedAt = Date.now();
  await ctx.ui.notify(`${previous.label} ${s.id} succeeded — watching the ${next.label}…`, "info");
}

function failReason(s: { reason: string }, stdout: string): string {
  return s.reason || extractFailReason(failLogFromStdout(stdout));
}

async function handleSucceeded(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, s: { id: number; result: string }): Promise<void> {
  const step = state.chain[state.step];
  if (state.step < state.chainLength - 1) {
    if (step.assertNugets) {
      const assert = await assertNugets(ctx, seams, opts.repoRoot, opts.choice ?? "Lab");
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

function thisRun(s: { buildNumber: string }, expected: string | null): boolean {
  if (!s.buildNumber || !expected) return true;
  return compareVersions(s.buildNumber, expected) === 0;
}

async function notifyWrongVersion(ctx: any, state: WatcherState, s: { id: number; buildNumber: string }, expected: string): Promise<void> {
  const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
  await ctx.ui.notify(`AzDO ${s.id} ${s.buildNumber} is not this run (${expected}) — waiting (${elapsed} min)…`, "info");
}

async function checkDeadline(ctx: any, state: WatcherState, opts: AzDoWatcherOptions, pi: any): Promise<boolean> {
  const maxMs = opts.maxMs ?? 7_200_000;
  if (Date.now() - state.startedAt <= maxMs) return false;
  await giveUp(ctx, state, `AzDO watcher gave up after ${Math.round(maxMs / 60_000)} min — check /devexpress status.`, pi);
  return true;
}

async function notifyEmptyPoll(ctx: any, state: WatcherState): Promise<void> {
  const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
  await ctx.ui.notify(`AzDO: no build found yet (${elapsed} min) — waiting…`, "info");
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
  const s = parseStatus(res.stdout);
  if (!s) {
    const err = (res.stderr || "no STATUS= line").trim().slice(-200);
    await giveUp(ctx, state, `AzDO watcher: ${err} — check /devexpress status.`, pi);
    return;
  }
  if (s.id === 0) {
    await notifyEmptyPoll(ctx, state);
    return;
  }
  if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
    const elapsed = Math.round((Date.now() - state.startedAt) / 60_000);
    state.lastId = s.id;
    await ctx.ui.notify(`AzDO ${s.id}: ${s.status} (${elapsed} min) — ${azdoBuildUrl(step.definition)}`, "info");
    return;
  }
  const expected = publishedVersion(opts.repoRoot, profileOf(seams).versionFile);
  if (!thisRun(s, expected)) {
    await notifyWrongVersion(ctx, state, s, expected!);
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
