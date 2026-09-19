/**
 * reactive-xaf-build/publish — VMs, commit, optional git push, queue, watcher.
 *
 * Called after a local build (or skip-build). Repo-specific queue/push
 * come from RepoProfile.
 *
 * The VM gate reads the agents through one classifier: a probe that failed, or
 * that did not name every agent, stops a publish instead of reading as "already
 * running". Queueing a pipeline onto agents nothing started is the failure the
 * gate exists to prevent. `prewarmVms` boots what it can while the build runs.
 */

import { sleep } from "./pane.js";
import { startAzDoWatcher } from "./watcher.js";
import { profileOf } from "./profile.js";
import type { Choice } from "./profile.js";
import type { BuildSeams } from "./build.js";

const VM_NAMES = ["C11", "C12", "C13", "C14"];
const VM_CHECK_CMD = `Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`;
/** The one state an AzDO agent answers work in. */
const VM_READY = "Running";
/** States a Start-VM brings up. */
const VM_STARTABLE = ["Off", "Saved"];
/** On the way up or down: wait for the agent, never start it mid-transition. */
const VM_BOOTING = ["Starting", "Pausing", "Resuming", "Saving", "Stopping"];
const VM_PROBE_TIMEOUT_MS = 60_000;
/** The first pwsh of a session is the slow one: one retry before refusing. */
const VM_PROBE_ATTEMPTS = 2;
const VM_WAIT_POLLS = 18;
/** Hyper-V's answer for an agent that is already up — expected on a blind start. */
const VM_STATE_ERROR_RE = /current state/i;

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

/** The agents to start and the agents to wait for. There is no "unreadable"
 *  member on purpose: a probe that cannot be read THROWS (VmProbeError), so a
 *  caller that does not handle it stops the flow loudly instead of acting on an
 *  empty agent list. */
export interface VmPlan {
  start: string[];
  booting: string[];
}

/** The probe could not be read. `planVms` throws it and `probeVms` rethrows it:
 *  the publish gate is the one place that turns it into a refusal. */
export class VmProbeError extends Error {
  constructor(reason: string) {
    super(reason);
    this.name = "VmProbeError";
  }
}

/** The refusal's message, whatever shape the failure arrived in. */
function reasonOf(err: unknown): string {
  if (err instanceof VmProbeError) return err.message;
  return `Get-VM did not run: ${err instanceof Error ? err.message : String(err)}`;
}

/** pwsh exited nonzero: killed at the timeout, or the command failed. */
function exitReason(probe: { code: number; stderr: string }): string {
  return `Get-VM failed (exit ${probe.code}): ${tail(probe.stderr) || "no stderr"}`;
}

export function planVms(probe: { code: number; stdout: string; stderr: string }): VmPlan {
  if (probe.code !== 0) {
    throw new VmProbeError(exitReason(probe));
  }
  const states = parseVmStates(probe.stdout);
  const missing = VM_NAMES.filter((n) => !states.has(n));
  if (missing.length > 0) {
    throw new VmProbeError(`Get-VM did not report ${missing.join(", ")} — no state to act on`);
  }
  const start: string[] = [];
  const booting: string[] = [];
  for (const name of VM_NAMES) {
    const state = states.get(name) as string;
    if (state === VM_READY) continue;
    if (VM_STARTABLE.includes(state)) {
      start.push(name);
      continue;
    }
    if (VM_BOOTING.includes(state)) {
      booting.push(name);
      continue;
    }
    throw new VmProbeError(`${name} is ${state} — neither running nor startable, fix it before publishing`);
  }
  return { start, booting };
}

/** The probe as a plan, or a VmProbeError. Only a TRANSPORT failure is retried
 *  once (the seam threw, or pwsh exited nonzero: the first pwsh of a session is
 *  the slow one, and a killed probe must not take the agent list with it). A
 *  readable answer that cannot be acted on is refused on the spot — retrying a
 *  state or a truncated list would only report the same thing twice. The
 *  invocation skips the user profile: a profile stall is what killed the probe
 *  that started this fix. */
async function probeVms(seams: BuildSeams): Promise<VmPlan> {
  let failure = "";
  for (let attempt = 0; attempt < VM_PROBE_ATTEMPTS; attempt++) {
    let probe: { code: number; stdout: string; stderr: string } | null = null;
    try {
      probe = await seams.run(VM_CHECK_CMD, { timeoutMs: VM_PROBE_TIMEOUT_MS, noProfile: true });
    } catch (err) {
      failure = `Get-VM did not run: ${err instanceof Error ? err.message : String(err)}`;
    }
    if (probe !== null && probe.code === 0) return planVms(probe);
    if (probe !== null) failure = exitReason(probe);
  }
  throw new VmProbeError(failure || "Get-VM did not answer");
}

/** Start-VM for the named agents: null when it ran, the reason when it did not. */
async function startVms(seams: BuildSeams, names: string[]): Promise<string | null> {
  try {
    const res = await seams.run(`Start-VM -Name ${names.join(",")}`, { timeoutMs: 120_000 });
    if (res.code === 0) return null;
    return `Start-VM failed: ${tail(res.stderr) || `exit ${res.code}`}`;
  } catch (err) {
    return `Start-VM did not run: ${err instanceof Error ? err.message : String(err)}`;
  }
}

/** Poll until every agent answers Running. A poll that cannot be read, or a
 *  state nothing can start, ends the wait loudly instead of burning the clock. */
async function waitForRunning(seams: BuildSeams, notes: string[]): Promise<{ ok: boolean; notes: string[] }> {
  for (let i = 0; i < VM_WAIT_POLLS; i++) {
    await sleep(seams.pollMs ?? 10_000);
    let plan: VmPlan;
    try {
      plan = await probeVms(seams);
    } catch (err) {
      notes.push(reasonOf(err));
      return { ok: false, notes };
    }
    if (plan.start.length === 0 && plan.booting.length === 0) {
      notes.push("Hyper-V agents running");
      return { ok: true, notes };
    }
  }
  notes.push("Hyper-V agents did not reach Running within 3 minutes");
  return { ok: false, notes };
}

/** The publish gate: the only place a probe becomes a decision. A probe that
 *  cannot be read refuses here, so nothing reaches the commit or the queue on an
 *  agent list nobody could read. */
async function ensureVmsRunning(seams: BuildSeams): Promise<{ ok: boolean; notes: string[] }> {
  const notes: string[] = [];
  let plan: VmPlan;
  try {
    plan = await probeVms(seams);
  } catch (err) {
    notes.push(reasonOf(err));
    return { ok: false, notes };
  }
  if (plan.start.length > 0) {
    if (plan.booting.length > 0) notes.push(`already booting: ${plan.booting.join(", ")}`);
    notes.push(`starting Hyper-V agents: ${plan.start.join(", ")}`);
    const failed = await startVms(seams, plan.start);
    if (failed !== null) {
      notes.push(failed);
      return { ok: false, notes };
    }
  } else if (plan.booting.length > 0) {
    notes.push(`Hyper-V agents already booting: ${plan.booting.join(", ")} — waiting for Running`);
  } else {
    notes.push("Hyper-V agents C11-C14 already running");
    return { ok: true, notes };
  }
  return waitForRunning(seams, notes);
}

/** The pre-warm's outcome: the lines the started notice shows, and whether the
 *  VM layer needs attention (an unreadable probe, or a start that did not run). */
export interface PrewarmOutcome {
  notes: string[];
  attention: boolean;
}

/** Boot the agents while the build runs. A readable probe starts exactly the
 *  agents that can start; an unreadable one blind-starts all four. Never waits
 *  for Running, never throws: the publish gate is still the authority. */
export async function prewarmVms(seams: BuildSeams): Promise<PrewarmOutcome> {
  let plan: VmPlan;
  try {
    plan = await probeVms(seams);
  } catch (err) {
    return blindPrewarm(seams, reasonOf(err));
  }
  if (plan.start.length === 0) {
    if (plan.booting.length > 0) return { notes: [`Hyper-V agents already booting: ${plan.booting.join(", ")}`], attention: false };
    return { notes: ["Hyper-V agents C11-C14 already running"], attention: false };
  }
  const failed = await startVms(seams, plan.start);
  if (failed !== null) return { notes: [`Hyper-V agents: ${failed}`], attention: true };
  return { notes: [`Hyper-V agents booting during the build: ${plan.start.join(", ")}`], attention: false };
}

/** The probe could not be read, so the pre-warm cannot tell which agent needs a
 *  start: start all four by name. The agents already up answer with Hyper-V's
 *  state error, which is not a failure — the build window is used either way. */
async function blindPrewarm(seams: BuildSeams, reason: string): Promise<PrewarmOutcome> {
  const failed = await startVms(seams, VM_NAMES);
  if (failed !== null && !VM_STATE_ERROR_RE.test(failed)) {
    return { notes: [`Hyper-V agents: ${reason}`, failed], attention: true };
  }
  return {
    notes: [`Hyper-V agents: ${reason}`, "blind Start-VM for C11-C14 (an agent already up answers with a state error)"],
    attention: true,
  };
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
