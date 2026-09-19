/**
 * reactive-xaf-build/pane — psmux pane machinery for the build step.
 *
 * The brx build runs in a NEW psmux pane split to the right of the invoking
 * window, and its output streams there live. This module owns the pane and the
 * machine it exposes: open a pane, send a command to it, capture its tail,
 * close it, and probe what the pane's process is doing (liveness, pid, CPU).
 * Everything about the RUN itself — its id, temp dir, supervisor script, exit
 * marker, TTL cleanup and the background watch — lives in `run.ts`.
 *
 * All seams are injectable (tests pass fakes via registerBuildCommand) — the
 * real psmux CLI is never touched by the test suite.
 */

import { spawn } from "node:child_process";
import { once } from "node:events";

export interface RunResult {
  code: number;
  stdout: string;
  stderr: string;
}

export interface RunOpts {
  cwd?: string;
  timeoutMs?: number;
  /** Run pwsh without the user profile and without a prompt. The VM probe sets
   *  it: what the profile loads must never decide whether a probe answers (a
   *  profile stall is what killed the probe in the incident). */
  noProfile?: boolean;
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

/** Spawn an argv array and capture stdout/stderr (bounded, timeout-killed). */
export async function runArgv(argv: string[], timeoutMs: number, cwd?: string): Promise<RunResult> {
  const child = spawn(argv[0], argv.slice(1), { cwd, windowsHide: true });
  let stdout = "";
  let stderr = "";
  child.stdout.on("data", (d: Buffer) => {
    stdout += d.toString();
    if (stdout.length > 100000) stdout = stdout.slice(-100000);
  });
  child.stderr.on("data", (d: Buffer) => {
    stderr += d.toString();
    if (stderr.length > 50000) stderr = stderr.slice(-50000);
  });
  const timer = setTimeout(() => {
    spawn("taskkill", ["/PID", String(child.pid), "/T", "/F"], { stdio: "ignore" });
  }, timeoutMs);
  const [code] = await once(child, "close");
  clearTimeout(timer);
  return { code: (code as number | null) ?? -1, stdout, stderr };
}

/** Run a command through pwsh (the default command runner seam). */
export async function runProcess(cmd: string, opts: RunOpts = {}): Promise<RunResult> {
  const argv = opts.noProfile
    ? ["pwsh", "-NoProfile", "-NonInteractive", "-Command", cmd]
    : ["pwsh", "-Command", cmd];
  return runArgv(argv, opts.timeoutMs ?? 60000, opts.cwd);
}

/** psmux CLI args with the socket-isolation seam (tests / parallel servers). */
export function psmuxArgs(args: string[]): string[] {
  const sock = process.env.PSMUX_SOCKET;
  return sock ? ["-L", sock, ...args] : args;
}

export async function defaultOpenBuildPane(repo: string): Promise<string | null> {
  const self = process.env.TMUX_PANE;
  const target = self ? ["-t", self] : [];
  const res = await runArgv(psmuxArgs(["psmux", "split-window", "-h", ...target, "-P", "-F", "#{pane_id}", "-c", repo.replace(/\\/g, "/")]), 15000);
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
