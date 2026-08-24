/**
 * reactive-xaf-build/delegate-tests — behavior contract for the window
 * delegation fallback (companion of delegate.ts): a spawned window that dies
 * during the boot grace period must NOT receive the flow —
 * defaultDelegateWindow returns null and the /devexpress menu flow falls back
 * to the invoking session, completing there. A surviving window is delegated
 * to. Mock-pi harness with injected delegate deps (run/windowExists/
 * killWindow/graceMs) — the real psmux CLI is never touched.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/delegate-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";
import { defaultDelegateWindow } from "./delegate.js";
import type { DelegateDeps } from "./delegate.js";

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
function mkPi(): any {
  const cmds = new Map<string, any>();
  return {
    registerCommand: (n: string, d: any) => { cmds.set(n, d); },
    _cmds: cmds,
  };
}
function mkCtx(selects: string[], cwd: string): any {
  const prompts: string[] = [];
  const notifies: string[] = [];
  return {
    cwd,
    ui: {
      select: async (title: string, _opts: string[]) => {
        prompts.push(title);
        return selects.shift();
      },
      notify: (m: string) => { notifies.push(m); },
    },
    _prompts: prompts,
    _notifies: notifies,
  };
}
function mkRunner(script: Array<{ match: string; result: any }>): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const calls: string[] = [];
  let i = 0;
  return {
    run: async (cmd: string) => {
      calls.push(cmd);
      const entry = script[i];
      i++;
      if (entry && (cmd === entry.match || (entry.match.includes("*") && cmd.startsWith(entry.match.replace("*", ""))))) {
        return entry.result;
      }
      return { code: 0, stdout: "", stderr: "" };
    },
    calls,
  };
}
function mkPaneSeams(): any {
  return {
    openBuildPane: async () => "pane1",
    runInPane: async () => {},
    waitForPaneExit: async () => ({ code: 0, timedOut: false }),
    capturePane: async () => "",
    closePane: async () => {},
    waitForAzDoBuild: async () => ({ id: 1, result: "succeeded", reason: "" }),
    delegateWindow: async () => null,
  };
}
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-delegate-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const GREEN_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: "", stderr: "" } },
  { match: "prx", result: { code: 0, stdout: "Queued build 123", stderr: "" } },
];

(async () => {
  const prevPane = process.env.TMUX_PANE;
  process.env.TMUX_PANE = "%1";
  // Section: S0 — extension boots through the real index (mock pi runtime)
  {
    const pi = mkPi();
    activate(pi);
    check("S0: devexpress command registered via index.ts", typeof pi._cmds.get("devexpress")?.handler === "function");
  }
  // Section: S1 — spawned window dies during grace → menu falls back to the invoking session
  {
    const repo = mkRepo();
    const runner = mkRunner(GREEN_PUBLISH);
    const killed: string[] = [];
    const deps: DelegateDeps = {
      run: async () => ({ code: 0, stdout: "7\n", stderr: "" }),
      windowExists: async () => false,
      killWindow: async (idx: string) => { killed.push(idx); },
      graceMs: 100,
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, ...mkPaneSeams(), delegateWindow: (r: string, t: string) => defaultDelegateWindow(r, t, deps) });
    const ctx = mkCtx(["Publish", "Lab", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("S1: dead window killed, flow fell back and published here", killed.length === 1 && killed[0] === "7" && result.includes("published"), result + " | killed: " + JSON.stringify(killed));
    check("S1: prx ran in the invoking session, no brx", runner.calls.includes("prx") && !runner.calls.some((c) => c.startsWith("brx")), runner.calls.join(" | "));
  }
  // Section: S2 — surviving window → delegated, flow not run here
  {
    const repo = mkRepo();
    const runner = mkRunner([]);
    const deps: DelegateDeps = {
      run: async () => ({ code: 0, stdout: "8\n", stderr: "" }),
      windowExists: async () => true,
      graceMs: 50,
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, ...mkPaneSeams(), delegateWindow: (r: string, t: string) => defaultDelegateWindow(r, t, deps) });
    const ctx = mkCtx(["Publish", "Lab"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("S2: surviving window delegated, nothing ran here", result.includes("delegated to window 8") && !runner.calls.includes("prx"), result + " | " + runner.calls.join(" | "));
  }
  // Section: S3 — outside psmux → null without spawning
  {
    delete process.env.TMUX_PANE;
    let spawned = false;
    const deps: DelegateDeps = {
      run: async () => {
        spawned = true;
        return { code: 0, stdout: "9\n", stderr: "" };
      },
    };
    const result = await defaultDelegateWindow(mkRepo(), "task", deps);
    check("S3: no TMUX_PANE → null, nothing spawned", result === null && !spawned, String(result));
  }
  if (prevPane === undefined) delete process.env.TMUX_PANE;
  else process.env.TMUX_PANE = prevPane;
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
