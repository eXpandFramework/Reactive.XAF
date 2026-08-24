/**
 * reactive-xaf-build/build-tests — behavior contract for the /devexpress workflow.
 *
 * Exercises the REAL command surface through a mock pi: T1 drives the real
 * index.ts boot (activate(pi)); the remaining tests register the command via
 * registerBuildCommand and invoke the captured handler with a stubbed ctx.ui
 * (scripted select answers, notify collector) and injected seams (fake command
 * runner, fake feed fetcher, fixture props in a temp repo). The real nuget.org,
 * pwsh, Hyper-V VMs and git are never touched.
 *
 * Behaviors pinned:
 *   T1 /devexpress registers a command with a handler (real index.ts boot)
 *   T2 outside the Reactive.XAF repo → loud error, zero commands ran
 *   T3 Lab happy path: menu Build → RX-XAF → Lab; DX 26.1.4 > pins 26.1.3
 *     → update prompt → props rewritten (non-DX pins untouched) → brx
 *     → VM start (C11 off) → commit "Update DX to 26.1.4" → confirm → prx → "published"
 *   T4 DX already latest → no update prompt, props untouched, build + publish run
 *   T5 mixed pins → file untouched, surfaced, build still runs
 *   T6 build failure (warnings) → FAILED result with output tail, no publish commands
 *   T7 Release flow → brx -Release and prx -Release
 *   T8 abort at the DX prompt → nothing ran
 *   T9 VMs already running → no Start-VM
 *   T10 nothing to commit → commit skipped, prx still runs
 *
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/build-tests.ts
 */

/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync } from "node:fs";
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

// The flow steers failures through globalThis.__steer — collect them here.
const steers: Array<{ type: string; content: string }> = [];
(globalThis as any).__steer = (_pi: any, type: string, content: string) => {
  steers.push({ type, content });
};

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

function mkFetch(versions: string[]): (url: string) => Promise<string> {
  return async (_url: string) => JSON.stringify({ versions });
}

function mkRepo(pins: Array<[string, string]>): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-build-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  const lines = pins.map(([id, v]) => `    <PackageVersion Include="${id}" Version="${v}" />`);
  const props = "<Project>\n  <ItemGroup>\n" + lines.join("\n") + "\n  </ItemGroup>\n</Project>\n";
  writeFileSync(join(root, "Directory.Packages.props"), props);
  return root;
}

function propsText(root: string): string {
  return readFileSync(join(root, "Directory.Packages.props"), "utf-8");
}

const VM_OFF = "C11=Off\nC12=Running\nC13=Running\nC14=Running\n";
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const MENU = ["Build", "RX-XAF"];
const DX_PINS: Array<[string, string]> = [
  ["DevExpress.ExpressApp", "26.1.3"],
  ["DevExpress.Xpo", "26.1.3"],
  ["DevExpress.Utils", "26.1.3"],
  ["Xpand.Collections", "1.0.4"],
];

function okResult(stdout = ""): any {
  return { code: 0, stdout, stderr: "" };
}

(async () => {
  // Section: T1 — /devexpress registration through the real index boot
  console.log("T1: /devexpress registration\n");
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("devexpress command registered via index.ts", typeof cmd?.handler === "function");
  }

  // Section: T2 — repo guard
  console.log("T2: repo guard\n");
  {
    const runner = mkRunner([]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]) });
    const ctx = mkCtx(["Build", "RX-XAF", "Lab"], tmpdir());
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("loud error outside the repo", result.includes("not inside the Reactive.XAF repo"), result);
    check("zero commands ran", runner.calls.length === 0);
  }

  // Section: T3 — Lab happy path with DX update
  console.log("T3: Lab happy path\n");
  {
    steers.length = 0;
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: okResult("Build succeeded") },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_OFF, stderr: "" } },
      { match: "Start-VM -Name C11", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M Directory.Packages.props\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult("Queued build 123") },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.2", "26.1.3", "26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Update", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const after = propsText(repo);
    check("result published", result.includes("published"), result);
    check("update prompt shown", ctx._prompts.some((p) => p.includes("update all DevExpress")), ctx._prompts.join(" | "));
    check("all DX pins rewritten to 26.1.4", after.includes('DevExpress.ExpressApp" Version="26.1.4"') && after.includes('DevExpress.Xpo" Version="26.1.4"'), after);
    check("non-DX pin untouched", after.includes('Xpand.Collections" Version="1.0.4"'), after);
    check("brx first command", runner.calls[0] === "brx", runner.calls.join(" | "));
    check("Start-VM invoked for C11", runner.calls.some((c) => c.startsWith("Start-VM -Name C11")), runner.calls.join(" | "));
    check("commit message carries DX", runner.calls.some((c) => c.startsWith('git commit -m "Update DX to 26.1.4"')), runner.calls.join(" | "));
    check("prx ran last", runner.calls[runner.calls.length - 1] === "prx", runner.calls.join(" | "));
    check("success: no failure steer", steers.length === 0, JSON.stringify(steers));
  }

  // Section: T4 — DX already latest
  console.log("T4: DX already latest\n");
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("no update prompt", !ctx._prompts.some((p) => p.includes("update all DevExpress")), ctx._prompts.join(" | "));
    check("props untouched", propsText(repo).includes('Version="26.1.3"') && !propsText(repo).includes("26.1.4"), "props changed");
    check("build still ran", runner.calls[0] === "brx", runner.calls.join(" | "));
    check("published", result.includes("published"), result);
  }

  // Section: T5 — mixed pins left untouched
  console.log("T5: mixed pins\n");
  {
    const repo = mkRepo([
      ["DevExpress.ExpressApp", "26.1.3"],
      ["DevExpress.Xpo", "26.1.2"],
    ]);
    const runner = mkRunner([
      { match: "brx", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const after = propsText(repo);
    check("mixed versions surfaced", result.includes("mixed"), result);
    check("file untouched", after.includes('Version="26.1.2"'), after);
    check("build still ran", runner.calls[0] === "brx", runner.calls.join(" | "));
    check("published", result.includes("published"), result);
  }

  // Section: T6 — build failure with warnings
  console.log("T6: build failure\n");
  {
    steers.length = 0;
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: { code: 1, stdout: "warning CS0219: unused variable", stderr: "" } },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("FAILED surfaced", result.includes("Build FAILED (exit 1)"), result);
    check("warning tail shown", result.includes("warning CS0219"), result);
    check("no publish commands", !runner.calls.some((c) => c.startsWith("Get-VM") || c === "prx"), runner.calls.join(" | "));
    check("warning notify", ctx._notifies.some((n) => n.includes("FAILED")), ctx._notifies.join(" | "));
    check("failure steer fired", steers.length === 1 && steers[0].type === "reactive-xaf-build:build-failed" && steers[0].content.includes("Build FAILED"), JSON.stringify(steers));
  }

  // Section: T7 — Release flow (DX update skipped)
  console.log("T7: Release flow\n");
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx -Release", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx -Release", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Release", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("brx -Release ran", runner.calls.includes("brx -Release"), runner.calls.join(" | "));
    check("prx -Release ran", runner.calls.includes("prx -Release"), runner.calls.join(" | "));
    check("published", result.includes("published"), result);
  }

  // Section: T8 — abort at the DX prompt
  console.log("T8: abort at DX prompt\n");
  {
    steers.length = 0;
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Abort"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("abort surfaced", result.includes("aborted at the DX update prompt"), result);
    check("nothing ran", runner.calls.length === 0, runner.calls.join(" | "));
    check("user abort: no steer", steers.length === 0, JSON.stringify(steers));
  }

  // Section: T9 — VMs already running (DX update skipped)
  console.log("T9: VMs already running\n");
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("no Start-VM", !runner.calls.some((c) => c.startsWith("Start-VM")), runner.calls.join(" | "));
    check("already-running noted", result.includes("already running"), result);
    check("published", result.includes("published"), result);
  }

  // Section: T10 — nothing to commit (DX update skipped)
  console.log("T10: nothing to commit\n");
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1 });
    const ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("no commit prompt", !ctx._prompts.some((p) => p.includes("Commit with message")), ctx._prompts.join(" | "));
    check("no git add", !runner.calls.includes("git add -A"), runner.calls.join(" | "));
    check("prx still ran", runner.calls.includes("prx"), runner.calls.join(" | "));
    check("published", result.includes("published"), result);
  }

  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
