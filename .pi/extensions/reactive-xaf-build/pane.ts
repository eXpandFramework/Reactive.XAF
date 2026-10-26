/**
 * reactive-xaf-build/pane — psmux pane machinery for the build step.
 *
 * The brx build runs in a NEW psmux pane split to the right of the invoking
 * window, and its output streams there live — the invoking pane keeps the
 * keyboard, because reading the build must never cost the user their cursor.
 * This module owns the pane and the machine it exposes: open a pane, send a
 * command to it, capture its tail, close it, and probe what the pane's process
 * is doing (liveness, pid, CPU). Everything about the RUN itself — its id, temp
 * dir, supervisor script, exit marker, TTL cleanup and the background watch —
 * lives in `run.ts`.
 *
 * All seams are injectable (tests pass fakes via registerBuildCommand) — the
 * real psmux CLI is never touched by the test suite.
 */

import { spawn, type ChildProcess } from "node:child_process";
import { once } from "node:events";

export interface RunResult {
  code: number;
  stdout: string;
  stderr: string;
  /** The deadline fired and the child never reported its death: `code` is -1 and
   *  what arrived before the kill is all there is. A caller must say SO —
   *  reading `code` alone would report a timeout as an exit code that never
   *  happened. */
  timedOut?: boolean;
}

export interface RunOpts {
  cwd?: string;
  timeoutMs?: number;
  /** Extra variables for the spawned process, merged over the inherited
   *  environment. The build hands the profile's `buildEnv` here, so a no-pane
   *  run starts with what a pane run starts with. */
  env?: Record<string, string>;
  /** Run pwsh without the user profile and without a prompt. The VM probe sets
   *  it: what the profile loads must never decide whether a probe answers (a
   *  profile stall is what killed the probe in the incident). */
  noProfile?: boolean;
  /** Which PowerShell runs the command. The Hyper-V layer sets "powershell":
   *  under pwsh 7 the Hyper-V cmdlets reach the module through the
   *  Windows-PowerShell compat session and `Get-VM` there never returned (three
   *  measured attempts: no output, no exit), while `powershell.exe` answered in
   *  about a second. Everything else stays on pwsh. */
  shell?: "pwsh" | "powershell";
  /** How a child past its deadline is taken down. Production leaves it unset
   *  (`taskkill /T /F`); a test swaps in a no-op to prove the call still answers
   *  at the deadline when the kill never lands. */
  kill?: KillFn;
}

export type PaneOpener = (repo: string) => Promise<string | null>;
export type PaneRunner = (pane: string, cmd: string) => Promise<void>;
export type PaneCapturer = (pane: string) => Promise<string>;
export type PaneCloser = (pane: string) => Promise<void>;
export type PaneProber = (pane: string) => Promise<PaneState>;
export type CpuSampler = (pid: number) => Promise<number | null>;

/** Liveness of a pane and, while it still answers, its shell pid. */
export interface PaneState {
  alive: boolean;
  pid: number | null;
}

const BUILD_PANE_KEY = Symbol.for("reactive-xaf-build.build-pane");

export function getBuildPane(): string | null {
  return (globalThis as any)[BUILD_PANE_KEY] ?? null;
}

export function setBuildPane(pane: string | null): void {
  if (pane === null) delete (globalThis as any)[BUILD_PANE_KEY];
  else (globalThis as any)[BUILD_PANE_KEY] = pane;
}

export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/** Keep the LAST `cap` characters: a chatty command's tail is the part that says
 *  what happened. */
function bound(text: string, cap: number): string {
  return text.length > cap ? text.slice(-cap) : text;
}

/** Spawn an argv array and capture stdout/stderr (bounded, timeout-killed). */
export async function runArgv(argv: string[], timeoutMs: number, cwd?: string, env?: Record<string, string>, kill: KillFn = treeKill): Promise<RunResult> {
  const child = spawn(argv[0], argv.slice(1), { cwd, env: { ...process.env, ...env }, windowsHide: true });
  const out = { stdout: "", stderr: "" };
  child.stdout.on("data", (d: Buffer) => {
    out.stdout = bound(out.stdout + d.toString(), 100000);
  });
  child.stderr.on("data", (d: Buffer) => {
    out.stderr = bound(out.stderr + d.toString(), 50000);
  });
  return await settleChild(child, out, timeoutMs, kill);
}

/** How the deadline takes a child down. A seam, because the one thing worth
 *  measuring is that the call settles when the kill does NOT land. */
export type KillFn = (child: ChildProcess) => void;

/** The production kill: the whole tree, because a child killed alone can leave a
 *  shell holding the pipe open, and that is what a missed kill looks like. */
const treeKill: KillFn = (child) => {
  if (child.pid !== undefined) spawn("taskkill", ["/PID", String(child.pid), "/T", "/F"], { stdio: "ignore" });
};

/** Kill the child and close its pipes, so a death that never reports cannot keep
 *  the call open. */
function killTree(child: ChildProcess, kill: KillFn): void {
  kill(child);
  child.stdout?.destroy();
  child.stderr?.destroy();
}

/** The wait: ONE awaited close, bounded by a timeout signal (the same shape
 *  vanguard's scout spawner uses). A child that outlives its budget is killed
 *  and answered `timedOut`, so a kill that misses cannot turn the call into a
 *  hang, which is exactly what the publish did for minutes on screen. A spawn
 *  failure still rejects, as it always did. */
async function settleChild(child: ChildProcess, out: { stdout: string; stderr: string }, timeoutMs: number, kill: KillFn): Promise<RunResult> {
  let spawnError: Error | undefined;
  child.once("error", (err: Error) => {
    spawnError = err;
  });
  try {
    const [code] = await once(child, "close", { signal: AbortSignal.timeout(timeoutMs) });
    return { code: (code as number | null) ?? -1, stdout: out.stdout, stderr: out.stderr };
  } catch {
    if (spawnError) throw spawnError;
    killTree(child, kill);
    return { code: -1, stdout: out.stdout, stderr: out.stderr, timedOut: true };
  }
}

/** Run a command through PowerShell (the default command runner seam): pwsh
 *  unless the caller names the Windows PowerShell, which is the only shell the
 *  Hyper-V cmdlets answer in on this machine. */
export async function runProcess(cmd: string, opts: RunOpts = {}): Promise<RunResult> {
  const shell = opts.shell ?? "pwsh";
  const argv = opts.noProfile
    ? [shell, "-NoProfile", "-NonInteractive", "-Command", cmd]
    : [shell, "-Command", cmd];
  return runArgv(argv, opts.timeoutMs ?? 60000, opts.cwd, opts.env, opts.kill);
}

/** Apply the socket-isolation seam (tests / parallel servers) to a psmux argv.
 *  `-L <socket>` goes directly AFTER the binary and never in front of it: the
 *  caller spawns argv[0], so a leading `-L` is not an executable
 *  (`spawn -L ENOENT`) and the seam only ever worked while the socket was
 *  unset. Everything after the binary is untouched. */
export function psmuxArgs(argv: string[]): string[] {
  const sock = process.env.PSMUX_SOCKET;
  if (!sock || argv.length === 0) return argv;
  const [binary, ...rest] = argv;
  return [binary, "-L", sock, ...rest];
}

/** Split a pane to the RIGHT of the caller and hand back its id. `-d` is the
 *  whole point: a plain split makes the NEW pane active, so the user's next
 *  keystroke would land in the build's output pane instead of their prompt. */
export async function defaultOpenBuildPane(repo: string): Promise<string | null> {
  const self = process.env.TMUX_PANE;
  const target = self ? ["-t", self] : [];
  const argv = ["psmux", "split-window", "-d", "-h", ...target, "-P", "-F", "#{pane_id}", "-c", repo.replace(/\\/g, "/")];
  const res = await runArgv(psmuxArgs(argv), 15000);
  const id = res.stdout.trim().split("\n").pop() ?? "";
  return res.code === 0 && id ? id : null;
}

export async function defaultRunInPane(pane: string, cmd: string): Promise<void> {
  await runArgv(psmuxArgs(["psmux", "send-keys", "-t", pane, cmd, "Enter"]), 15000);
}

export async function defaultCapturePane(pane: string): Promise<string> {
  const res = await runArgv(psmuxArgs(["psmux", "capture-pane", "-t", pane, "-p", "-S", "-40"]), 15000);
  return res.stdout;
}

export async function defaultClosePane(pane: string): Promise<void> {
  await runArgv(psmuxArgs(["psmux", "kill-pane", "-t", pane]), 15000);
}

/** Is the pane still there, and what is its shell pid? A `pane_dead` pane
 *  (shell gone, remain-on-exit) answers, but is dead all the same. */
export async function defaultProbePane(pane: string): Promise<PaneState> {
  const res = await runArgv(psmuxArgs(["psmux", "display-message", "-t", pane, "-p", "#{pane_dead} #{pane_pid}"]), 10000);
  if (res.code !== 0) return { alive: false, pid: null };
  const [dead, pid] = res.stdout.trim().split(/\s+/);
  const parsed = Number(pid);
  return { alive: dead !== "1", pid: Number.isFinite(parsed) ? parsed : null };
}

/** CPU seconds burned by the pane's shell and its descendants. A hung wait
 *  burns none, a slow compile keeps burning: the stall signal that does not
 *  depend on the build printing anything. */
export async function defaultSampleCpu(pid: number): Promise<number | null> {
  const script = `$r=${pid};$all=@($r)+@(Get-CimInstance Win32_Process -Filter "ParentProcessId=$r" -ErrorAction SilentlyContinue|Select-Object -ExpandProperty ProcessId);$s=0.0;foreach($p in $all){$x=Get-Process -Id $p -ErrorAction SilentlyContinue;if($x){$s+=$x.CPU}};("{0:F1}" -f $s)`;
  const res = await runArgv(["pwsh", "-NoProfile", "-Command", script], 10000);
  const value = Number(res.stdout.trim());
  return res.code === 0 && Number.isFinite(value) ? value : null;
}
