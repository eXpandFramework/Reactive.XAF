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
/** Local delay: the test harness owns its own clock. */
const sleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
import { runArgv } from "./pane.js";
import { isBuildRunActive, runPaths, stopBuildRun, writeRunScript } from "./run.js";

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
function mkPaneSeams(overrides: Partial<{
  open: string | null; capture: string; captureThrows: boolean; pid: number | null;
  probeAlive: boolean; cpu: (n: number) => number; runCadence: any;
}> = {}): any {
  const opened: string[] = [];
  const sent: string[] = [];
  const closed: string[] = [];
  const watcherStarts: number[] = [];
  let cpuCalls = 0;
  return {
    openBuildPane: async () => {
      if (overrides.open === null) return null;
      const id = overrides.open ?? "pane1";
      opened.push(id);
      return id;
    },
    runInPane: async (_pane: string, cmd: string) => { sent.push(cmd); },
    capturePane: async () => {
      if (overrides.captureThrows) throw new Error("pane gone");
      return overrides.capture ?? "";
    },
    closePane: async (pane: string) => { closed.push(pane); },
    probePane: async () => ({ alive: overrides.probeAlive ?? true, pid: overrides.pid ?? 4242 }),
    sampleCpu: async () => (overrides.cpu ? overrides.cpu(cpuCalls++) : 1),
    runCadence: overrides.runCadence ?? TEST_CADENCE,
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
/** The test cadence: fast ticks, and neither backstop fires on its own. */
const TEST_CADENCE = { signalMs: 10, probeMs: 10, cpuMs: 10, stallMs: 3_600_000, overrunMs: 3_600_000 };
const steers: Array<{ type: string; content: string; reason: string; deliverAs: string; opts: any }> = [];
function installSteer(): void {
  (globalThis as any).__steer = (_pi: any, customType: string, content: string, blockReason: string, deliverAs: string, options: any) => {
    steers.push({ type: customType, content, reason: blockReason, deliverAs, opts: options });
  };
}
function clearSteers(): void {
  steers.length = 0;
}
/** The warnings the agent got (severity warning), the ones that demand a turn. */
function warnings(): string[] {
  return steers.filter((s) => s.opts?.severity === "warning").map((s) => s.content);
}
/** The run dir the pane was handed, read back out of the typed line. */
function markerOf(sentLine: string): string {
  const script = /-File "([^"]+)"/.exec(sentLine)?.[1] ?? "";
  return join(dirname(script), "exit.code");
}
/** Finish a started run the way the supervisor would. */
function finishRun(pane: any, code: number): void {
  writeFileSync(markerOf(pane.sent[0]), String(code));
}
async function waitFor(cond: () => boolean, ms = 4000): Promise<boolean> {
  const deadline = Date.now() + ms;
  while (Date.now() < deadline) {
    if (cond()) return true;
    await sleep(20);
  }
  return cond();
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
function mkMonitor(): { pi: any; repo: string; starts: number[]; pane: any } {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const pi = mkPi();
  registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
  return { pi, repo, starts: pane.watcherStarts, pane };
}

(async () => {
  installSteer();
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
    check("T3: DX update prompt shown", ctx._prompts.some((p) => p.includes("update all DevExpress")), ctx._prompts.join(" | "));
    check("T3: all DX pins rewritten, non-DX untouched", after.includes('DevExpress.ExpressApp" Version="26.1.4"') && after.includes('DevExpress.Xpo" Version="26.1.4"') && after.includes('Xpand.Collections" Version="1.0.4"'), after);
    check("T3: build pane opened once", pane.opened.length === 1, JSON.stringify(pane.opened));
    check("T3: the pane got the supervisor script, not a bare command", pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(pane.sent[0]) && readFileSync(join(dirname(markerOf(pane.sent[0])), "run.ps1"), "utf-8").includes("brx"), JSON.stringify(pane.sent));
    check("T3: the command returned on a STARTED build, nothing published yet", result.includes("Build started in pane") && !runner.calls.includes("prx"), result + " | " + runner.calls.join(" | "));
    check("T3: build.ps1 version bumped with DX", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("T3: started notice rides along without forcing a turn", steers.some((s) => s.content.includes("Build started in pane") && s.opts?.triggerTurn !== true) && ctx._notifies.some((n) => n.includes("Build started in pane")), JSON.stringify(steers));
    finishRun(pane, 0);
    await waitFor(() => runner.calls.includes("prx"));
    check("T3: green marker published", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
    check("T3: milestones notified", ctx._notifies.some((n) => n.includes("Checking Hyper-V agents")) && ctx._notifies.some((n) => n.includes("Committing build state")) && ctx._notifies.some((n) => n.includes("Publishing via prx")), ctx._notifies.join(" | "));
    check("T3: commit message carries DX", runner.calls.some((c) => c.startsWith('git commit -m "Update DX to 26.1.4"')), runner.calls.join(" | "));
    check("T3: pane kept, close is conversational", pane.closed.length === 0 && !ctx._prompts.some((p) => p.includes("Close build pane")), JSON.stringify(pane.closed));
    check("T3: no failure warning on a green build", warnings().length === 0, JSON.stringify(steers));
  }
  // Section: T4 — DX already latest
  {
    const repo = mkRepo(DX_PINS);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.300.0\"\n");
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
    check("build.ps1 untouched when DX already latest", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.300.0"'), "build.ps1 changed");
    check("T4: build started in the pane, nothing published yet", pane.sent.length === 1 && result.includes("Build started in pane") && !runner.calls.includes("prx"), JSON.stringify(pane.sent) + " " + result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T4: the green marker published", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
  }
  // Section: T4b — DX already latest but build.ps1 stale: repaired once, no creep on rerun
  {
    const repo = mkRepo([
      ["DevExpress.ExpressApp", "26.1.4"],
      ["DevExpress.Xpo", "26.1.4"],
      ["DevExpress.Utils", "26.1.4"],
      ["Xpand.Collections", "1.0.4"],
    ]);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.301.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: { code: 0, stdout: " M build.ps1\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "prx", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T4b: stale build.ps1 repaired to DX base", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("T4b: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T4b: the green marker published", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
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
    check("T5: mixed versions surfaced, file untouched", result.includes("mixed") && after.includes('Version="26.1.2"'), result + " | " + after);
    check("T5: build started in the pane, nothing published yet", pane.sent.length === 1 && result.includes("Build started in pane") && !runner.calls.includes("prx"), JSON.stringify(pane.sent) + " " + result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T5: the green marker published", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
  }
  // Section: T6 — build failure with warnings (pane kept), reported by the watch
  {
    clearSteers();
    stopBuildRun();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams({ capture: "warning CS0219: unused variable" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T6: the command returns before the outcome is known", result.includes("Build started in pane") && warnings().length === 0, result);
    finishRun(pane, 1);
    await waitFor(() => warnings().length > 0);
    const msg = warnings().join("\n");
    check("T6: FAILED surfaced with the exit code", msg.includes("Build FAILED (exit 1)"), msg);
    check("T6: captured pane tail shown", msg.includes("warning CS0219"), msg);
    check("T6: pane KEPT, no close on failure", pane.closed.length === 0 && !ctx._notifies.some((n) => n.includes("Close build pane")), JSON.stringify(pane.closed) + " | " + ctx._notifies.join(" | "));
    check("T6: no publish commands", runner.calls.length === 0, runner.calls.join(" | "));
    check("T6: one warning steer, forces a turn", warnings().length === 1 && steers.some((s) => s.opts?.severity === "warning" && s.opts?.triggerTurn === true), JSON.stringify(steers));
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
    check("T7: the supervisor script runs brx -Release, the command returned started", pane.sent.length === 1 && readFileSync(join(dirname(markerOf(pane.sent[0])), "run.ps1"), "utf-8").includes("brx -Release") && result.includes("Build started in pane"), JSON.stringify(pane.sent) + " " + result);
    finishRun(pane, 0);
    await waitFor(() => runner.calls.includes("prx -Release"));
    check("T7: prx -Release ran (def 23 on master), published", runner.calls.includes("prx -Release") && !runner.calls.some((c) => c.startsWith("$cred") && c.includes("Push-GitSSH")) && ctx._notifies.some((n) => n.includes("published")), runner.calls.join(" | "));
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
    check("T9: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T9: no Start-VM", !runner.calls.some((c) => c.startsWith("Start-VM")), runner.calls.join(" | "));
    check("T9: already-running noted", ctx._notifies.some((n) => n.includes("already running")), ctx._notifies.join(" | "));
    check("T9: published", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
    runner = mkRunner(GREEN_PUBLISH);
    pane = mkPaneSeams();
    pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T10: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(pane, 0);
    await waitFor(() => runner.calls.includes("prx"));
    check("T10: no commit prompt, prx still ran, published", !ctx._prompts.some((p) => p.includes("Commit with message")) && !runner.calls.includes("git add -A") && runner.calls.includes("prx") && ctx._notifies.some((n) => n.includes("published")), ctx._prompts.join(" | ") + " | " + runner.calls.join(" | "));
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
    check("T11: fallback note notified", ctx._notifies.some((n) => n.includes("building in-process")), ctx._notifies.join(" | "));
    check("T11: in-process brx ran", runner.calls.includes("brx"), runner.calls.join(" | "));
    check("T11: no pane sent, the start message says in-process", pane.sent.length === 0 && result.includes("Build started in-process"), JSON.stringify(pane.sent) + " " + result);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T11: the in-process run still reports its outcome", ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
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
    check("T13: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T13: no Start-VM for the booting VM, waited then published", !runner.calls.some((c) => c.startsWith("Start-VM")) && ctx._notifies.some((n) => n.includes("already booting")) && ctx._notifies.some((n) => n.includes("published")), runner.calls.join(" | ") + " | " + ctx._notifies.join(" | "));
  }
  // Section: T14-T16 — publish starts the background watcher and returns immediately
  {
    let t = mkMonitor();
    let ctx = mkCtx([...MENU, "Lab", "Publish"], t.repo);
    clearSteers();
    let result = await t.pi._cmds.get("devexpress").handler([], ctx);
    check("T14: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(t.pane, 0);
    const t14done = await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T14: publish runs from the watch, monitoring in background", t14done && ctx._notifies.some((n) => n.includes("monitoring in background")), ctx._notifies.join(" | "));
    check("T14: watcher started once", t.starts.length === 1, JSON.stringify(t.starts));
    check("T14: no failure steer from the flow (the watcher steers at the end)", warnings().length === 0, JSON.stringify(steers));
    t = mkMonitor();
    ctx = mkCtx([...MENU, "Lab", "Publish"], t.repo);
    result = await t.pi._cmds.get("devexpress").handler([], ctx);
    check("T15: the command returned on a started build", result.includes("Build started in pane"), result);
    finishRun(t.pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T15: second publish also starts the watcher and publishes", t.starts.length === 1 && ctx._notifies.some((n) => n.includes("published")), ctx._notifies.join(" | "));
    const t2 = { repo: mkRepo(DX_PINS), pi: mkPi() };
    registerBuildCommand(t2.pi, { run: mkRunner([{ match: "$cred = @{ Project*", result: { code: 0, stdout: "STATUS=35735;completed;failed;Artifact TestAssemblies was not found for build 35735", stderr: "" } }]).run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: t2.repo, ...mkPaneSeams() });
    const r2 = await t2.pi._cmds.get("devexpress").handler([], mkCtx(["Last build status"], t2.repo));
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
    check("T19: Lab pick runs the flow here (pane opened, build started)", result.includes("Build started in pane") && pane.opened.length === 1 && pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(pane.sent[0]), result + " | " + JSON.stringify(pane.opened) + " | " + JSON.stringify(pane.sent));
    finishRun(pane, 0);
    await waitFor(() => runner.calls.includes("prx") || runner.calls.includes("prx -Release"));
    check("T19: the green build published from the watch", runner.calls.includes("prx") || runner.calls.includes("prx -Release"), runner.calls.join(" | "));
  }
  // Section: T21 — the command returns on a started build, the run is watched
  {
    stopBuildRun();
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Skip"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T21: returned with the build still running", isBuildRunActive() && !runner.calls.includes("prx"), result);
    check("T21: start notice rides along without a model turn", steers.some((s) => s.content.includes("Build started in pane") && s.opts?.triggerTurn !== true), JSON.stringify(steers));
    const second = await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    check("T21: a second build is refused, no second pane", second.includes("already running") && pane.opened.length === 1, second + " | " + JSON.stringify(pane.opened));
    stopBuildRun();
  }
  // Section: T22 — no output and no CPU progress reports once, and kills nothing
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ capture: "unchanged", cpu: () => 5, runCadence: { ...TEST_CADENCE, stallMs: 60 } });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    await sleep(300);
    check("T22: one stall warning", warnings().filter((w) => w.includes("looks stuck")).length === 1, JSON.stringify(warnings()));
    check("T22: nothing killed, the run is still watched", pane.closed.length === 0 && isBuildRunActive(), JSON.stringify(pane.closed));
    stopBuildRun();
  }
  // Section: T23 — a quiet build that keeps burning CPU is not a stall
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ capture: "unchanged", cpu: (n) => n * 0.5, runCadence: { ...TEST_CADENCE, stallMs: 60 } });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    await sleep(300);
    check("T23: CPU progress suppresses the stall entirely", warnings().length === 0, JSON.stringify(warnings()));
    stopBuildRun();
  }
  // Section: T24 — past the overrun window the run reports once, pane untouched
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ cpu: () => 5, runCadence: { ...TEST_CADENCE, overrunMs: 60 } });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    await sleep(300);
    check("T24: one overrun warning, pane never closed", warnings().filter((w) => w.includes("has been running for")).length === 1 && pane.closed.length === 0, JSON.stringify(warnings()));
    stopBuildRun();
  }
  // Section: T25 — a pane that dies before the marker is terminal, not silence
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([]);
    const pane = mkPaneSeams({ probeAlive: false, capture: "half a build" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    await waitFor(() => warnings().length > 0);
    check("T25: reported as a failure with no exit code", warnings().some((w) => w.includes("build pane is gone")), JSON.stringify(warnings()));
    check("T25: no publish from a dead build", !runner.calls.includes("prx"), runner.calls.join(" | "));
  }
  // Section: T26 — abort closes the pane, stops the watch, reports no failure
  {
    stopBuildRun();
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    clearSteers();
    const aborted = await pi._cmds.get("devexpress").handler([], mkCtx(["Abort build (pane1, 1 min)"], repo));
    check("T26: abort closed the pane and stopped the watch", pane.closed.length === 1 && !isBuildRunActive(), aborted + " | " + JSON.stringify(pane.closed));
    check("T26: a deliberate stop reports no failure", warnings().length === 0, JSON.stringify(steers));
    const again = await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    check("T26: the next build starts normally", again.includes("Build started") && pane.opened.length === 2, again);
    stopBuildRun();
  }
  // Section: T27 — the failure tail stays bounded
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ capture: "x".repeat(20000) + "TAILEND" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    finishRun(pane, 1);
    await waitFor(() => warnings().length > 0);
    const msg = warnings().join("");
    check("T27: bounded tail that keeps its end", msg.length < 6000 && msg.includes("TAILEND"), `len=${msg.length}`);
  }
  // Section: T28 — without the shared sender the warning still lands
  {
    const saved = (globalThis as any).__steer;
    delete (globalThis as any).__steer;
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ capture: "boom" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    finishRun(pane, 1);
    await waitFor(() => pi._userMessages.length > 0);
    check("T28: the runtime message API carries the failure", pi._userMessages.length === 1 && pi._userMessages[0].opts?.deliverAs === "steer" && pi._userMessages[0].content.includes("Build FAILED"), JSON.stringify(pi._userMessages));
    (globalThis as any).__steer = saved;
  }
  // Section: T29 — the supervisor's exit code survives an exit inside the build
  {
    const failing = writeRunScript(runPaths(`test-fail-${Date.now()}`), "exit 3");
    await runArgv(["pwsh", "-NoLogo", "-File", failing.script], 60000);
    const got = readFileSync(failing.marker, "utf-8").trim();
    check("T29: build exit 3 still writes the code", got === "3", `marker=${got}`);
    const green = writeRunScript(runPaths(`test-ok-${Date.now()}`), "Write-Output ok");
    await runArgv(["pwsh", "-NoLogo", "-File", green.script], 60000);
    check("T29: a green build writes 0", readFileSync(green.marker, "utf-8").trim() === "0", "marker read");
  }
  // Section: T30 — a corrupt marker is a failure, never silence
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const pane = mkPaneSeams({ capture: "out" });
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkRunner([]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    writeFileSync(markerOf(pane.sent[0]), "not-a-code");
    await waitFor(() => warnings().length > 0);
    check("T30: reported as a failure naming the raw marker", warnings().some((w) => w.includes("no readable code")), JSON.stringify(warnings()));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
