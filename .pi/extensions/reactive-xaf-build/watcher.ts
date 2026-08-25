/**
 * reactive-xaf-build/watcher — background AzDO watcher for the full publish
 * chain.
 *
 * After prx queues a build, monitorPhase starts a watcher instead of blocking
 * the agent turn: a timer polls the chain's current pipeline via
 * azdoStatusScript and NOTIFIES ON EVERY CHECK (the user asked for a toast
 * each time the build is looked at — same-type notifies self-replace, so the
 * toast acts as a live status line). With followNugets the watcher walks the
 * whole chain — Reactive.XAF (def 23) → PublishNugets (def 72) → release
 * consumers (def 89) — advancing when each pipeline succeeds (each next
 * build is the newest with id > the finished build's id). At the nugets step
 * the watcher ASSERTS the nugets landed on the eXpand nuget server
 * (xpandnugetserver.azurewebsites.net — the lab feed; version read from
 * AssemblyInfoVersion.cs, checked against the v2 FindPackagesById OData
 * feed). At the final step it ASSERTS the GitHub PRE-RELEASE for the lab
 * build (tag = the same version, prerelease=true), retrying a few times to
 * absorb the release-creation race. A failed pipeline — or a failed
 * assertion — steers via pi.sendUserMessage (deliverAs "steer"), the same
 * turn-independent failure path steerFailure uses.
 *
 * Registry lives on globalThis (Symbol key) — module-level state would
 * duplicate on Windows path-casing double loads. One watcher at a time:
 * starting a new one stops the previous.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { setTimeout as sleep } from "node:timers/promises";
import { azdoStatusScript, parseStatus, AZDO_BUILD_URL } from "./azdo.js";
import type { BuildSeams } from "./build.js";

export interface AzDoWatcherOptions {
  /** Poll interval (default 60 s). */
  intervalMs?: number;
  /** Give-up deadline per chain step (default 2 h). */
  maxMs?: number;
  /** Walk the publish chain (def 23 → 72 → 89) and assert the nugets +
   *  GitHub pre-release. Off = watch only the Reactive.XAF build. */
  followNugets?: boolean;
  /** Repo root — the assertions read the version from
   *  src/Common/AssemblyInfoVersion.cs. */
  repoRoot?: string;
  /** GitHub pre-release assertion attempts (default 6) and delay (default 30 s). */
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
/** The eXpand nuget server (lab feed) — v2 OData FindPackagesById. */
const NUGET_ASSERT_URL = "https://xpandnugetserver.azurewebsites.net/nuget/FindPackagesById()?id=%27xpand.extensions%27";
const NUGET_VERSION_RE = /Version='([^']+)'/g;
/** The GitHub releases API for the lab pre-releases. */
const GITHUB_RELEASES_URL = "https://api.github.com/repos/eXpandFramework/Reactive.XAF/releases?per_page=20";
const VERSION_RE = /Version\s*=\s*"([^"]+)"/;

interface ChainStep {
  definition: string;
  label: string;
  assertNugets?: boolean;
}

/** The publish chain: Reactive.XAF → PublishNugets → release consumers. */
const CHAIN: ChainStep[] = [
  { definition: "23", label: "Reactive.XAF build" },
  { definition: "72", label: "nuget publish pipeline", assertNugets: true },
  { definition: "89", label: "release consumers pipeline" },
];

interface WatcherState {
  timer: ReturnType<typeof setInterval> | null;
  startedAt: number;
  step: number;
  chainLength: number;
  /** Only builds with id > minId are considered (the chain already passed the rest). */
  minId: number;
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

/** Read the version the build published from src/Common/AssemblyInfoVersion.cs. */
function publishedVersion(repoRoot: string | undefined): string | null {
  if (!repoRoot) return null;
  try {
    const text = fs.readFileSync(path.join(repoRoot, "src", "Common", "AssemblyInfoVersion.cs"), "utf-8");
    return text.match(VERSION_RE)?.[1] ?? null;
  } catch {
    return null;
  }
}

/** Assert the nugets landed: the version must appear on the eXpand server. */
async function assertNugets(ctx: any, seams: BuildSeams, repoRoot: string | undefined): Promise<{ ok: boolean; detail: string }> {
  const version = publishedVersion(repoRoot);
  if (!version) {
    return { ok: false, detail: "no version read from AssemblyInfoVersion.cs" };
  }
  try {
    const text = await seams.fetchFeed(NUGET_ASSERT_URL);
    const versions = [...text.matchAll(NUGET_VERSION_RE)].map((m) => m[1]);
    const found = versions.includes(version);
    return { ok: found, detail: `xpand.extensions ${version} ${found ? "found" : "NOT found"} on the eXpand nuget server` };
  } catch (err) {
    return { ok: false, detail: `feed query failed: ${err instanceof Error ? err.message : String(err)}` };
  }
}

/** Assert the GitHub pre-release for the lab build; retries absorb the
 *  release-creation race (the release lags the build completion). */
async function assertGitHubRelease(ctx: any, seams: BuildSeams, repoRoot: string | undefined, opts: AzDoWatcherOptions): Promise<{ ok: boolean; detail: string; version: string | null }> {
  const version = publishedVersion(repoRoot);
  if (!version) {
    return { ok: false, detail: "no version read from AssemblyInfoVersion.cs", version: null };
  }
  const attempts = opts.ghRetries ?? 6;
  const delayMs = opts.ghRetryMs ?? 30_000;
  for (let i = 0; i < attempts; i++) {
    try {
      const text = await seams.fetchFeed(GITHUB_RELEASES_URL);
      const releases = JSON.parse(text) as Array<{ tag_name?: string; prerelease?: boolean }>;
      if (releases.some((r) => r.prerelease === true && r.tag_name === version)) {
        return { ok: true, detail: `GitHub pre-release ${version} found`, version };
      }
    } catch (err) {
      if (i === attempts - 1) {
        return { ok: false, detail: `GitHub query failed: ${err instanceof Error ? err.message : String(err)}`, version };
      }
    }
    if (i < attempts - 1) await sleep(delayMs);
  }
  return { ok: false, detail: `GitHub pre-release ${version} NOT found after ${attempts} tries`, version };
}

/** Advance to the next chain step after a succeeded build. */
async function advanceStep(state: WatcherState, ctx: any, s: { id: number; result: string }, next: ChainStep): Promise<void> {
  const previous = CHAIN[state.step];
  state.minId = s.id;
  state.step++;
  state.startedAt = Date.now();
  await ctx.ui.notify(`${previous.label} ${s.id} succeeded — watching the ${next.label}…`, "info");
}

/** Terminal handling for every chain step: assertion, advance, steer. */
async function handleTerminal(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions, s: { id: number; result: string; reason: string }): Promise<void> {
  const step = CHAIN[state.step];
  if (s.result === "succeeded" && state.step < state.chainLength - 1) {
    if (step.assertNugets) {
      const assert = await assertNugets(ctx, seams, opts.repoRoot);
      if (assert.ok) {
        await ctx.ui.notify(`Nugets published: ${assert.detail}`, "info");
      } else {
        const msg = `Nugets NOT confirmed: ${assert.detail}`;
        await ctx.ui.notify(msg, "warning");
        pi.sendUserMessage(msg, { deliverAs: "steer" });
      }
    }
    await advanceStep(state, ctx, s, CHAIN[state.step + 1]);
    return;
  }
  if (s.result === "succeeded") {
    const gh = await assertGitHubRelease(ctx, seams, opts.repoRoot, opts);
    if (gh.ok) {
      await ctx.ui.notify(`${step.label} ${s.id} succeeded — ${gh.detail} — chain complete.`, "info");
    } else {
      const msg = `${step.label} ${s.id} succeeded but the GitHub pre-release was NOT confirmed: ${gh.detail}`;
      await ctx.ui.notify(msg, "warning");
      pi.sendUserMessage(msg, { deliverAs: "steer" });
    }
    state.stop();
    return;
  }
  const { msg, failed } = terminalMessage(s);
  await ctx.ui.notify(msg, failed ? "warning" : "info");
  state.stop();
  if (failed) pi.sendUserMessage(msg, { deliverAs: "steer" });
}

/** One poll tick: query, toast, stop or advance on terminal. */
async function pollTick(state: WatcherState, pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions): Promise<void> {
  if (state.stopped) return;
  const maxMs = opts.maxMs ?? 7_200_000;
  if (Date.now() - state.startedAt > maxMs) {
    await giveUp(ctx, state, `AzDO watcher gave up after ${Math.round(maxMs / 60_000)} min — check /devexpress status.`);
    return;
  }
  const step = CHAIN[state.step];
  let res;
  try {
    res = await seams.run(azdoStatusScript(step.definition, state.minId), { timeoutMs: 60_000 });
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
  await handleTerminal(state, pi, ctx, seams, opts, s);
}

/** Start the background watcher. The previous watcher, if any, is stopped. */
export function startAzDoWatcher(pi: any, ctx: any, seams: BuildSeams, opts: AzDoWatcherOptions = {}): AzDoWatcherHandle {
  stopAzDoWatcher();
  const intervalMs = opts.intervalMs ?? 60_000;
  const state: WatcherState = {
    timer: null,
    startedAt: Date.now(),
    step: 0,
    chainLength: opts.followNugets ? CHAIN.length : 1,
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
