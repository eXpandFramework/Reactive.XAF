/**
 * reactive-xaf-build/build — the /devexpress workflow engine.
 *
 * DX check → optional depPins → profile.buildCmd in a pane → publish.ts.
 * Menu pick RX-XAF | eXpand switches the profile and finds that tree.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import {
  runProcess, getBuildPane, setBuildPane, exitMarkerPath,
  defaultOpenBuildPane, defaultRunInPane, defaultWaitForPaneExit,
  defaultCapturePane, defaultClosePane,
} from "./pane.js";
import type { RunResult, PaneOpener, PaneRunner, PaneWaiter, PaneCapturer, PaneCloser } from "./pane.js";
import { startAzDoWatcher } from "./watcher.js";
import type { AzDoWatcherStarter } from "./watcher.js";
import { defaultGhFetch } from "./azdo.js";
import { runDevexpressMenu } from "./menu.js";
import { rxProfile, compareVersions, profileOf, profileByPick, resolveRepo } from "./profile.js";
import type { RepoProfile, Choice } from "./profile.js";
import { depPinsPhase } from "./pins.js";
import { publishPhase } from "./publish.js";

export type { RunResult } from "./pane.js";
export { profileOf };

export type CommandRunner = (cmd: string, opts?: { cwd?: string; timeoutMs?: number }) => Promise<RunResult>;
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
  waitForPaneExit?: PaneWaiter;
  capturePane?: PaneCapturer;
  closePane?: PaneCloser;
  startAzDoWatcher?: AzDoWatcherStarter;
  profile?: RepoProfile;
}

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
    waitForPaneExit: defaultWaitForPaneExit,
    capturePane: defaultCapturePane,
    closePane: defaultClosePane,
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

function trackedWrite(file: string, data: string): void {
  const seam = (globalThis as any).__writeFileSync;
  if (typeof seam !== "function") throw new Error("__writeFileSync seam missing — pi-dev not loaded");
  seam(file, data);
}

function tail(s: string, n = 1500): string {
  const t = s.trim();
  return t.length <= n ? t : "..." + t.slice(-n);
}

async function dxPhase(ctx: any, seams: BuildSeams, propsPath: string, latest: string): Promise<{ changed: boolean; notes: string[] }> {
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
    return { changed: false, notes };
  }
  const pick = await ctx.ui.select(`DX ${unique} → ${latest}: update all DevExpress.* pins?`, ["Update", "Skip", "Abort"]);
  if (pick === "Abort") throw new Error("aborted at the DX update prompt");
  if (pick === "Skip") {
    notes.push(`kept DX ${unique} (latest on feed: ${latest})`);
    return { changed: false, notes };
  }
  trackedWrite(propsPath, rewriteDxVersion(text, latest));
  notes.push(`updated all DevExpress.* pins ${unique} → ${latest}`);
  return { changed: true, notes };
}

async function buildPhase(ctx: any, seams: BuildSeams, choice: string, repo: string): Promise<{ code: number; stdout: string }> {
  const marker = exitMarkerPath();
  const cmd = profileOf(seams).buildCmd(choice as Choice);
  const pane = await (seams.openBuildPane ?? defaultOpenBuildPane)(repo);
  if (!pane) {
    await ctx.ui.notify("Build pane could not be opened — building in-process.", "warning");
    const res = await seams.run(cmd, { cwd: repo, timeoutMs: 3600000 });
    return { code: res.code, stdout: res.stdout + res.stderr };
  }
  setBuildPane(pane);
  await ctx.ui.notify(`Build started — pane ${pane} on the right.`, "info");
  const wrapped = `${cmd}; if ($?) { Set-Content -LiteralPath '${marker}' -Value 0 } else { Set-Content -LiteralPath '${marker}' -Value 1 }`;
  await (seams.runInPane ?? defaultRunInPane)(pane, wrapped);
  const wait = await (seams.waitForPaneExit ?? defaultWaitForPaneExit)(pane, marker, 3600000);
  if (wait.timedOut) {
    await (seams.closePane ?? defaultClosePane)(pane);
    setBuildPane(null);
    return { code: -1, stdout: "build timed out after 60 minutes — pane closed" };
  }
  const captured = await (seams.capturePane ?? defaultCapturePane)(pane);
  return { code: wait.code ?? -1, stdout: captured };
}

function failureResult(id: string, choice: string, latest: string, notes: string[], build: { code: number; stdout: string }): string {
  return [
    `${id} build — ${choice}`,
    `DX latest: ${latest}`,
    ...notes,
    `Build FAILED (exit ${build.code})`,
    "--- output tail (from the build pane) ---",
    tail(build.stdout, 4000),
    "Fix the warnings, then re-run /devexpress.",
  ].join("\n");
}

function summaryResult(id: string, choice: string, latest: string, notes: string[], pubOk: boolean): string {
  const dxLine = latest ? `DX latest: ${latest}` : "no DX check (build skipped)";
  return [`${id} build — ${choice}`, dxLine, ...notes, pubOk ? "published" : "publish stopped"].join("\n");
}

function steerFailure(pi: any, msg: string): void {
  pi.sendUserMessage(msg, { deliverAs: "steer" });
}

async function runLocalBuild(
  pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string, notes: string[],
): Promise<{ dxChanged: boolean; latest: string; failed?: string }> {
  const id = profileOf(seams).label;
  const latest = await getLatestDx(seams.fetchFeed);
  const propsPath = seams.propsPath ?? path.join(repo, "Directory.Packages.props");
  const dx = await dxPhase(ctx, seams, propsPath, latest);
  notes.push(...dx.notes);
  const pins = await depPinsPhase(ctx, seams, propsPath, choice);
  notes.push(...pins.notes);
  const build = await buildPhase(ctx, seams, choice, repo);
  if (build.code !== 0) {
    const msg = failureResult(id, choice, latest, notes, build);
    await ctx.ui.notify(msg, "warning");
    steerFailure(pi, msg);
    return { dxChanged: dx.changed || pins.changed, latest, failed: msg };
  }
  notes.push(`build succeeded (${choice})`);
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
  if (!pub.ok && pub.failed) steerFailure(pi, msg);
  return msg;
}

async function runBuildFlow(pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string, skipBuild = false): Promise<string> {
  const id = profileOf(seams).label;
  try {
    const notes: string[] = [];
    let dxChanged = false;
    let latest = "";
    if (!skipBuild) {
      const local = await runLocalBuild(pi, ctx, seams, choice, repo, notes);
      if (local.failed) return local.failed;
      dxChanged = local.dxChanged;
      latest = local.latest;
    } else {
      notes.push("build skipped — publish only");
    }
    return finishPublish(pi, ctx, seams, choice, repo, notes, dxChanged, latest, id, skipBuild);
  } catch (err) {
    const detail = err instanceof Error ? err.message : String(err);
    const msg = `${id} build aborted: ${detail}`;
    await ctx.ui.notify(msg, "warning");
    if (!detail.includes("aborted")) steerFailure(pi, msg);
    return msg;
  }
}

function seamsForPick(merged: BuildSeams, projectPick: string | undefined, cwd: string): { seams: BuildSeams; repo: string | null } {
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

async function watchPhase(pi: any, ctx: any, merged: BuildSeams, cwd: string, parts: string[]): Promise<string> {
  const p = profileOf(merged);
  const repo = repoRootOf(cwd, p);
  if (!repo) return missingRepo(p, cwd);
  const choice = parts[1] === "release" ? "Release" : "Lab";
  startAzDoWatcher(pi, ctx, merged, { followNugets: true, repoRoot: repo, choice });
  await ctx.ui.notify("AzDO watcher started — it follows the build, the nuget publish and the release consumers chain, toasting on every check.", "info");
  return "AzDO watcher started in the background — toasts on every check, nuget assertion on the eXpand server at the nugets step, release consumers watched last.";
}

function flowRunner(pi: any, ctx: any, merged: BuildSeams, cwd: string) {
  return (choice: string, skipBuild = false, projectPick?: string) => {
    const { seams: s, repo } = seamsForPick(merged, projectPick, cwd);
    if (!repo) return Promise.resolve(missingRepo(profileOf(s), cwd));
    return runBuildFlow(pi, ctx, s, choice, repo, skipBuild);
  };
}

async function handleDevexpress(pi: any, ctx: any, merged: BuildSeams, args: string | string[]): Promise<string> {
  const cwd = ctx?.cwd ?? merged.repoRoot ?? process.cwd();
  const parts = (typeof args === "string" ? args.split(/\s+/) : args ?? []).filter(Boolean);
  if (parts[0] === "watch") return watchPhase(pi, ctx, merged, cwd, parts);
  return runDevexpressMenu(ctx, merged, args, flowRunner(pi, ctx, merged, cwd));
}

export function registerBuildCommand(pi: any, seams?: Partial<BuildSeams>): void {
  pi.registerCommand("devexpress", {
    description: "DevExpress menu: Build → RX-XAF | eXpand → Lab | Release; args: status | cancel | watch | build lab|release | publish lab|release",
    handler: async (args: string | string[], ctx: any) => {
      return handleDevexpress(pi, ctx, { ...defaultSeams(), ...seams }, args);
    },
  });
}
