/**
 * reactive-xaf-build/build — the /devexpress menu workflow engine.
 *
 * Menu: /devexpress → Build → RX-XAF → Lab | Release, "Last build status"
 * (+ "Close build pane" while a build pane is open). Menu picks delegate to
 * a NEW psmux window (delegate.ts); direct args run here: /devexpress status,
 * /devexpress build lab|release (the delegated window uses these).
 * Flow (Lab | Release):
 *   1. getLatestDx — nuget.org flat-container, max stable DevExpress.ExpressApp
 *   2. props compare — Directory.Packages.props DevExpress.* pins:
 *        no pins / single shared version → ask update-all / skip / abort (rewrite on update)
 *        mixed versions → file left untouched, surfaced
 *   3. brx / brx -Release — runs in a NEW psmux pane split to the RIGHT; the
 *      output streams there live (pane.ts). Milestones are notified in this
 *      window. Green → conversational ask only (no modal, no auto-close): the
 *      pane is left open and closed via /devexpress → "Close build pane".
 *      Red → the failure delivery (sendUserMessage — a triggered turn) with
 *      the pane's captured tail; the pane is KEPT for reuse. In-process
 *      fallback when the pane cannot be opened.
 *   4. publish — Hyper-V C11-C14 ensured (Start-VM + poll), git commit (message
 *      from changes, confirmed), confirm, prx / prx -Release, then the AzDO
 *      build monitor (waitForAzDoBuild — polls the queued build to completion;
 *      on failure the failed record's log supplies the ##[error] reason).
 *   4b. skip-build variant (menu "Lab (skip build)" / "Release (skip build)",
 *      arg publish lab|release): no DX check, no brx — straight to publish.
 *
 * Seams (injectable, default = real): run, fetchFeed, propsPath, repoRoot,
 * pollMs, waitForAzDoBuild (azdo.ts), delegateWindow (delegate.ts) + the pane
 * seams from pane.ts. Tests pass fakes via
 * registerBuildCommand — the real nuget.org, pwsh, psmux, VMs and git are
 * never touched by the test suite.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import {
  sleep, runProcess, getBuildPane, setBuildPane, exitMarkerPath,
  defaultOpenBuildPane, defaultRunInPane, defaultWaitForPaneExit,
  defaultCapturePane, defaultClosePane,
} from "./pane.js";
import type { RunResult, PaneOpener, PaneRunner, PaneWaiter, PaneCapturer, PaneCloser } from "./pane.js";
import { defaultWaitForAzDoBuild, AZDO_BUILD_URL } from "./azdo.js";
import type { AzDoBuildWaiter } from "./azdo.js";
import { runDevexpressMenu } from "./menu.js";
import { defaultDelegateWindow } from "./delegate.js";
import type { WindowDelegator } from "./delegate.js";

export type { RunResult } from "./pane.js";

export type CommandRunner = (cmd: string, opts?: { cwd?: string; timeoutMs?: number }) => Promise<RunResult>;
export type FeedFetcher = (url: string) => Promise<string>;

export interface BuildSeams {
  run: CommandRunner;
  fetchFeed: FeedFetcher;
  propsPath?: string;
  repoRoot?: string;
  pollMs?: number;
  openBuildPane?: PaneOpener;
  runInPane?: PaneRunner;
  waitForPaneExit?: PaneWaiter;
  capturePane?: PaneCapturer;
  closePane?: PaneCloser;
  waitForAzDoBuild?: AzDoBuildWaiter;
  delegateWindow?: WindowDelegator;
}

const DX_FEED_URL = "https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json";
const VM_NAMES = ["C11", "C12", "C13", "C14"];
const VM_CHECK_CMD = `Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`;
const DX_PIN_RE = /Include="(DevExpress\.[^"]*)"\s+Version="([^"]*)"/g;

export function defaultSeams(): BuildSeams {
  return {
    run: runProcess,
    fetchFeed: async (url: string) => {
      const res = await globalThis.fetch(url);
      if (!res.ok) throw new Error(`feed query failed: HTTP ${res.status}`);
      return res.text();
    },
    openBuildPane: defaultOpenBuildPane,
    runInPane: defaultRunInPane,
    waitForPaneExit: defaultWaitForPaneExit,
    capturePane: defaultCapturePane,
    closePane: defaultClosePane,
    waitForAzDoBuild: defaultWaitForAzDoBuild,
    delegateWindow: defaultDelegateWindow,
  };
}

export function repoRootOf(cwd: string): string | null {
  const p = path.resolve(cwd);
  const hasProps = fs.existsSync(path.join(p, "Directory.Packages.props"));
  const hasSrc = fs.existsSync(path.join(p, "src", "Extensions"));
  return hasProps && hasSrc ? p : null;
}

function compareVersions(a: string, b: string): number {
  const pa = a.split(".").map(Number);
  const pb = b.split(".").map(Number);
  for (let i = 0; i < 3; i++) {
    if (pa[i] !== pb[i]) return pa[i] - pb[i];
  }
  return 0;
}

export async function getLatestDx(fetchFeed: FeedFetcher): Promise<string> {
  const text = await fetchFeed(DX_FEED_URL);
  const versions = JSON.parse(text).versions as string[];
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
  const pane = await (seams.openBuildPane ?? defaultOpenBuildPane)(repo);
  if (!pane) {
    await ctx.ui.notify("Build pane could not be opened — building in-process.", "warning");
    const res = await seams.run(choice === "Release" ? "brx -Release" : "brx", { cwd: repo, timeoutMs: 3600000 });
    return { code: res.code, stdout: res.stdout + res.stderr };
  }
  setBuildPane(pane);
  await ctx.ui.notify(`Build started — pane ${pane} on the right.`, "info");
  const cmd = `brx${choice === "Release" ? " -Release" : ""}; if ($?) { Set-Content -LiteralPath '${marker}' -Value 0 } else { Set-Content -LiteralPath '${marker}' -Value 1 }`;
  await (seams.runInPane ?? defaultRunInPane)(pane, cmd);
  const wait = await (seams.waitForPaneExit ?? defaultWaitForPaneExit)(pane, marker, 3600000);
  if (wait.timedOut) {
    await (seams.closePane ?? defaultClosePane)(pane);
    setBuildPane(null);
    return { code: -1, stdout: "build timed out after 60 minutes — pane closed" };
  }
  const captured = await (seams.capturePane ?? defaultCapturePane)(pane);
  return { code: wait.code ?? -1, stdout: captured };
}

function parseVmStates(stdout: string): Map<string, string> {
  const states = new Map<string, string>();
  for (const line of stdout.split("\n")) {
    const m = line.match(/^(C1[1-4])=(.*)$/);
    if (m) states.set(m[1], m[2].trim());
  }
  return states;
}

async function ensureVmsRunning(seams: BuildSeams): Promise<{ ok: boolean; notes: string[] }> {
  const notes: string[] = [];
  const check = async () => seams.run(VM_CHECK_CMD, { timeoutMs: 60000 });
  const first = await check();
  const states = parseVmStates(first.stdout);
  const off = VM_NAMES.filter((n) => states.get(n) === "Off");
  const starting = VM_NAMES.filter((n) => states.get(n) === "Starting");
  if (off.length > 0) {
    if (starting.length > 0) notes.push(`already booting: ${starting.join(", ")}`);
    notes.push(`starting Hyper-V agents: ${off.join(", ")}`);
    const start = await seams.run(`Start-VM -Name ${off.join(",")}`, { timeoutMs: 120000 });
    if (start.code !== 0) {
      notes.push(`Start-VM failed: ${tail(start.stderr)}`);
      return { ok: false, notes };
    }
  } else if (starting.length > 0) {
    notes.push(`Hyper-V agents already booting: ${starting.join(", ")} — waiting for Running`);
  } else {
    notes.push("Hyper-V agents C11-C14 already running");
    return { ok: true, notes };
  }
  for (let i = 0; i < 18; i++) {
    await sleep(seams.pollMs ?? 10000);
    const res = await check();
    const st = parseVmStates(res.stdout);
    if (VM_NAMES.every((n) => st.get(n) === "Running")) {
      notes.push("Hyper-V agents running");
      return { ok: true, notes };
    }
  }
  notes.push("Hyper-V agents did not reach Running within 3 minutes");
  return { ok: false, notes };
}

async function commitPhase(ctx: any, seams: BuildSeams, repoRoot: string, dxChanged: boolean, latest: string, label = "Build fixes"): Promise<{ committed: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  const status = await seams.run("git status --short", { cwd: repoRoot, timeoutMs: 30000 });
  const changed = status.stdout.split("\n").filter((l) => l.trim()).length;
  if (changed === 0) {
    notes.push("nothing to commit");
    return { committed: true, failed: false, notes };
  }
  const msg = dxChanged ? `Update DX to ${latest}` : `${label} (${changed} files)`;
  const pick = await ctx.ui.select(`Commit with message: "${msg}"?`, ["Commit", "Abort"]);
  if (pick !== "Commit") {
    notes.push("commit aborted");
    return { committed: false, failed: false, notes };
  }
  const add = await seams.run("git add -A", { cwd: repoRoot, timeoutMs: 60000 });
  if (add.code !== 0) {
    notes.push(`git add failed: ${tail(add.stderr)}`);
    return { committed: false, failed: true, notes };
  }
  const safeMsg = msg.replace(/"/g, "'");
  const commit = await seams.run(`git commit -m "${safeMsg}"`, { cwd: repoRoot, timeoutMs: 60000 });
  if (commit.code !== 0) {
    notes.push(`git commit failed: ${tail(commit.stderr)}`);
    return { committed: false, failed: true, notes };
  }
  notes.push(`committed: ${msg}`);
  return { committed: true, failed: false, notes };
}

/** Await the queued AzDO build (prx queues it; the newest build is ours).
 *  Failure policy: on failure the agent PLANS a fix and presents it — user
 *  permission is ALWAYS required before any action. No auto-fix, no auto
 *  re-run. */
async function monitorPhase(ctx: any, seams: BuildSeams): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  await ctx.ui.notify("AzDO build queued — monitoring…", "info");
  const monitor = await (seams.waitForAzDoBuild ?? defaultWaitForAzDoBuild)(7200000);
  if (monitor.result === "succeeded") {
    notes.push(`AzDO build ${monitor.id} succeeded`);
    return { ok: true, failed: false, notes };
  }
  if (monitor.result === "canceled") {
    notes.push("AzDO build canceled");
    return { ok: true, failed: false, notes };
  }
  if (monitor.result === "failed") {
    const detail = monitor.reason ? ` — ${monitor.reason}` : "";
    notes.push(`AzDO build ${monitor.id} FAILED${detail} — ${AZDO_BUILD_URL}`);
    return { ok: false, failed: true, notes };
  }
  notes.push(monitor.reason === "timeout" ? "AzDO build monitoring timed out" : `AzDO build ${monitor.id} ended unexpectedly: ${monitor.reason || monitor.result}`);
  return { ok: false, failed: true, notes };
}

async function publishPhase(ctx: any, seams: BuildSeams, choice: string, repoRoot: string, dxChanged: boolean, latest: string, skipBuild = false): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  let failed = false;
  await ctx.ui.notify("Checking Hyper-V agents C11-C14…", "info");
  const vms = await ensureVmsRunning(seams);
  notes.push(...vms.notes);
  if (!vms.ok) return { ok: false, failed: true, notes };
  await ctx.ui.notify("Committing build state…", "info");
  const commit = await commitPhase(ctx, seams, repoRoot, dxChanged, latest, skipBuild ? "Publish" : "Build fixes");
  notes.push(...commit.notes);
  if (!commit.committed) {
    failed = commit.failed === true;
    return { ok: false, failed, notes };
  }
  const prxCmd = choice === "Release" ? "prx -Release" : "prx";
  await ctx.ui.notify(`Publishing via ${prxCmd}…`, "info");
  const pick = await ctx.ui.select(`Publish: ${prxCmd} (stage, force-push, queue AzDO Reactive.XAF)?`, ["Publish", "Abort"]);
  if (pick !== "Publish") {
    notes.push("publish aborted");
    return { ok: false, failed: false, notes };
  }
  const res = await seams.run(prxCmd, { cwd: repoRoot, timeoutMs: 600000 });
  if (res.code !== 0) {
    notes.push(`prx failed: ${tail(res.stderr)}`);
    return { ok: false, failed: true, notes };
  }
  notes.push(`prx done (exit ${res.code})`);
  const monitor = await monitorPhase(ctx, seams);
  notes.push(...monitor.notes);
  return { ok: monitor.ok, failed: monitor.failed, notes };
}

function failureResult(choice: string, latest: string, notes: string[], build: { code: number; stdout: string }): string {
  return [
    `Reactive.XAF build — ${choice}`,
    `DX latest: ${latest}`,
    ...notes,
    `Build FAILED (exit ${build.code})`,
    "--- output tail (from the build pane) ---",
    tail(build.stdout, 4000),
    "Fix the warnings, then re-run /devexpress.",
  ].join("\n");
}

function summaryResult(choice: string, latest: string, notes: string[], pubOk: boolean): string {
  const dxLine = latest ? `DX latest: ${latest}` : "no DX check (build skipped)";
  return [`Reactive.XAF build — ${choice}`, dxLine, ...notes, pubOk ? "published" : "publish stopped"].join("\n");
}

/** Deliver the failure so it ALWAYS lands in the agent's context: a user
 *  message via sendUserMessage triggers a turn unconditionally (pi core:
 *  "Always triggers a turn"). The triggerTurn steer was delivered as a
 *  custom_message but started no turn in long-lived sessions (2026-08-25,
 *  reproduced + validated via steer-repro). User aborts never reach this
 *  path. */
function steerFailure(pi: any, msg: string): void {
  pi.sendUserMessage(msg, { deliverAs: "steer" });
}

async function runBuildFlow(pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string, skipBuild = false): Promise<string> {
  try {
    const notes: string[] = [];
    let dxChanged = false;
    let latest = "";
    if (!skipBuild) {
      latest = await getLatestDx(seams.fetchFeed);
      const propsPath = seams.propsPath ?? path.join(repo, "Directory.Packages.props");
      const dx = await dxPhase(ctx, seams, propsPath, latest);
      dxChanged = dx.changed;
      notes.push(...dx.notes);
      const build = await buildPhase(ctx, seams, choice, repo);
      if (build.code !== 0) {
        const msg = failureResult(choice, latest, notes, build);
        await ctx.ui.notify(msg, "warning");
        steerFailure(pi, msg);
        return msg;
      }
      notes.push(`build succeeded (${choice})`);
    } else {
      notes.push("build skipped — publish only");
    }
    const pub = await publishPhase(ctx, seams, choice, repo, dxChanged, latest, skipBuild);
    notes.push(...pub.notes);
    const pane = getBuildPane();
    const closeAsk = pane ? `\nThe build pane ${pane} is left open — close it via /devexpress → "Close build pane" when done.` : "";
    const msg = summaryResult(choice, latest, notes, pub.ok) + closeAsk;
    await ctx.ui.notify(msg, "info");
    if (!pub.ok && pub.failed) steerFailure(pi, msg);
    return msg;
  } catch (err) {
    const detail = err instanceof Error ? err.message : String(err);
    const msg = `Reactive.XAF build aborted: ${detail}`;
    await ctx.ui.notify(msg, "warning");
    if (!detail.includes("aborted")) steerFailure(pi, msg);
    return msg;
  }
}

export function registerBuildCommand(pi: any, seams?: Partial<BuildSeams>): void {
  pi.registerCommand("devexpress", {
    description: "DevExpress menu: Build → RX-XAF (Lab | Release, skip-build variants), Last build status; args: status | build lab|release | publish lab|release",
    handler: async (args: string | string[], ctx: any) => {
      const merged = { ...defaultSeams(), ...seams };
      const cwd = ctx?.cwd ?? merged.repoRoot ?? process.cwd();
      const repo = repoRootOf(cwd);
      if (!repo) {
        return `Reactive.XAF build: not inside the Reactive.XAF repo (cwd: ${cwd}) — no commands ran.`;
      }
      const runFlow = (choice: string, skipBuild = false) => runBuildFlow(pi, ctx, merged, choice, repo, skipBuild);
      return runDevexpressMenu(ctx, merged, repo, args, runFlow);
    },
  });
}
