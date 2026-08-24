/**
 * reactive-xaf-build/pane — psmux pane machinery for the build step.
 *
 * The brx build runs in a NEW psmux pane split to the right; the output
 * streams there live. Completion is signaled by a transient exit-code
 * marker (consume-on-read, in %TEMP%). Green leaves the pane open for the
 * user to close (/devexpress → "Close build pane"); failure keeps it for
 * reuse. In-process fallback happens in build.ts when the pane cannot be
 * opened.
 *
 * All seams are injectable (tests pass fakes via registerBuildCommand) —
 * the real psmux CLI is never touched by the test suite.
 */

import { spawn } from "node:child_process";
import { once } from "node:events";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";

export interface RunResult {
  code: number;
  stdout: string;
  stderr: string;
}

export interface RunOpts {
  cwd?: string;
  timeoutMs?: number;
}

export type PaneOpener = (repo: string) => Promise<string | null>;
export type PaneRunner = (pane: string, cmd: string) => Promise<void>;
export type PaneWaiter = (pane: string, marker: string, timeoutMs: number) => Promise<{ code: number | null; timedOut: boolean }>;
export type PaneCapturer = (pane: string) => Promise<string>;
export type PaneCloser = (pane: string) => Promise<void>;

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
  return runArgv(["pwsh", "-Command", cmd], opts.timeoutMs ?? 60000, opts.cwd);
}

/** psmux CLI args with the socket-isolation seam (tests / parallel servers). */
function psmuxArgs(args: string[]): string[] {
  const sock = process.env.PSMUX_SOCKET;
  return sock ? ["-L", sock, ...args] : args;
}

export async function defaultOpenBuildPane(repo: string): Promise<string | null> {
  const self = process.env.TMUX_PANE;
  const target = self ? ["-t", self] : [];
  const res = await runArgv(psmuxArgs(["split-window", "-h", ...target, "-P", "-F", "#{pane_id}", "-c", repo.replace(/\\/g, "/")]), 15000);
  const id = res.stdout.trim().split("\n").pop() ?? "";
  return res.code === 0 && id ? id : null;
}

export async function defaultRunInPane(pane: string, cmd: string): Promise<void> {
  await runArgv(psmuxArgs(["send-keys", "-t", pane, cmd, "Enter"]), 15000);
}

export async function defaultWaitForPaneExit(pane: string, marker: string, timeoutMs: number): Promise<{ code: number | null; timedOut: boolean }> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (fs.existsSync(marker)) {
      try {
        const code = Number(fs.readFileSync(marker, "utf-8").trim());
        fs.rmSync(marker, { force: true });
        return { code, timedOut: false };
      } catch {
        return { code: null, timedOut: false };
      }
    }
    await sleep(2000);
  }
  return { code: null, timedOut: true };
}

export async function defaultCapturePane(pane: string): Promise<string> {
  const res = await runArgv(psmuxArgs(["capture-pane", "-t", pane, "-p", "-S", "-40"]), 15000);
  return res.stdout;
}

export async function defaultClosePane(pane: string): Promise<void> {
  await runArgv(psmuxArgs(["kill-pane", "-t", pane]), 15000);
}

/** A transient consume-on-read marker path for the pane's exit code. */
export function exitMarkerPath(): string {
  return path.join(os.tmpdir(), `rxaf-build-${Date.now()}.exit`);
}
