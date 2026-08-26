/**
 * reactive-xaf-build/build-tests — behavior contract for the /devexpress workflow.
 * Mock-pi harness with injected seams (fake command runner, feed fetcher, pane
 * seams, fixture props) — the real nuget.org, pwsh, psmux, VMs and git are
 * never touched. T1-T13 build/commit/publish/failure/abort/pane flows;
 * T14-T17 AzDO monitor + status; T18-T19 menu delegation; T20 fail-reason
 * extraction (wrapper noise filtered, real error delivered).
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
function mkPi(): any {
  const cmds = new Map<string, any>();
  const userMessages: Array<{ content: string; opts: any }> = [];
  return {
    registerCommand: (n: string, d: any) => { cmds.set(n, d); },
    sendUserMessage: (content: string, opts: any) => { userMessages.push({ content, opts }); },
    _cmds: cmds,
    _userMessages: userMessages,
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
function mkPaneSeams(overrides: Partial<{ open: string | null; exitCode: number | null; timedOut: boolean; capture: string }> = {}): any {
  const opened: string[] = [];
  const sent: string[] = [];
  const closed: string[] = [];
  const watcherStarts: number[] = [];
  return {
    openBuildPane: async () => {
      if (overrides.open === null) return null;
      const id = overrides.open ?? "pane1";
      opened.push(id);
      return id;
    },
    runInPane: async (_pane: string, cmd: string) => { sent.push(cmd); },
    waitForPaneExit: async () => ({ code: overrides.exitCode ?? 0, timedOut: overrides.timedOut ?? false }),
    capturePane: async () => overrides.capture ?? "",
    closePane: async (pane: string) => { closed.push(pane); },
    startAzDoWatcher: async () => {
      watcherStarts.push(1);
      return { stop: () => {}, active: () => false, lastBuildId: () => null };
    },
    delegateWindow: async () => null,
    opened,
    sent,
    closed,
    watcherStarts,
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
const GREEN_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: okResult("") },
  { match: "prx", result: okResult() },
];
function mkMonitor(): { pi: any; repo: string; starts: number[] } {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const pi = mkPi();
  registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
  return { pi, repo, starts: pane.watcherStarts };
}

(async () => {
  // Section: T1 — /devexpress registration through the real index boot
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("devexpress command registered via index.ts", typeof cmd?.handler === "function");
  }
  // Section: T2 — repo guard
  {
    const runner = mkRunner([]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]) });
    const ctx = mkCtx(["Build", "RX-XAF", "Lab"], tmpdir());
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("loud error outside the repo, zero commands ran", result.includes("not inside the Reactive.XAF repo") && runner.calls.length === 0, result);
  }
  // Section: T3 — Lab happy path with DX update, build in a pane
  {
    const repo = mkRepo(DX_PINS);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.300.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_OFF, stderr: "" } },
      { match: "Start-VM -Name C11", result: okResult() },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M Directory.Packages.props\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult("Queued build 123") },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.2", "26.1.3", "26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Update", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const after = propsText(repo);
    check("result published", result.includes("published"), result);
    check("update prompt shown", ctx._prompts.some((p) => p.includes("update all DevExpress")), ctx._prompts.join(" | "));
    check("all DX pins rewritten, non-DX untouched", after.includes('DevExpress.ExpressApp" Version="26.1.4"') && after.includes('DevExpress.Xpo" Version="26.1.4"') && after.includes('Xpand.Collections" Version="1.0.4"'), after);
    check("build pane opened", pane.opened.length === 1, JSON.stringify(pane.opened));
    check("brx sent to the pane", pane.sent.length === 1 && pane.sent[0].startsWith("brx;"), JSON.stringify(pane.sent));
    check("milestones notified", ctx._notifies.some((n) => n.includes("Build started — pane")) && ctx._notifies.some((n) => n.includes("Checking Hyper-V agents")) && ctx._notifies.some((n) => n.includes("Committing build state")) && ctx._notifies.some((n) => n.includes("Publishing via prx")), ctx._notifies.join(" | "));
    check("close ask conversational, no modal, pane kept", ctx._notifies.some((n) => n.includes("Close build pane")) && !ctx._prompts.some((p) => p.includes("Close build pane")) && pane.closed.length === 0, ctx._notifies.join(" | ") + " " + JSON.stringify(pane.closed));
    check("commit message carries DX", runner.calls.some((c) => c.startsWith('git commit -m "Update DX to 26.1.4"')), runner.calls.join(" | "));
    check("build.ps1 version bumped with DX", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("prx ran last", runner.calls[runner.calls.length - 1] === "prx", runner.calls.join(" | "));
    check("success: no failure delivery", pi._userMessages.length === 0, JSON.stringify(pi._userMessages));
  }
  // Section: T4 — DX already latest
  {
    const repo = mkRepo(DX_PINS);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.400.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("no update prompt, props untouched", !ctx._prompts.some((p) => p.includes("update all DevExpress")) && propsText(repo).includes('Version="26.1.3"') && !propsText(repo).includes("26.1.4"), "props changed");
    check("build.ps1 untouched when DX already latest", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), "build.ps1 changed");
    check("build ran in pane", pane.sent.length === 1, JSON.stringify(pane.sent));
    check("published", result.includes("published"), result);
  }
  // Section: T5 — mixed pins left untouched
  {
    const repo = mkRepo([
      ["DevExpress.ExpressApp", "26.1.3"],
      ["DevExpress.Xpo", "26.1.2"],
    ]);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const after = propsText(repo);
    check("mixed versions surfaced, file untouched", result.includes("mixed") && after.includes('Version="26.1.2"'), result + " | " + after);
    check("build ran in pane", pane.sent.length === 1, JSON.stringify(pane.sent));
    check("published", result.includes("published"), result);
  }
  // Section: T6 — build failure with warnings (pane kept)
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams({ exitCode: 1, capture: "warning CS0219: unused variable" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("FAILED surfaced", result.includes("Build FAILED (exit 1)"), result);
    check("captured pane tail shown", result.includes("warning CS0219"), result);
    check("pane KEPT, no close ask on failure", pane.closed.length === 0 && !ctx._notifies.some((n) => n.includes("Close build pane")), JSON.stringify(pane.closed) + " | " + ctx._notifies.join(" | "));
    check("no publish commands", runner.calls.length === 0, runner.calls.join(" | "));
    check("failure delivery fired", pi._userMessages.length === 1 && pi._userMessages[0].opts?.deliverAs === "steer" && pi._userMessages[0].content.includes("Build FAILED"), JSON.stringify(pi._userMessages));
  }
  // Section: T7 — Release flow (DX update skipped)
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx -Release", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Release", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("brx -Release sent to pane", pane.sent.length === 1 && pane.sent[0].startsWith("brx -Release;"), JSON.stringify(pane.sent));
    check("T7: prx -Release ran (def 23 on master), published", runner.calls.includes("prx -Release") && !runner.calls.some((c) => c.startsWith("$cred") && c.includes("Push-GitSSH")) && result.includes("published"), runner.calls.join(" | ") + " " + result);
  }
  // Section: T8 — abort at the DX prompt
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Abort"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("abort surfaced", result.includes("aborted at the DX update prompt"), result);
    check("nothing ran, user abort: no delivery", runner.calls.length === 0 && pane.opened.length === 0 && pi._userMessages.length === 0, JSON.stringify({ calls: runner.calls, opened: pane.opened, msgs: pi._userMessages }));
  }
  // Section: T9-T10 — green publish: VMs running / nothing to commit
  {
    const repo = mkRepo(DX_PINS);
    let runner = mkRunner(GREEN_PUBLISH);
    let pane = mkPaneSeams();
    let pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    let ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    let result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T9: no Start-VM", !runner.calls.some((c) => c.startsWith("Start-VM")), runner.calls.join(" | "));
    check("T9: already-running noted", result.includes("already running"), result);
    check("T9: published", result.includes("published"), result);
    runner = mkRunner(GREEN_PUBLISH);
    pane = mkPaneSeams();
    pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T10: no commit prompt, prx still ran, published", !ctx._prompts.some((p) => p.includes("Commit with message")) && !runner.calls.includes("git add -A") && runner.calls.includes("prx") && result.includes("published"), ctx._prompts.join(" | ") + " | " + result);
  }
  // Section: T11 — pane open fails → in-process fallback
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "brx", result: okResult("Build succeeded") },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pane = mkPaneSeams({ open: null });
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("fallback note notified", ctx._notifies.some((n) => n.includes("building in-process")), ctx._notifies.join(" | "));
    check("in-process brx ran", runner.calls.includes("brx"), runner.calls.join(" | "));
    check("no pane sent, published", pane.sent.length === 0 && result.includes("published"), JSON.stringify(pane.sent) + " " + result);
  }
  // Section: T12 — /devexpress → Close build pane
  {
    (globalThis as any)[Symbol.for("reactive-xaf-build.build-pane")] = "paneX";
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), repoRoot: repo, ...pane });
    const ctx = mkCtx(["Close build pane"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("close result + notified", result.includes("Build pane closed") && ctx._notifies.some((n) => n.includes("closed")), result + " | " + ctx._notifies.join(" | "));
    check("pane closed", pane.closed.length === 1 && pane.closed[0] === "paneX", JSON.stringify(pane.closed));
    check("no build ran", pane.sent.length === 0 && runner.calls.length === 0, JSON.stringify({ sent: pane.sent, calls: runner.calls }));
    check("pane state cleared", (globalThis as any)[Symbol.for("reactive-xaf-build.build-pane")] === undefined);
  }
  // Section: T13 — a Starting VM is not Start-VM'd; the flow waits for it
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: "C11=Starting\nC12=Running\nC13=Running\nC14=Running\n", stderr: "" } },
      ...GREEN_PUBLISH,
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("no Start-VM for the booting VM, waited then published", !runner.calls.some((c) => c.startsWith("Start-VM")) && result.includes("already booting") && result.includes("published"), runner.calls.join(" | ") + " | " + result);
  }
  // Section: T14-T16 — publish starts the background watcher and returns immediately
  {
    let t = mkMonitor();
    let ctx = mkCtx([...MENU, "Lab", "Publish"], t.repo);
    let result = await t.pi._cmds.get("devexpress").handler([], ctx);
    check("T14: publish returns immediately, monitoring in background", result.includes("monitoring in background") && result.includes("published"), result);
    check("T14: watcher started once", t.starts.length === 1, JSON.stringify(t.starts));
    check("T14: no failure steer from the flow (the watcher steers at the end)", t.pi._userMessages.length === 0, JSON.stringify(t.pi._userMessages));
    t = mkMonitor();
    ctx = mkCtx([...MENU, "Lab", "Publish"], t.repo);
    result = await t.pi._cmds.get("devexpress").handler([], ctx);
    check("T15: second publish also starts the watcher and publishes", t.starts.length === 1 && result.includes("published"), result);
    const t2 = { repo: mkRepo(DX_PINS), pi: mkPi() };
    registerBuildCommand(t2.pi, { run: mkRunner([{ match: "$cred = @{ Project*", result: { code: 0, stdout: "STATUS=35735;completed;failed;Artifact TestAssemblies was not found for build 35735", stderr: "" } }]).run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: t2.repo, ...mkPaneSeams() });
    const r2 = await t2.pi._cmds.get("devexpress").handler(["status"], mkCtx([], t2.repo));
    check("T16: status shows id + reason + link", r2.includes("35735") && r2.includes("Artifact TestAssemblies") && r2.includes("definitionId=23"), r2);
  }
  // Section: T18-T19 — menu picks run in the invoking window (no delegation)
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: "$cred = @{ Project*", result: okResult("STATUS=35735;completed;succeeded;") },
      ...GREEN_PUBLISH,
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: repo, pollMs: 1, ...pane });
    let ctx = mkCtx(["Last build status"], repo);
    let result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T18: status pick runs in this window (no delegation)", result.includes("35735") && result.includes("succeeded"), result);
    ctx = mkCtx([...MENU, "Lab", "Publish"], repo);
    result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T19: Lab pick runs the full flow in this window (pane opened, published)", result.includes("published") && pane.opened.length === 1 && pane.sent.length === 1 && pane.sent[0].startsWith("brx;"), result + " | " + JSON.stringify(pane.opened) + " | " + JSON.stringify(pane.sent));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
