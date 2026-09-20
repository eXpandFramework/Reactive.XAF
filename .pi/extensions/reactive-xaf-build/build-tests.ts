/**
 * reactive-xaf-build/build-tests — behavior contract for the /devexpress workflow.
 * Mock-pi harness with injected seams (fake command runner, feed fetcher, pane
 * seams, fixture props) — the real nuget.org, pwsh, psmux, VMs and git are
 *  never touched. T1-T13 build/commit/publish/failure/abort/pane flows;
 *  T14-T17 AzDO monitor + status; T18-T19 menu delegation; T20 fail-reason
 *  extraction (wrapper noise filtered, real error delivered); T31-T47 the VM
 *  contract (a failed probe stops the publish and never the commit, a build
 *  pre-warms the agents); T48-T53 the commit summary and the pre-push check;
 *  T54-T56 the run's build env (the profile owns it, the template adds none,
 *  and the no-pane fallback is handed it too).
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
function mkRunner(script: Array<{ match: string; result: any }>): { run: (cmd: string, opts?: any) => Promise<any>; calls: string[]; runOpts: any[] } {
  const calls: string[] = [];
  const runOpts: any[] = [];
  let i = 0;
  return {
    run: async (cmd: string, opts?: any) => {
      calls.push(cmd);
      runOpts.push(opts);
      const entry = script[i];
      i++;
      if (entry && (cmd === entry.match || (entry.match.includes("*") && cmd.startsWith(entry.match.replace("*", ""))))) {
        return entry.result;
      }
      return { code: 0, stdout: "", stderr: "" };
    },
    calls,
    runOpts,
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
/** The env the run was handed, parsed out of its script into pairs. */
function envOf(scriptPath: string): Record<string, string> {
  const env: Record<string, string> = {};
  for (const line of readFileSync(scriptPath, "utf-8").split("\n")) {
    const m = /^\$env:([A-Za-z_][A-Za-z0-9_]*)\s*=\s*'(.*)'\s*$/.exec(line);
    if (m) env[m[1]] = m[2].replace(/''/g, "'");
  }
  return env;
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
/** The fixture repo. `marker` is the directory the picked profile's `detect`
 *  looks for: the RX path by default, the eXpand path for the push cases. */
function mkRepo(pins: Array<[string, string]>, marker = join("src", "Extensions")): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-build-"));
  mkdirSync(join(root, marker), { recursive: true });
  const lines = pins.map(([id, v]) => `    <PackageVersion Include="${id}" Version="${v}" />`);
  const props = "<Project>\n  <ItemGroup>\n" + lines.join("\n") + "\n  </ItemGroup>\n</Project>\n";
  writeFileSync(join(root, "Directory.Packages.props"), props);
  return root;
}
function propsText(root: string): string {
  return readFileSync(join(root, "Directory.Packages.props"), "utf-8");
}
/** The probe's real shape: pwsh writes CRLF and terminates the last line too.
 *  A bare "\n" split left the \r on every line and read the answer as no
 *  agents at all (2026-09-19 incident) — these fixtures are CRLF so the suite
 *  fails with it. VM_LF keeps the other shape a seam may hand back. */
const VM_OFF = "C11=Off\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n";
const VM_SAVED = "C11=Saved\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n";
const VM_STARTING = "C11=Starting\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n";
const VM_RUN = "C11=Running\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n";
const VM_LF = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
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
/** The green publish fixture. Two probes: a build probes once at build start
 *  and the gate probes again, so every build-path flow consumes both. */
const GREEN_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
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
    const scriptPath = join(dirname(markerOf(pane.sent[0])), "run.ps1");
    check("T3: the pane got the supervisor script, not a bare command", pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(pane.sent[0]) && readFileSync(scriptPath, "utf-8").includes("brx"), JSON.stringify(pane.sent));
    check("T54: the run is handed the profile's build env", envOf(scriptPath).MSBuildWarningsAsMessages === "MSB3026", JSON.stringify(envOf(scriptPath)));
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
  // Section: T55 — the run template adds no policy of its own
  {
    const paths = runPaths("t55-no-env");
    writeRunScript(paths, "brx", {});
    check("T55: a run handed no env carries no assignment", Object.keys(envOf(paths.script)).length === 0, readFileSync(paths.script, "utf-8"));
    let threw = "";
    try {
      writeRunScript(runPaths("t55-bad-env"), "brx", { "BAD NAME": "1" });
    } catch (err) {
      threw = err instanceof Error ? err.message : String(err);
    }
    check("T55: a malformed env name throws at write time", threw.includes("invalid env name"), threw);
  }
  // Section: T4 — DX already latest
  {
    const repo = mkRepo(DX_PINS);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.300.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
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
    const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } }]);
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
    check("T6: no publish commands", !runner.calls.some((c) => c.startsWith("git") || c.includes("prx")), runner.calls.join(" | "));
    check("T6: one warning steer, forces a turn", warnings().length === 1 && steers.some((s) => s.opts?.severity === "warning" && s.opts?.triggerTurn === true), JSON.stringify(steers));
  }
  // Section: T7 — Release flow (DX update skipped)
  {
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
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
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
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
    const brxOpts = runner.runOpts[runner.calls.indexOf("brx")] ?? {};
    check("T56: the in-process run is handed the profile's build env", brxOpts.env?.MSBuildWarningsAsMessages === "MSB3026", JSON.stringify(runner.runOpts));
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
    // The order is the flow's: the probe is issued, the commit's own status read
    // follows it before the probe's continuation starts the wait, and the poll
    // lands last (the wait sleeps pollMs, the commit does not sleep at all).
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_STARTING, stderr: "" } },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_STARTING, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "prx", result: okResult() },
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
    registerBuildCommand(pi, { run: mkRunner([{ match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } }]).run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
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
    await waitFor(() => warnings().some((w) => w.includes("build pane is gone")));
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
    await waitFor(() => warnings().some((w) => w.includes("Build FAILED")));
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
    await waitFor(() => pi._userMessages.some((m: any) => m.content.includes("Build FAILED")));
    check("T28: the runtime message API carries the failure", pi._userMessages.some((m: any) => m.opts?.deliverAs === "steer" && m.content.includes("Build FAILED")), JSON.stringify(pi._userMessages));
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
    await waitFor(() => warnings().some((w) => w.includes("no readable code")));
    check("T30: reported as a failure naming the raw marker", warnings().some((w) => w.includes("no readable code")), JSON.stringify(warnings()));
  }
  // Section: T31 — an unreadable VM probe stops the publish, never the commit
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish", "RX-XAF", "Lab", "Commit"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T31: the failed probe is retried once, then refused", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 2, runner.calls.join(" | "));
    check("T31: the commit ran without the gate", runner.calls.includes("git add -A") && runner.calls.some((c) => c.startsWith('git commit -m "Publish (1 files)"')), runner.calls.join(" | "));
    check("T31: no queue from an unreadable VM layer", !runner.calls.some((c) => c.includes("prx")), runner.calls.join(" | "));
    check("T31: exit code and stderr reach the warning", warnings().some((w) => w.includes("Get-VM failed (exit 1)") && w.includes("Access is denied")), JSON.stringify(warnings()));
    check("T31: the report commits first, then names the VM error", result.indexOf("committed: Publish") >= 0 && result.indexOf("committed: Publish") < result.indexOf("Get-VM failed (exit 1)"), result);
    check("T31: the summary says the publish stopped", result.includes("publish stopped"), result);
  }
  // Section: T32 — a probe that reported nothing is loud, never "already running"
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("") }]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    const msg = warnings().join("\n");
    check("T32: every unseen agent named", msg.includes("did not report C11, C12, C13, C14"), msg);
    check("T32: the silent read is named as silent", msg.includes("no output on either stream"), msg);
    check("T32: no already-running claim", !msg.includes("already running"), msg);
    check("T32: no commit, no queue", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
  }
  // Section: T33 — a partial probe names exactly the agent it missed
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("C12=Running\r\nC13=Running\r\nC14=Running\r\n") }]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    const msg = warnings().join("\n");
    check("T33: names C11", msg.includes("did not report C11"), msg);
    check("T33: the agents it did see are not blamed", !msg.includes("C12") && !msg.includes("C13"), msg);
  }
  // Section: T34 — a state nothing can start fails the gate with the state named
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("C11=Paused\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n") }]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish", "RX-XAF", "Lab"], repo);
    await pi._cmds.get("devexpress").handler([], ctx);
    check("T34: the state is named, the publish stopped", warnings().some((w) => w.includes("C11 is Paused") && w.includes("publish stopped")), JSON.stringify(warnings()));
    check("T34: no blind Start-VM, no already-running claim", !runner.calls.some((c) => c.startsWith("Start-VM")) && !ctx._notifies.some((n) => n.includes("already running")), runner.calls.join(" | "));
  }
  // Section: T35 — a Saved agent is startable and gets started
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_SAVED) },
      { match: "git status --short", result: okResult("") },
      { match: "Start-VM -Name C11", result: okResult() },
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    check("T35: the Saved agent was started", runner.calls.includes("Start-VM -Name C11"), runner.calls.join(" | "));
    check("T35: published once the queue confirm came", result.includes("published") && runner.calls.includes("prx") && warnings().length === 0, result + " | " + runner.calls.join(" | ") + " | " + JSON.stringify(steers));
  }
  // Section: T36 — a build start pre-warms the agents and does not wait
  {
    stopBuildRun();
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_OFF) },
      { match: "Start-VM -Name C11", result: okResult() },
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
    const ctx = mkCtx([...MENU, "Lab", "Skip", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T36: the agents are started before the pane takes the build", runner.calls[0].startsWith("Get-VM") && runner.calls[1].startsWith("Start-VM -Name C11") && pane.opened.length === 1, runner.calls.join(" | "));
    check("T36: the pre-warm does not wait", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 1, runner.calls.join(" | "));
    check("T36: the started notice says they are booting", result.includes("booting during the build: C11") && result.includes("Build started in pane"), result);
    finishRun(pane, 0);
    await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    check("T36: the gate finds them running and publishes", ctx._notifies.some((n) => n.includes("published")) && runner.calls.filter((c) => c.startsWith("Start-VM")).length === 1, runner.calls.join(" | "));
  }
  // Section: T37 — a broken pre-warm never stops the build
  {
    stopBuildRun();
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
      { match: "Start-VM -Name C11,C12,C13,C14", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    check("T37: the build started with the probe broken", result.includes("Build started in pane") && result.includes("Get-VM failed (exit 1)"), result);
    check("T37: all four agents were blind-started anyway", runner.calls.includes("Start-VM -Name C11,C12,C13,C14"), runner.calls.join(" | "));
    check("T37: the broken probe steers a warning that spends no model turn", steers.some((s) => s.opts?.severity === "warning" && s.opts?.triggerTurn !== true && s.content.includes("Get-VM failed (exit 1)")), JSON.stringify(steers));
    stopBuildRun();
    const boom = { run: async () => { throw new Error("pwsh missing"); } };
    const pi2 = mkPi();
    registerBuildCommand(pi2, { run: boom.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const r2 = await pi2._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    check("T37: a probe seam that throws is a note too", r2.includes("Build started") && r2.includes("Get-VM did not run: pwsh missing"), r2);
    stopBuildRun();
  }
  // Section: T38 — a failed Start-VM in the pre-warm is a note
  {
    stopBuildRun();
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_OFF) },
      { match: "Start-VM -Name C11", result: { code: 1, stdout: "", stderr: "Start-VM: not enough memory" } },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx([...MENU, "Lab", "Skip"], repo));
    check("T38: the build starts, the failed start is a note", result.includes("Build started in pane") && result.includes("Start-VM failed: Start-VM: not enough memory"), result);
    check("T38: the failed start steers a warning that spends no model turn", steers.some((s) => s.opts?.severity === "warning" && s.opts?.triggerTurn !== true && s.content.includes("not enough memory")), JSON.stringify(steers));
    check("T38: no publish attempt", !runner.calls.some((c) => c.startsWith("git")), runner.calls.join(" | "));
    stopBuildRun();
  }
  // Section: T39 — the publish-only flow probes once and never pre-warms
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: okResult("") },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    check("T39: one probe, then the queue", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 1 && runner.calls.includes("prx"), runner.calls.join(" | "));
    check("T39: published", result.includes("published"), result);
  }
  // Section: T40 — an agent that never boots fails the publish at the wait timeout
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const stuck = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return okResult(VM_OFF);
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: stuck.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    const msg = warnings().join("\n");
    check("T40: the wait ends loudly", msg.includes("did not reach Running within 3 minutes"), msg || "(no warning)");
    check("T40: still nothing committed or queued", !calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), calls.join(" | "));
  }
  // Section: T41 — a probe killed mid-list refuses instead of acting on part of a plan
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const KILLED = "C11=Off\r\nC12=Running\r\n";
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: KILLED, stderr: "" } },
      { match: "git status --short", result: okResult("") },
      { match: VM_CHECK_PREFIX, result: { code: 1, stdout: KILLED, stderr: "" } },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    const msg = warnings().join("\n");
    check("T41: the kill is reported, not the truncated list", msg.includes("Get-VM failed (exit 1)"), msg || "(no warning)");
    check("T41: no commit and no queue from a partial probe", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
  }
  // Section: T42 — a probe that answers the retry publishes, and asks for a profile-free pwsh
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const opts: any[] = [];
    const flaky = {
      run: async (cmd: string, o?: any) => {
        calls.push(cmd);
        opts.push(o);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) {
          return calls.filter((c) => c.startsWith("Get-VM")).length === 1
            ? { code: 1, stdout: "", stderr: "Get-VM: killed" }
            : okResult(VM_RUN);
        }
        if (cmd === "git status --short") return okResult("");
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: flaky.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    const probeCalls = calls.map((cmd, i) => ({ cmd, o: opts[i] })).filter((x) => x.cmd.startsWith("Get-VM"));
    check("T42: the retry answered and the publish went through", result.includes("published") && probeCalls.length === 2, result + " | " + calls.join(" | "));
    check("T42: the probe asked for a profile-free, prompt-free pwsh", probeCalls.every((x) => x.o?.noProfile === true), JSON.stringify(probeCalls.map((x) => x.o)));
    check("T42: one retry was enough, no warning", warnings().length === 0, JSON.stringify(warnings()));
  }
  // Section: T43 — an exit-0 read that listed nobody is retried and carries the probe's own words
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const silent = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return { code: 0, stdout: "", stderr: "Get-VM: The service cannot be started." };
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: silent.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    const msg = warnings().join("\n");
    check("T43: the silent read is probed twice before it refuses", calls.filter((c) => c.startsWith("Get-VM")).length === 2, calls.join(" | "));
    check("T43: the refusal names the agents and quotes the probe", msg.includes("did not report C11, C12, C13, C14") && msg.includes("The service cannot be started"), msg || "(no warning)");
    check("T43: nothing committed or queued from a silent read", !calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), calls.join(" | "));
    check("T43: the summary says the publish stopped", result.includes("publish stopped"), result);
  }
  // Section: T44 — an empty read that answers the retry publishes
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const onceEmpty = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) {
          return calls.filter((c) => c.startsWith("Get-VM")).length === 1 ? { code: 0, stdout: "", stderr: "" } : okResult(VM_RUN);
        }
        if (cmd === "git status --short") return okResult("");
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: onceEmpty.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    check("T44: the empty read answered on the retry and the publish went through", result.includes("published") && calls.filter((c) => c.startsWith("Get-VM")).length === 2, result + " | " + calls.join(" | "));
    check("T44: no warning out of a recovered read", warnings().length === 0, JSON.stringify(warnings()));
  }
  // Section: T45 — the probe's real CRLF shape reads every agent
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const crlfRead = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return okResult(VM_RUN);
        if (cmd === "git status --short") return okResult("");
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: crlfRead.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    check("T45: no agent started from a complete CRLF read", !calls.some((c) => c.startsWith("Start-VM")), calls.join(" | "));
    check("T45: one probe from a complete CRLF read, no warning", calls.filter((c) => c.startsWith("Get-VM")).length === 1 && warnings().length === 0, calls.join(" | ") + " | " + warnings().join(" | "));
    check("T45: the CRLF read published", result.includes("published"), result);
  }
  // Section: T46 — a CRLF read that names one agent Off starts exactly that agent
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const oneOff = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) {
          return okResult(calls.filter((c) => c.startsWith("Get-VM")).length === 1 ? VM_OFF : VM_RUN);
        }
        if (cmd === "git status --short") return okResult("");
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: oneOff.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    const started = calls.filter((c) => c.startsWith("Start-VM"));
    check("T46: a CRLF read that names C11 Off starts exactly C11", started.length === 1 && started[0] === "Start-VM -Name C11", calls.join(" | "));
    check("T46: the wait saw them running and the publish went through", result.includes("published") && warnings().length === 0, result + " | " + warnings().join(" | "));
  }
  // Section: T47 — a bare-LF read, the other seam shape, still parses
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const calls: string[] = [];
    const lfRead = {
      run: async (cmd: string) => {
        calls.push(cmd);
        if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return okResult(VM_LF);
        if (cmd === "git status --short") return okResult("");
        return okResult();
      },
    };
    const pi = mkPi();
    registerBuildCommand(pi, { run: lfRead.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Publish"], repo));
    check("T47: an LF read is still read as four agents and publishes", result.includes("published") && warnings().length === 0 && calls.filter((c) => c.startsWith("Get-VM")).length === 1, result + " | " + calls.join(" | "));
  }
  // Section: T48 — the agents boot while the commit prompt is open
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_SAVED) },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
      { match: "Start-VM -Name C11", result: okResult() },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "prx", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const atPrompt: string[][] = [];
    const ctx = mkCtx(["Publish", "RX-XAF", "Lab", "Commit", "Publish"], repo);
    const select = ctx.ui.select;
    ctx.ui.select = async (t: string, o: string[]) => {
      if (t.includes("Commit with message")) atPrompt.push([...runner.calls]);
      return select(t, o);
    };
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const seen = atPrompt[0] ?? [];
    check("T48: the probe and the start already ran at the prompt", seen.some((c) => c.startsWith("Start-VM")) && seen.some((c) => c.startsWith("Get-VM")), JSON.stringify(seen));
    check("T48: the commit itself had not started", !seen.some((c) => c.startsWith("git add")), JSON.stringify(seen));
    check("T48: the prompt carries counts and areas, never a path", ctx._prompts.some((p) => p.includes("1 file: 1 modified") && p.includes("areas: src") && !p.includes("src/x.cs")), ctx._prompts.join(" | "));
    check("T48: committed after the answer, then published", runner.calls.includes("git add -A") && runner.calls.includes("prx") && result.includes("published"), result + " | " + runner.calls.join(" | "));
  }
  // Section: T49 — a dirty tree at the push is a prompt, not a silent push
  {
    clearSteers();
    const repo = mkRepo(DX_PINS, join("Xpand", "Xpand.ExpressApp.Modules"));
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: okResult("") },
      { match: "git status --short", result: { code: 0, stdout: " M Xpand/Xpand.Utils/Properties/XpandAssemblyInfo.cs\n", stderr: "" } },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "git push lab HEAD:master", result: okResult() },
      { match: "px", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish", "eXpand", "Lab", "Publish", "Commit before push"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    const push = runner.calls.indexOf("git push lab HEAD:master");
    const add = runner.calls.indexOf("git add -A");
    check("T49: the dirty window is asked about, with a summary", ctx._prompts.some((p) => p.includes("Dirty working tree before pushing to lab") && p.includes("1 file: 1 modified") && p.includes("areas: Xpand/Xpand.Utils")), ctx._prompts.join(" | "));
    check("T49: Commit before push commits first, then pushes", add >= 0 && push > add, runner.calls.join(" | "));
    check("T49: the queue followed the push", runner.calls.includes("px") && result.includes("published"), result + " | " + runner.calls.join(" | "));
  }
  // Section: T50 — Push as is pushes the tree without committing it
  {
    clearSteers();
    const repo = mkRepo(DX_PINS, join("Xpand", "Xpand.ExpressApp.Modules"));
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: okResult("") },
      { match: "git status --short", result: { code: 0, stdout: " M build.ps1\n", stderr: "" } },
      { match: "git push lab HEAD:master", result: okResult() },
      { match: "px", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish", "eXpand", "Lab", "Publish", "Push as is"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T50: a top-level file reads as (root), never as a path", ctx._prompts.some((p) => p.includes("areas: (root) (1)") && !p.includes("build.ps1")), ctx._prompts.join(" | "));
    check("T50: no commit from a Push as is answer", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit")), runner.calls.join(" | "));
    check("T50: pushed as it is and queued", runner.calls.includes("git push lab HEAD:master") && runner.calls.includes("px") && result.includes("published"), result + " | " + runner.calls.join(" | "));
  }
  // Section: T51 — Abort at the push stops before the push and the queue
  {
    clearSteers();
    const repo = mkRepo(DX_PINS, join("Xpand", "Xpand.ExpressApp.Modules"));
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: okResult("") },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const ctx = mkCtx(["Publish", "eXpand", "Lab", "Publish", "Abort"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("T51: no push and no queue after the abort", !runner.calls.some((c) => c.startsWith("git push") || c === "px"), runner.calls.join(" | "));
    check("T51: the abort is named and the summary stopped", result.includes("push aborted") && result.includes("publish stopped"), result);
    check("T51: a user abort is not a warning", warnings().length === 0, JSON.stringify(steers));
  }
  // Section: T52 — a status read that failed refuses instead of reading as clean
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
      { match: "git status --short", result: { code: 1, stdout: "", stderr: "fatal: not a git repository" } },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab"], repo));
    check("T52: the failed read is named, never read as clean", result.includes("git status failed (exit 1)") && result.includes("not a git repository"), result);
    check("T52: nothing committed and nothing queued", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
    check("T52: the publish stopped", result.includes("publish stopped"), result);
  }
  // Section: T53 — aborting the commit drops the VM outcome with it
  {
    clearSteers();
    const repo = mkRepo(DX_PINS);
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: okResult(VM_SAVED) },
      { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
      { match: "Start-VM -Name C11", result: okResult() },
    ]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Publish", "RX-XAF", "Lab", "Abort"], repo));
    check("T53: the abort stops before the commit and the queue", !runner.calls.some((c) => c.startsWith("git add") || c.includes("prx")) && result.includes("commit aborted"), result + " | " + runner.calls.join(" | "));
    check("T53: the dropped VM outcome is not reported as waited for", !result.includes("starting Hyper-V agents"), result);
    check("T53: the summary stopped without a warning", result.includes("publish stopped") && warnings().length === 0, result + " | " + JSON.stringify(steers));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
