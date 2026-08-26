/**
 * reactive-xaf-build/publish — VMs, commit, optional git push, queue, watcher.
 *
 * Called after a local build (or skip-build). Repo-specific queue/push
 * come from RepoProfile.
 */

import { sleep } from "./pane.js";
import { startAzDoWatcher } from "./watcher.js";
import { profileOf } from "./profile.js";
import type { Choice } from "./profile.js";
import type { BuildSeams } from "./build.js";

const VM_NAMES = ["C11", "C12", "C13", "C14"];
const VM_CHECK_CMD = `Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`;

function tail(s: string, n = 1500): string {
  const t = s.trim();
  return t.length <= n ? t : "..." + t.slice(-n);
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

async function monitorPhase(pi: any, ctx: any, seams: BuildSeams, repo: string, choice: string): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  await ctx.ui.notify("AzDO build queued — monitoring in background (toast on every check).", "info");
  const starter = seams.startAzDoWatcher ?? startAzDoWatcher;
  starter(pi, ctx, seams, { followNugets: true, repoRoot: repo, choice: choice === "Release" ? "Release" : "Lab" });
  notes.push("AzDO monitoring in background — toasts on every check; follows the nuget + release publish chain and asserts the nugets");
  return { ok: true, failed: false, notes };
}

async function pushThenQueue(seams: BuildSeams, repoRoot: string, choice: Choice, notes: string[]): Promise<{ failed: boolean }> {
  const p = profileOf(seams);
  const remote = p.pushRemote(choice);
  if (remote) {
    const push = await seams.run(`git push ${remote} HEAD:master`, { cwd: repoRoot, timeoutMs: 120000 });
    if (push.code !== 0) {
      notes.push(`git push ${remote} failed: ${tail(push.stderr)}`);
      return { failed: true };
    }
    notes.push(`pushed to ${remote}`);
  }
  const queueCmd = p.queueCmd(choice);
  const res = await seams.run(queueCmd, { cwd: repoRoot, timeoutMs: 600000 });
  if (res.code !== 0) {
    notes.push(`${queueCmd} failed: ${tail(res.stderr)}`);
    return { failed: true };
  }
  notes.push(`${queueCmd} done (exit ${res.code})`);
  return { failed: false };
}

async function queuePhase(ctx: any, seams: BuildSeams, repoRoot: string, choice: string): Promise<{ failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  const p = profileOf(seams);
  const queueCmd = p.queueCmd(choice as Choice);
  await ctx.ui.notify(`Publishing via ${queueCmd}…`, "info");
  const pick = await ctx.ui.select(`Publish: ${p.queueLabel(choice as Choice)}?`, ["Publish", "Abort"]);
  if (pick !== "Publish") {
    notes.push("publish aborted");
    return { failed: false, notes };
  }
  const ran = await pushThenQueue(seams, repoRoot, choice as Choice, notes);
  return { failed: ran.failed, notes };
}

export async function publishPhase(
  pi: any, ctx: any, seams: BuildSeams, choice: string, repoRoot: string,
  dxChanged: boolean, latest: string, skipBuild = false,
): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  await ctx.ui.notify("Checking Hyper-V agents C11-C14…", "info");
  const vms = await ensureVmsRunning(seams);
  notes.push(...vms.notes);
  if (!vms.ok) return { ok: false, failed: true, notes };
  await ctx.ui.notify("Committing build state…", "info");
  const commit = await commitPhase(ctx, seams, repoRoot, dxChanged, latest, skipBuild ? "Publish" : "Build fixes");
  notes.push(...commit.notes);
  if (!commit.committed) return { ok: false, failed: commit.failed === true, notes };
  const queue = await queuePhase(ctx, seams, repoRoot, choice);
  notes.push(...queue.notes);
  if (queue.failed) return { ok: false, failed: true, notes };
  const monitor = await monitorPhase(pi, ctx, seams, repoRoot, choice);
  notes.push(...monitor.notes);
  return { ok: monitor.ok, failed: monitor.failed, notes };
}
