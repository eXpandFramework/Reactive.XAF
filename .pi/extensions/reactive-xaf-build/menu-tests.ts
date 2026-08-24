/**
 * reactive-xaf-build/menu-tests — behavior contract for the /devexpress
 * skip-build publish surface (companion of menu.ts): the top-level Publish
 * menu (Publish → RX-XAF → Lab | Release) and the /devexpress publish
 * lab|release arg run the publish phase (VM check → commit → prx → AzDO
 * monitor) WITHOUT the DX feed check or the local brx build. Mock-pi harness
 * (build-tests.ts sits at the 400-line gate and runs its suite on import, so
 * it cannot be imported here) — the real nuget.org, pwsh, psmux, VMs and git
 * are never touched.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";

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
  const opened: string[] = [];
  const sent: string[] = [];
  return {
    openBuildPane: async () => {
      opened.push("pane1");
      return "pane1";
    },
    runInPane: async (_pane: string, cmd: string) => { sent.push(cmd); },
    waitForPaneExit: async () => ({ code: 0, timedOut: false }),
    capturePane: async () => "",
    closePane: async () => {},
    waitForAzDoBuild: async () => ({ id: 1, result: "succeeded", reason: "" }),
    delegateWindow: async () => null,
    opened,
    sent,
  };
}
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-skip-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const MENU = ["Publish", "Lab"];
const GREEN_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: "", stderr: "" } },
  { match: "prx", result: { code: 0, stdout: "Queued build 123", stderr: "" } },
];
function okResult(stdout = ""): any {
  return { code: 0, stdout, stderr: "" };
}

(async () => {
  // Section: S0 — extension boots through the real index (mock pi runtime)
  {
    const pi = mkPi();
    activate(pi);
    check("S0: devexpress command registered via index.ts", typeof pi._cmds.get("devexpress")?.handler === "function");
  }
  // Section: S1 — menu Publish → RX-XAF → Lab: no pane, no brx, prx runs, monitor awaited
  {
    const repo = mkRepo();
    const runner = mkRunner(GREEN_PUBLISH);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("S1: no brx, no pane opened or sent", !runner.calls.some((c) => c.startsWith("brx")) && pane.opened.length === 0 && pane.sent.length === 0, JSON.stringify({ calls: runner.calls, opened: pane.opened, sent: pane.sent }));
    check("S1: prx ran, monitor awaited, published", runner.calls.includes("prx") && result.includes("AzDO build 1 succeeded") && result.includes("published"), result);
  }
  // Section: S2 — direct arg /devexpress publish lab (publish confirm answered)
  {
    const repo = mkRepo();
    const runner = mkRunner(GREEN_PUBLISH);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    check("S2: no brx, prx ran, published", !runner.calls.some((c) => c.startsWith("brx")) && runner.calls.includes("prx") && result.includes("published"), result);
  }
  // Section: S3 — skip-build commit label "Publish (N files)"
  {
    const repo = mkRepo();
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M Build/nuspec/Xpand.XAF.nuspec\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx([...MENU, "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("S3: commit labeled Publish, not Build fixes", runner.calls.some((c) => c.startsWith('git commit -m "Publish (1 files)"')), runner.calls.join(" | "));
    check("S3: published", result.includes("published"), result);
  }
  // Section: S4 — Publish menu pick delegates with the publish task
  {
    const repo = mkRepo();
    const tasks: string[] = [];
    const runner = mkRunner([]);
    const pi = mkPi();
    const dw = async (_r: string, task: string) => {
      tasks.push(task);
      return "W8";
    };
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: repo, pollMs: 1, delegateWindow: dw });
    const ctx = mkCtx([...MENU], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("S4: delegated with /devexpress publish lab task", result.includes("W8") && tasks.some((t) => t.includes("/devexpress publish lab")), result + " | " + JSON.stringify(tasks));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
