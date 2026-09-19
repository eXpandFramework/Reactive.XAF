/**
 * reactive-xaf-build/build — the /devexpress workflow engine.
 *
 * DX check → optional depPins → the build started in a pane and watched out of
 * band (run.ts) → publish when the marker reports green. Menu pick RX-XAF |
 * eXpand switches the profile and finds that tree.
 *
 * This module is pure engine: it imports no surface module. menu.ts drives it
 * through the runners built by `createFlowRunners`, which is what keeps the
 * dependency pointing surface → engine.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import {
  runProcess, getBuildPane, setBuildPane, defaultOpenBuildPane, defaultRunInPane, defaultCapturePane,
  defaultClosePane, defaultProbePane, defaultSampleCpu,
} from "./pane.js";
import type {
  RunResult, RunOpts, PaneOpener, PaneRunner, PaneCapturer, PaneCloser, PaneProber, CpuSampler,
} from "./pane.js";
import {
  startBuildRun, watchInProcessRun, activeBuildRun, newRunId, runPaths, writeRunScript, supervisorCommand,
  pruneRunDirs, trackedWrite,
} from "./run.js";
import type { BuildRunSeams, RunCadence, RunPaths, RunReporter } from "./run.js";
import { finishMessage, steerStarted, steerWarning, steerWatch, summaryResult, warn, watchMessage } from "./report.js";
import { startAzDoWatcher } from "./watcher.js";
import type { AzDoWatcherStarter } from "./watcher.js";
import { defaultGhFetch } from "./azdo.js";
import { rxProfile, compareVersions, profileOf, profileByPick, resolveRepo } from "./profile.js";
import type { RepoProfile, Choice } from "./profile.js";
import { releaseVersionTarget } from "./release.js";
import { depPinsPhase } from "./pins.js";
import { prewarmVms, publishPhase } from "./publish.js";

export type { RunResult } from "./pane.js";
export { profileOf };

export type CommandRunner = (cmd: string, opts?: RunOpts) => Promise<RunResult>;
export type FeedFetcher = (url: string) => Promise<string>;

export interface BuildSeams {
  run: CommandRunner;
  fetchFeed: FeedFetcher;
  ghFetch?: (url: string, opts?: { method?: string; body?: string }) => Promise<{ ok: boolean; status: number; text: string }>;
  propsPath?: string;
  repoRoot?: string;
  pollMs?: number;
  openBuildPane?: PaneOpener;
  runInPane?: PaneRunner;
  capturePane?: PaneCapturer;
  closePane?: PaneCloser;
  probePane?: PaneProber;
  sampleCpu?: CpuSampler;
  runPaths?: (runId: string) => RunPaths;
  runCadence?: Partial<RunCadence>;
  startAzDoWatcher?: AzDoWatcherStarter;
  profile?: RepoProfile;
}

/** What menu.ts calls to run a build or publish flow. */
export type FlowRunner = (choice: string, skipBuild?: boolean, projectPick?: string) => Promise<string>;
/** What menu.ts calls for "Start AzDO watcher". */
export type WatchStarter = (choice: string, projectPick?: string) => Promise<string>;

const DX_FEED_URL = "https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json";
const DX_PIN_RE = /Include="(DevExpress\.[^"]*)"\s+Version="([^"]*)"/g;

export function defaultSeams(): BuildSeams {
  return {
    run: runProcess,
    fetchFeed: async (url: string) => {
      const res = await globalThis.fetch(url, { headers: { "User-Agent": "rxaf-watcher" } });
      if (!res.ok) throw new Error(`feed query failed: HTTP ${res.status}`);
      return res.text();
    },
    ghFetch: defaultGhFetch,
    openBuildPane: defaultOpenBuildPane,
    runInPane: defaultRunInPane,
    capturePane: defaultCapturePane,
    closePane: defaultClosePane,
    probePane: defaultProbePane,
    sampleCpu: defaultSampleCpu,
    startAzDoWatcher,
  };
}

export function repoRootOf(cwd: string, profile: RepoProfile = rxProfile): string | null {
  return resolveRepo(profile, cwd);
}

export async function getLatestDx(fetchFeed: FeedFetcher): Promise<string> {
  const text = await fetchFeed(DX_FEED_URL);
  const versions = (JSON.parse(text).versions as string[] | undefined) ?? [];
  const stable = versions.filter((v) => /^\d+\.\d+\.\d+$/.test(v));
  if (!stable.length) throw new Error("no stable DevExpress.ExpressApp versions on nuget.org");
  stable.sort((a, b) => compareVersions(b, a));
  return stable[0];
}

export function readDxPins(text: string): { count: number; unique: string | null } {
  const versions = new Set<string>();
  let m: RegExpExecArray | null;
  DX_PIN_RE.lastIndex = 0;
  while ((m = DX_PIN_RE.exec(text)) !== null) versions.add(m[2]);
  return { count: versions.size, unique: versions.size === 1 ? [...versions][0] : null };
}

export function rewriteDxVersion(text: string, newVersion: string): string {
  return text.replace(/(Include="DevExpress\.[^"]*"\s+Version=")[^"]*(")/g, `$1${newVersion}$2`);
}

/** Rewrite build.ps1's `-version "X.Y.Z.W"` when it mismatches the target. */
function writeBuildVersion(text: string, version: string): string {
  return text.replace(/-version "(\d+\.\d+\.\d+\.\d+)"/, `-version "${version}"`);
}

/** Rewrite build.ps1's -version when it mismatches the target version.
 *  Returns true when the file changed; a correct version is never touched. */
async function bumpBuildPs1(repoRoot: string, dxVersion: string, notes: string[], seams: BuildSeams, choice: string): Promise<boolean> {
  const buildPs1 = path.join(repoRoot, "build.ps1");
  if (!fs.existsSync(buildPs1)) return false;
  const bp = fs.readFileSync(buildPs1, "utf-8");
  const target = await releaseVersionTarget(seams, dxVersion, choice);
  if (!target) return false;
  const bumped = writeBuildVersion(bp, target);
  if (bumped === bp) return false;
  trackedWrite(buildPs1, bumped);
  notes.push(`bumped build.ps1 -version to ${target}`);
  return true;
}

async function dxPhase(ctx: any, seams: BuildSeams, propsPath: string, latest: string, choice: string): Promise<{ changed: boolean; notes: string[] }> {
  const text = fs.readFileSync(propsPath, "utf-8");
  const { count, unique } = readDxPins(text);
  const notes: string[] = [];
  if (count === 0) {
    notes.push("no DevExpress.* pins found in Directory.Packages.props");
    return { changed: false, notes };
  }
  if (unique === null) {
    notes.push(`DX pins are mixed (${count} versions) — file left untouched`);
    return { changed: false, notes };
  }
  if (unique === latest) {
    notes.push(`DX already at latest (${latest})`);
    const repaired = await bumpBuildPs1(path.dirname(propsPath), latest, notes, seams, choice);
    return { changed: repaired, notes };
  }
  const pick = await ctx.ui.select(`DX ${unique} → ${latest}: update all DevExpress.* pins?`, ["Update", "Skip", "Abort"]);
  if (pick === "Abort") throw new Error("aborted at the DX update prompt");
  if (pick === "Skip") {
    notes.push(`kept DX ${unique} (latest on feed: ${latest})`);
    return { changed: false, notes };
  }
  trackedWrite(propsPath, rewriteDxVersion(text, latest));
  notes.push(`updated all DevExpress.* pins ${unique} → ${latest}`);
  await bumpBuildPs1(path.dirname(propsPath), latest, notes, seams, choice);
  return { changed: true, notes };
}

/** The started build: its identity plus where the output and the exit code
 *  land. Returned as soon as the pane has the command, never after the build. */
interface StartedBuild {
  runId: string;
  pane: string | null;
  marker: string;
  command: string;
}

/** The watch's seams, taken from the flow's seam bag. */
function runSeamsOf(seams: BuildSeams): BuildRunSeams {
  return {
    probe: seams.probePane,
    capture: seams.capturePane,
    close: seams.closePane,
    sampleCpu: seams.sampleCpu,
    cadence: seams.runCadence,
  };
}

/** Write the supervisor script, open the pane, hand it the command, and let
 *  go. The outcome arrives through `report`; nothing here awaits the build. */
async function startBuildPhase(
  ctx: any, seams: BuildSeams, choice: string, repo: string, report: RunReporter,
): Promise<StartedBuild> {
  const cmd = profileOf(seams).buildCmd(choice as Choice);
  const paths = (seams.runPaths ?? runPaths)(newRunId());
  writeRunScript(paths, cmd);
  pruneRunDirs();
  const started: StartedBuild = { runId: paths.runId, pane: null, marker: paths.marker, command: cmd };
  const pane = await (seams.openBuildPane ?? defaultOpenBuildPane)(repo);
  if (!pane) {
    await ctx.ui.notify("Build pane could not be opened — building in-process.", "warning");
    setBuildPane(null);
    watchInProcessRun(seams.run(cmd, { cwd: repo, timeoutMs: 3_600_000 }), report);
    return started;
  }
  started.pane = pane;
  setBuildPane(pane);
  await (seams.runInPane ?? defaultRunInPane)(pane, supervisorCommand(paths));
  startBuildRun({ runId: paths.runId, pane, marker: paths.marker, startedAt: Date.now() }, runSeamsOf(seams), report);
  return started;
}

function startedMessage(id: string, choice: string, latest: string, notes: string[], started: StartedBuild): string {
  const dxLine = latest ? `DX latest: ${latest}` : "no DX check";
  const where = started.pane
    ? `Build started in pane ${started.pane} (run ${started.runId})`
    : `Build started in-process (run ${started.runId})`;
  return [`${id} build — ${choice}`, dxLine, ...notes, `${where}. The outcome lands here when it ends.`].join("\n");
}

/** The flow's half of the watch: green continues into publish, everything else
 *  reports through the toast plus steer pair. */
function buildRunReporter(
  pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string,
  notes: string[], dxChanged: boolean, latest: string, id: string,
): RunReporter {
  return async (event) => {
    const backstop = watchMessage(event, id);
    if (backstop) {
      await warn(pi, ctx, backstop);
      return;
    }
    if (event.kind === "done" && event.code === 0) {
      notes.push(`build succeeded (${choice})`);
      try {
        await finishPublish(pi, ctx, seams, choice, repo, notes, dxChanged, latest, id, false);
      } catch (err) {
        const detail = err instanceof Error ? err.message : String(err);
        await warn(pi, ctx, `${id} publish threw after a green build: ${detail}\n${notes.join("\n")}`);
      }
      return;
    }
    if (event.kind === "done" || event.kind === "died") {
      await warn(pi, ctx, finishMessage(event, id, choice, latest, notes, event.tail));
      return;
    }
    throw new Error(`reactive-xaf-build: unhandled run event ${event.kind}`);
  };
}

/** DX check plus pin rewrites: everything that runs before the build itself. */
async function runLocalBuild(
  ctx: any, seams: BuildSeams, choice: string, repo: string, notes: string[],
): Promise<{ dxChanged: boolean; latest: string }> {
  const latest = await getLatestDx(seams.fetchFeed);
  const propsPath = seams.propsPath ?? path.join(repo, "Directory.Packages.props");
  const dx = await dxPhase(ctx, seams, propsPath, latest, choice);
  notes.push(...dx.notes);
  const pins = await depPinsPhase(ctx, seams, propsPath, choice);
  notes.push(...pins.notes);
  return { dxChanged: dx.changed || pins.changed, latest };
}

async function finishPublish(
  pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string,
  notes: string[], dxChanged: boolean, latest: string, id: string, skipBuild: boolean,
): Promise<string> {
  const pub = await publishPhase(pi, ctx, seams, choice, repo, dxChanged, latest, skipBuild);
  notes.push(...pub.notes);
  const pane = getBuildPane();
  const closeAsk = pane ? `\nThe build pane ${pane} is left open — close it via /devexpress → "Close build pane" when done.` : "";
  const msg = summaryResult(id, choice, latest, notes, pub.ok) + closeAsk;
  await ctx.ui.notify(msg, "info");
  if (!pub.ok && pub.failed) steerWarning(pi, msg);
  return msg;
}

/** Boot the agents while the build runs: the publish gate then finds them
 *  Running instead of meeting a cold start at the queue. A flow that aborts
 *  before this point (DX prompt, feed consultation) leaves the VMs alone. The
 *  pre-warm never fails the build: a VM layer that needs attention steers a
 *  warning that spends no model turn. */
async function prewarmPhase(pi: any, seams: BuildSeams, notes: string[], id: string, choice: string): Promise<void> {
  const pre = await prewarmVms(seams);
  notes.push(...pre.notes);
  if (pre.attention) steerWatch(pi, `${id} build — ${choice}\n${pre.notes.join("\n")}`);
}

async function runBuildFlow(pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string, skipBuild = false): Promise<string> {
  const id = profileOf(seams).label;
  try {
    const notes: string[] = [];
    if (skipBuild) {
      notes.push("build skipped — publish only");
      return finishPublish(pi, ctx, seams, choice, repo, notes, false, "", id, true);
    }
    const running = activeBuildRun();
    if (running) {
      return `${id} build is already running in pane ${running.pane ?? "the build host"} — stop it with /devexpress → "Abort build" first.`;
    }
    const local = await runLocalBuild(ctx, seams, choice, repo, notes);
    await prewarmPhase(pi, seams, notes, id, choice);
    const report = buildRunReporter(pi, ctx, seams, choice, repo, notes, local.dxChanged, local.latest, id);
    const started = await startBuildPhase(ctx, seams, choice, repo, report);
    const msg = startedMessage(id, choice, local.latest, notes, started);
    await ctx.ui.notify(msg, "info");
    steerStarted(pi, msg);
    return msg;
  } catch (err) {
    const detail = err instanceof Error ? err.message : String(err);
    const msg = `${id} build aborted: ${detail}`;
    await ctx.ui.notify(msg, "warning");
    if (!detail.includes("aborted")) steerWarning(pi, msg);
    return msg;
  }
}

export function seamsForPick(merged: BuildSeams, projectPick: string | undefined, cwd: string): { seams: BuildSeams; repo: string | null } {
  if (!projectPick) {
    const p = profileOf(merged);
    return { seams: merged, repo: repoRootOf(cwd, p) };
  }
  const p = profileByPick(projectPick);
  return { seams: { ...merged, profile: p }, repo: resolveRepo(p, cwd) };
}

function missingRepo(p: RepoProfile, cwd: string): string {
  return `${p.label} build: not inside the ${p.label} repo (cwd: ${cwd}) — no commands ran.`;
}

/** Start the AzDO chain watcher for a build (menu item; picked like Build). */
async function watchPhase(pi: any, ctx: any, seams: BuildSeams, repo: string, choice: string): Promise<string> {
  startAzDoWatcher(pi, ctx, seams, { followNugets: true, repoRoot: repo, choice: choice === "Release" ? "Release" : "Lab" });
  await ctx.ui.notify("AzDO watcher started — it follows the build, the nuget publish and the release consumers chain, toasting on every check.", "info");
  return "AzDO watcher started in the background — toasts on every check, nuget assertion on the eXpand server at the nugets step, release consumers watched last.";
}

/** The two entries menu.ts drives: a flow run and the watcher start. */
export function createFlowRunners(
  pi: any, ctx: any, merged: BuildSeams, cwd: string,
): { runFlow: FlowRunner; startWatch: WatchStarter } {
  const runFlow: FlowRunner = (choice: string, skipBuild = false, projectPick?: string) => {
    const { seams: s, repo } = seamsForPick(merged, projectPick, cwd);
    if (!repo) return Promise.resolve(missingRepo(profileOf(s), cwd));
    return runBuildFlow(pi, ctx, s, choice, repo, skipBuild);
  };
  const startWatch: WatchStarter = (choice: string, projectPick?: string) => {
    const { seams: s, repo } = seamsForPick(merged, projectPick, cwd);
    if (!repo) return Promise.resolve(missingRepo(profileOf(s), cwd));
    return watchPhase(pi, ctx, s, repo, choice);
  };
  return { runFlow, startWatch };
}
