/**
 * reactive-xaf-build/publish — the VM layer and the phase order: commit,
 * optional git push, queue, watcher.
 *
 * Called after a local build (or skip-build). Repo-specific queue/push
 * come from RepoProfile.
 *
 * The VM gate reads the agents through one classifier: a probe that failed, or
 * that did not name every agent, stops a PUBLISH instead of reading as "already
 * running". Queueing a pipeline onto agents nothing started is the failure the
 * gate exists to prevent. `prewarmVms` boots what it can while the build runs,
 * and the gate itself runs while the commit is decided: it can cost the queue,
 * never the commit.
 *
 * The git half (the dirty read, the two commit prompts, the commit core) lives
 * in gitphase.ts: the prompts and the shared core crossed the 400-line cap here.
 */

import { startReleaseWatch } from "./github-release.js";
import { sleep } from "./pane.js";
import { startAzDoWatcher } from "./watcher.js";
import { profileOf } from "./profile.js";
import { commitMessage, commitPhase, pushDirtyPhase, runStep } from "./gitphase.js";
import { tail } from "./text.js";
import type { Choice } from "./profile.js";
import type { BuildSeams } from "./build.js";
import type { PushCheck } from "./gitphase.js";

const VM_NAMES = ["C11", "C12", "C13", "C14"];
const VM_CHECK_CMD = `Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`;
/** The one state an AzDO agent answers work in. */
const VM_READY = "Running";
/** States a Start-VM brings up. */
const VM_STARTABLE = ["Off", "Saved"];
/** On the way up or down: wait for the agent, never start it mid-transition. */
const VM_BOOTING = ["Starting", "Pausing", "Resuming", "Saving", "Stopping"];
/** The probe's budget, exported so the refusal text and its test read ONE number. */
export const VM_PROBE_TIMEOUT_MS = 60_000;
/** The first pwsh of a session is the slow one: one retry before refusing. */
const VM_PROBE_ATTEMPTS = 2;
const VM_WAIT_POLLS = 18;
/** Hyper-V's answer for an agent that is already up — expected on a blind start. */
const VM_STATE_ERROR_RE = /current state/i;

/** pwsh writes CRLF on Windows and terminates the last line: the read is split
 *  on it. A bare "\n" split leaves the \r on every line, where the anchored
 *  `(.*)$` never matches (JS `$` without /m is end-of-input) — a complete read
 *  parsed as no agents at all and a healthy lab refused to publish. Same
 *  convention as azdo.ts's STATUS/CANCEL parse. */
function parseVmStates(stdout: string): Map<string, string> {
  const states = new Map<string, string>();
  for (const line of stdout.split(/\r?\n/)) {
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

/** The probe's command failed. A timeout no longer arrives here: `runArgv`
 *  marks a killed child, and `probeVms` refuses on that mark before this. */
function exitReason(probe: { code: number; stderr: string }): string {
  return `Get-VM failed (exit ${probe.code}): ${tail(probe.stderr) || "no stderr"}`;
}

/** A probe killed at its deadline never ran a command, read no agent and exited
 *  with nothing: naming an exit code would send the reader looking for a failure
 *  that never happened. `planVms` decides it with every other probe shape, so the
 *  gate and the pre-warm read ONE wording. */
function timeoutReason(probe: VmProbe): string {
  const seconds = VM_PROBE_TIMEOUT_MS / 1000;
  if (probe.reaped === false) {
    return `Get-VM did not answer within ${seconds}s and its process could not be killed: the agent list is unreadable, and the leftover pwsh may still be running`;
  }
  return `Get-VM did not answer within ${seconds}s: the probe was killed at its deadline, so the agent list is unreadable`;
}

/** The refusal's evidence: what the probe itself printed. Naming the agents is
 *  not enough — an exit-0 read that listed nobody reads exactly like a probe that
 *  printed nothing at all, and that ambiguity is why one incident reached the
 *  report with no explanation in it. The stdout tail is added only when no agent
 *  was read at all: on a partial read it would read as a claim about the agents
 *  that did answer. */
function missingReason(missing: string[], probe: { stdout: string; stderr: string }): string {
  const head = `Get-VM did not report ${missing.join(", ")} — no state to act on`;
  const err = tail(probe.stderr, 400);
  if (err) return `${head} (stderr: ${err})`;
  if (missing.length < VM_NAMES.length) return head;
  const out = tail(probe.stdout, 400);
  return `${head} (${out ? `stdout: ${out}` : "no output on either stream"})`;
}

/** What one probe attempt produced, in one shape: `planVms` decides on it and the
 *  log row prints it. */
export interface VmProbe {
  code: number;
  stdout: string;
  stderr: string;
  /** The deadline fired: there is no exit code to read and no agent list to
   *  trust. */
  timedOut?: boolean;
  /** On a timed-out probe: whether the deadline's kill was VERIFIED. `false` means
   *  the process may still be running, and both the refusal and the log row say so
   *  instead of claiming a kill that never landed. */
  reaped?: boolean;
}

/** The probe wrote nothing to the log before this, which is why two windows had to
 *  reconstruct a stall from process counts. One row per attempt, tails bounded. */
function probeLog(probe: VmProbe | null, why: string, ms: number): string {
  if (probe === null) return `did not run in ${ms}ms (${why})`;
  const outcome = probe.timedOut ? (probe.reaped === false ? "timed out, kill not verified" : "timed out") : `exit ${probe.code}`;
  const out = tail(probe.stdout, 200).replace(/\s+/g, " ").trim() || "none";
  const err = tail(probe.stderr, 200).replace(/\s+/g, " ").trim() || "none";
  return `${outcome} in ${ms}ms | stdout: ${out} | stderr: ${err}`;
}

export function planVms(probe: VmProbe): VmPlan {
  if (probe.timedOut) {
    throw new VmProbeError(timeoutReason(probe));
  }
  if (probe.code !== 0) {
    throw new VmProbeError(exitReason(probe));
  }
  const states = parseVmStates(probe.stdout);
  const missing = VM_NAMES.filter((n) => !states.has(n));
  if (missing.length > 0) {
    throw new VmProbeError(missingReason(missing, probe));
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

/** The probe as a plan, or a VmProbeError. Two shapes are retried once: a
 *  TRANSPORT failure (the seam threw, or pwsh exited nonzero — the first pwsh of
 *  a session is the slow one, and a killed probe must not take the agent list
 *  with it) and an exit-0 read that named NO agent at all, which is not an
 *  answer about the agents: Hyper-V exits nonzero for a name it cannot find, so
 *  a silent exit-0 read is an unreadable probe, and the incident that produced
 *  one took a publish down with no cause in the report. A readable answer that
 *  cannot be acted on is refused on the spot — retrying a state or a truncated
 *  list would only report the same thing twice. The invocation skips the user
 *  profile: a profile stall is what killed the probe that started this fix. The
 *  shell stays pwsh — the VM layer was moved to `powershell.exe` on the theory that
 *  pwsh 7 could not answer `Get-VM` at all, and the re-measurement on the same box
 *  had pwsh at 1.3-2.3 s against 2.5 s for 5.1, so the theory bought nothing and
 *  cost a second environment. A probe killed at its deadline carries no exit code to
 *  retry, so it goes straight to `planVms` — the one place that decides what a
 *  probe means — and that refusal ends the loop: a second 60s wait would report
 *  the same silence. Every attempt leaves one log row. */
async function probeVms(seams: BuildSeams): Promise<VmPlan> {
  let failure = "";
  for (let attempt = 0; attempt < VM_PROBE_ATTEMPTS; attempt++) {
    let probe: VmProbe | null = null;
    let why = "";
    const started = Date.now();
    try {
      probe = await seams.run(VM_CHECK_CMD, { timeoutMs: VM_PROBE_TIMEOUT_MS, noProfile: true });
    } catch (err) {
      why = `Get-VM did not run: ${err instanceof Error ? err.message : String(err)}`;
    }
    seams.log?.(`Get-VM attempt ${attempt + 1} of ${VM_PROBE_ATTEMPTS}: ${probeLog(probe, why, Date.now() - started)}`);
    if (probe !== null && probe.timedOut) return planVms(probe);
    if (probe !== null && probe.code === 0) {
      if (parseVmStates(probe.stdout).size > 0) return planVms(probe);
      why = missingReason(VM_NAMES, probe);
    } else if (probe !== null) {
      why = exitReason(probe);
    }
    failure = why;
  }
  throw new VmProbeError(failure || "Get-VM did not answer");
}

/** Start-VM for the named agents: null when it ran, the reason when it did not.
 *  Profile-free like the probe: the user profile is the Tmux.ps1 BOM trap, and a
 *  VM start must not enter it. */
async function startVms(seams: BuildSeams, names: string[]): Promise<string | null> {
  try {
    const res = await seams.run(`Start-VM -Name ${names.join(",")}`, { timeoutMs: 120_000, noProfile: true });
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

async function monitorPhase(pi: any, ctx: any, seams: BuildSeams, repo: string, choice: string): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  await ctx.ui.notify("AzDO build queued — monitoring in background (toast on every check).", "info");
  const mode: Choice = choice === "Release" ? "Release" : "Lab";
  const starter = seams.startAzDoWatcher ?? startAzDoWatcher;
  starter(pi, ctx, seams, { followNugets: true, repoRoot: repo, choice: mode });
  startReleaseWatch(pi, ctx, seams, { repo, choice: mode });
  notes.push("AzDO monitoring in background — toasts on every check; follows the nuget + release publish chain, asserts the nugets, then publishes the GitHub release (the outcome is recorded)");
  return { ok: true, failed: false, notes };
}

async function pushThenQueue(
  ctx: any, seams: BuildSeams, repoRoot: string, choice: Choice, dxChanged: boolean, latest: string,
  label: string, notes: string[],
): Promise<PushCheck> {
  const p = profileOf(seams);
  const remote = p.pushRemote(choice);
  if (remote) {
    const pre = await pushDirtyPhase(ctx, seams, repoRoot, remote, (n) => commitMessage(label, dxChanged, latest, n), notes);
    if (pre.failed || pre.aborted) return pre;
    const push = await runStep(seams, repoRoot, `git push ${remote}`, `git push ${remote} HEAD:master`, 120000);
    if (!push.ok) {
      notes.push(`git push ${remote} failed (exit ${push.code}): ${push.said}`);
      return { failed: true, aborted: false };
    }
    notes.push(`pushed to ${remote}`);
  }
  const queueCmd = p.queueCmd(choice);
  const res = await runStep(seams, repoRoot, queueCmd, queueCmd, 600000);
  if (!res.ok) {
    notes.push(`${queueCmd} failed (exit ${res.code}): ${res.said}`);
    return { failed: true, aborted: false };
  }
  notes.push(`${queueCmd} done (exit ${res.code})`);
  return { failed: false, aborted: false };
}

async function queuePhase(
  ctx: any, seams: BuildSeams, repoRoot: string, choice: string, dxChanged: boolean, latest: string,
  label: string,
): Promise<{ failed: boolean; aborted: boolean; notes: string[] }> {
  const notes: string[] = [];
  const p = profileOf(seams);
  const queueCmd = p.queueCmd(choice as Choice);
  await ctx.ui.notify(`Publishing via ${queueCmd}…`, "info");
  const pick = await ctx.ui.select(`Publish: ${p.queueLabel(choice as Choice)}?`, ["Publish", "Abort"]);
  if (pick !== "Publish") {
    notes.push("publish aborted");
    return { failed: false, aborted: true, notes };
  }
  const ran = await pushThenQueue(ctx, seams, repoRoot, choice as Choice, dxChanged, latest, label, notes);
  return { failed: ran.failed, aborted: ran.aborted, notes };
}

export async function publishPhase(
  pi: any, ctx: any, seams: BuildSeams, choice: string, repoRoot: string,
  dxChanged: boolean, latest: string, skipBuild = false,
): Promise<{ ok: boolean; failed: boolean; notes: string[] }> {
  const notes: string[] = [];
  const label = skipBuild ? "Publish" : "Build fixes";
  // Both milestones in ONE notify: two of the same type back to back lose the
  // first, and from here on the two run at the same time.
  await ctx.ui.notify("Checking Hyper-V agents C11-C14…\nCommitting build state…", "info");
  // The gate is STARTED, not awaited: the agents boot while the commit prompt is
  // open. Its outcome is read after the commit, so a VM layer that cannot be read
  // costs the queue, never the commit. The catch folds a throw into the same
  // shape, so a dropped task (the commit aborted) never rejects unobserved.
  const vms = ensureVmsRunning(seams).catch((err) => ({ ok: false, failed: true, notes: [reasonOf(err)] }));
  const commit = await commitPhase(ctx, seams, repoRoot, dxChanged, latest, label);
  notes.push(...commit.notes);
  if (!commit.committed) return { ok: false, failed: commit.failed === true, notes };
  const vm = await vms;
  notes.push(...vm.notes);
  if (!vm.ok) return { ok: false, failed: true, notes };
  const queue = await queuePhase(ctx, seams, repoRoot, choice, dxChanged, latest, label);
  notes.push(...queue.notes);
  if (queue.aborted) return { ok: false, failed: false, notes };
  if (queue.failed) return { ok: false, failed: true, notes };
  const monitor = await monitorPhase(pi, ctx, seams, repoRoot, choice);
  notes.push(...monitor.notes);
  return { ok: monitor.ok, failed: monitor.failed, notes };
}
