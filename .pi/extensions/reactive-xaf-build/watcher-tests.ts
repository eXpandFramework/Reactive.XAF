/**
 * reactive-xaf-build/watcher-tests — behavior contract for the background
 * AzDO watcher, driven through the registered /devexpress command with a
 * mock pi: publish starts the watcher and returns immediately (chat not
 * locked); the watcher toasts on EVERY poll; a failed terminal steers; the
 * give-up deadline stops it; a new publish replaces the previous watcher.
 * Real watcher timers with SHORT injected intervals (the flow's default is
 * 60 s — a test with default opts would need a minute per poll); fake run
 * seam (CRLF STATUS= fixtures for the watcher's polls, scripted flow
 * commands for VM/git/prx). No real pwsh/AzDO/pi.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { setTimeout as sleep } from "node:timers/promises";
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";
import { startAzDoWatcher, stopAzDoWatcher, isAzDoWatcherActive } from "./watcher.js";

let ok = 0;
let fail = 0;
function check(label: string, cond: boolean, detail?: string): void {
  if (cond) {
    ok++;
    console.log("PASS " + label);
  } else {
    fail++;
    console.log("FAIL " + label + (detail ? " — " + detail : ""));
  }
}
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-watcher-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  return root;
}
function crlf(lines: string[]): string {
  return lines.join("\r\n") + "\r\n";
}
function running(id: number): string {
  return crlf([`STATUS=${id};inProgress;;`]);
}
function done(id: number, result: string): string {
  return crlf([`STATUS=${id};completed;${result};`]);
}
function mkPi(): any {
  const cmds = new Map<string, any>();
  const msgs: Array<{ content: string; opts: any }> = [];
  return {
    registerCommand: (n: string, d: any) => {
      cmds.set(n, d);
    },
    sendUserMessage: (content: string, opts: any) => {
      msgs.push({ content, opts });
    },
    _cmds: cmds,
    _userMessages: msgs,
  };
}
function mkCtx(repo: string): any {
  const notifies: Array<{ msg: string; type: string }> = [];
  return {
    cwd: repo,
    ui: {
      select: async () => "Publish",
      notify: async (msg: string, type: string) => {
        notifies.push({ msg, type });
      },
    },
    _notifies: notifies,
  };
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
function flowResult(cmd: string): any {
  if (cmd.startsWith(VM_PREFIX)) return { code: 0, stdout: VM_RUN, stderr: "" };
  if (cmd === "git status --short") return { code: 0, stdout: "", stderr: "" };
  if (cmd === "prx") return { code: 0, stdout: "Queued build", stderr: "" };
  return { code: 0, stdout: "", stderr: "" };
}
/** Run seam: the watcher's status polls serve from the queue; flow commands scripted. */
function mkSeams(statusQueue: string[]): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const calls: string[] = [];
  return {
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.includes("$cred = @{ Project")) {
        return { code: 0, stdout: statusQueue.shift() ?? running(35760), stderr: "" };
      }
      return flowResult(cmd);
    },
    calls,
  };
}
/** Short-interval watcher seam — the flow's default is 60 s per poll. */
function fastWatcher(p: any, c: any, s: any): ReturnType<typeof startAzDoWatcher> {
  return startAzDoWatcher(p, c, s, { intervalMs: 20, maxMs: 5000 });
}

(async () => {
  // Section: W1 — publish returns immediately; the watcher toasts on every poll and stops on success
  {
    const repo = mkRepo();
    const pi = mkPi();
    const seams = mkSeams([running(35760), running(35760), done(35760, "succeeded")]);
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, { run: seams.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), repoRoot: repo, startAzDoWatcher: fastWatcher });
    const result = await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    check("W1: publish returns immediately, monitoring in background", result.includes("monitoring in background") && result.includes("published"), result);
    check("W1: watcher active right after publish", isAzDoWatcherActive());
    await sleep(250);
    const runningToasts = ctx._notifies.filter((n) => n.msg.includes("inProgress"));
    check("W1: toast on every poll (both running checks toasted)", runningToasts.length >= 2, JSON.stringify(ctx._notifies));
    check("W1: terminal succeeded toast + watcher stopped", ctx._notifies.some((n) => n.msg.includes("succeeded")) && !isAzDoWatcherActive(), JSON.stringify(ctx._notifies));
    check("W1: no steer on success", pi._userMessages.length === 0, JSON.stringify(pi._userMessages));
  }
  // Section: W2 — a failed terminal toasts a warning and steers
  {
    const repo = mkRepo();
    const pi = mkPi();
    const seams = mkSeams([done(35761, "failed")]);
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, { run: seams.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(200);
    check("W2: failure toast is a warning", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("FAILED")), JSON.stringify(ctx._notifies));
    check("W2: failure steers (deliverAs steer)", pi._userMessages.length === 1 && pi._userMessages[0].opts?.deliverAs === "steer" && pi._userMessages[0].content.includes("FAILED"), JSON.stringify(pi._userMessages));
    check("W2: watcher stopped", !isAzDoWatcherActive());
  }
  // Section: W3 — the give-up deadline stops the watcher with a warning
  {
    const repo = mkRepo();
    const pi = mkPi();
    const seams = mkSeams([]);
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: seams.run,
      fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
      repoRoot: repo,
      startAzDoWatcher: (p: any, c: any, s: any) => startAzDoWatcher(p, c, s, { intervalMs: 10, maxMs: 60 }),
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(250);
    check("W3: gave-up warning + watcher stopped", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("gave up")) && !isAzDoWatcherActive(), JSON.stringify(ctx._notifies));
  }
  // Section: W4 — a new publish replaces the previous watcher; stop works
  {
    const repo = mkRepo();
    const pi1 = mkPi();
    const seams1 = mkSeams([]);
    registerBuildCommand(pi1, { run: seams1.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi1._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    check("W4: first watcher active", isAzDoWatcherActive());
    const pi2 = mkPi();
    const seams2 = mkSeams([]);
    registerBuildCommand(pi2, { run: seams2.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi2._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    check("W4: second publish keeps exactly one watcher active", isAzDoWatcherActive());
    stopAzDoWatcher();
    check("W4: stopAzDoWatcher stops it", !isAzDoWatcherActive());
  }
  // Section: W5 — registration through the real index boot
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("W5: devexpress command registered via index.ts", typeof cmd?.handler === "function");
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
