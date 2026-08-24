/**
 * reactive-xaf-build/build — the /devexpress menu workflow engine.
 *
 * Menu: /devexpress → Build → RX-XAF → Lab | Release (+ "Close build pane"
 * while a build pane is open).
 * Flow (Lab | Release):
 *   1. getLatestDx — nuget.org flat-container, max stable DevExpress.ExpressApp
 *   2. props compare — Directory.Packages.props DevExpress.* pins:
 *        no pins / single shared version → ask update-all / skip / abort (rewrite on update)
 *        mixed versions → file left untouched, surfaced
 *   3. brx / brx -Release — runs in a NEW psmux pane split to the RIGHT; the
 *      output streams there live (pane.ts). Milestones are notified in this
 *      window. Green → conversational ask only (no modal, no auto-close): the
 *      pane is left open and closed via /devexpress → "Close build pane".
 *      Red → the failure steer (triggerTurn) with the pane's captured tail;
 *      the pane is KEPT for reuse. In-process fallback when the pane cannot
 *      be opened.
 *   4. publish — Hyper-V C11-C14 ensured (Start-VM + poll), git commit (message
 *      from changes, confirmed), confirm, prx / prx -Release
 *
 * Seams (injectable, default = real): run, fetchFeed, propsPath, repoRoot,
 * pollMs + the pane seams from pane.ts. Tests pass fakes via
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
    const res = await seams.run(choice === "Release" ? "brx -Release" : "brx", { cwd: repo, timeoutMs: 1800000 });
    return { code: res.code, stdout: res.stdout + res.stderr };
  }
  setBuildPane(pane);
  await ctx.ui.notify(`Build started — pane ${pane} on the right.`, "info");
  const cmd = `brx${choice === "Release" ? " -Release" : ""}; if ($?) { Set-Content -LiteralPath '${marker}' -Value 0 } else { Set-Content -LiteralPath '${marker}' -Value 1 }`;
  await (seams.runInPane ?? defaultRunInPane)(pane, cmd);
  const wait = await (seams.waitForPaneExit ?? defaultWaitForPaneExit)(pane, marker, 1800000);
  if (wait.timedOut) {
    await (seams.closePane ?? defaultClosePane)(pane);
    setBuildPane(null);
    return { code: -1, stdout: "build timed out after 30 minutes — pane closed" };
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
  const off = VM_NAMES.filter((n) => parseVmStates(first.stdout).get(n) !== "Running");
  if (!off.length) {
    notes.push("Hyper-V agents C11-C14 already running");
    return { ok: true, notes };
  }
  notes.push(`starting Hyper-V agents: ${off.join(", ")}`);
  const start = await seams.run(`Start-VM -Name ${off.join(",")}`, { timeoutMs: 120000 });
  if (start.code !== 0) {
    notes.push(`Start-VM failed: ${tail(start.stderr)}`);
    return { ok: false, notes };
  }
  for (let i = 0; i < 18; i++) {
    await sleep(seams.pollMs ?? 10000);
    const res = await check();
    const states = parseVmStates(res.stdout);
    if (VM_NAMES.every((n) => states.get(n) === "Running")) {
      notes.push("Hyper-V agents running");
      return { ok: true, notes };
    }
  }
  notes.push("Hyper-V agents did not reach Running within 3 minutes");
  return { ok: false, notes };
}

async function commitPhase(ctx: any, seams: BuildSeams, repoRoot: string, dxChanged: boolean, latest: string): Promise<{ committed: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  const status = await seams.run("git status --short", { cwd: repoRoot, timeoutMs: 30000 });
  const changed = status.stdout.split("\n").filter((l) => l.trim()).length;
  if (changed === 0) {
    notes.push("nothing to commit");
    return { committed: true, failed: false, notes };
  }
  const msg = dxChanged ? `Update DX to ${latest}` : `Build fixes (${changed} files)`;
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

async function publishPhase(ctx: any, seams: BuildSeams, choice: string, repoRoot: string, dxChanged: boolean, latest: string): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  let failed = false;
  const vms = await ensureVmsRunning(seams);
  notes.push(...vms.notes);
  if (!vms.ok) return { ok: false, failed: true, notes };
  const commit = await commitPhase(ctx, seams, repoRoot, dxChanged, latest);
  notes.push(...commit.notes);
  if (!commit.committed) {
    failed = commit.failed === true;
    return { ok: false, failed, notes };
  }
  const prxCmd = choice === "Release" ? "prx -Release" : "prx";
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
  return { ok: true, failed: false, notes };
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
  return [`Reactive.XAF build — ${choice}`, `DX latest: ${latest}`, ...notes, pubOk ? "published" : "publish stopped"].join("\n");
}

/** Warn the agent with triggerTurn — the failure lands in the agent's context
 *  so it can act (fix warnings) instead of waiting for the user to relay it.
 *  User aborts never reach this path. */
function steerFailure(pi: any, msg: string): void {
  const steer = (globalThis as any).__steer;
  if (typeof steer === "function") {
    steer(pi, "reactive-xaf-build:build-failed", msg, "", "steer", { triggerTurn: true, severity: "warning" });
  }
}

async function runBuildFlow(pi: any, ctx: any, seams: BuildSeams, choice: string, repo: string): Promise<string> {
  try {
    const latest = await getLatestDx(seams.fetchFeed);
    const propsPath = seams.propsPath ?? path.join(repo, "Directory.Packages.props");
    const dx = await dxPhase(ctx, seams, propsPath, latest);
    const build = await buildPhase(ctx, seams, choice, repo);
    const notes = [...dx.notes];
    if (build.code !== 0) {
      const msg = failureResult(choice, latest, notes, build);
      await ctx.ui.notify(msg, "warning");
      steerFailure(pi, msg);
      return msg;
    }
    notes.push(`build succeeded (${choice})`);
    const pub = await publishPhase(ctx, seams, choice, repo, dx.changed, latest);
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

async function runDevExpressMenu(pi: any, ctx: any, seams: BuildSeams): Promise<string> {
  const cwd = ctx?.cwd ?? seams.repoRoot ?? process.cwd();
  const repo = repoRootOf(cwd);
  if (!repo) {
    return `Reactive.XAF build: not inside the Reactive.XAF repo (cwd: ${cwd}) — no commands ran.`;
  }
  const pane = getBuildPane();
  const top = await ctx.ui.select("DevExpress", pane ? ["Build", "Close build pane"] : ["Build"]);
  if (top === "Close build pane") {
    await (seams.closePane ?? defaultClosePane)(pane!);
    setBuildPane(null);
    await ctx.ui.notify(`Build pane ${pane} closed.`, "info");
    return "Build pane closed.";
  }
  if (top !== "Build") return "DevExpress menu: aborted.";
  const build = await ctx.ui.select("Build", ["RX-XAF"]);
  if (build !== "RX-XAF") return "Build menu: aborted.";
  const rx = await ctx.ui.select("RX-XAF", ["Lab", "Release"]);
  if (rx !== "Lab" && rx !== "Release") return "RX-XAF: aborted (no flow selected).";
  return runBuildFlow(pi, ctx, seams, rx, repo);
}

export function registerBuildCommand(pi: any, seams?: Partial<BuildSeams>): void {
  pi.registerCommand("devexpress", {
    description: "DevExpress menu: Build → RX-XAF (Lab | Release)",
    handler: async (_args: string | string[], ctx: any) => runDevExpressMenu(pi, ctx, { ...defaultSeams(), ...seams }),
  });
}
