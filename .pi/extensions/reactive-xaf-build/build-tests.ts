/**
 * reactive-xaf-build/build-tests — behavior contract for the /devexpress workflow.
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (its commit 93e1a4754 is where it comes
 * from): `./resolve.mjs` installs the `@pi/` name floor first, and the harness
 * plus the seam-owning modules are DYNAMIC imports after it, since a static one
 * is linked before the hook exists. One difference from that file: this suite
 * imports no boot proof and reads no ledger entry, because nothing here spawns.
 * The command is the one ./index.ts and ./menu.ts register, and only the
 * extension's OWN injectable ports are wired in (command runner, feed fetcher,
 * pane machinery, watcher starter, propsPath, repoRoot, pollMs): the real
 * nuget.org, pwsh, psmux, Hyper-V VMs and git are never touched.
 *
 * The harness answers ui.select with the FIRST option, so every case answers
 * the flow's prompts itself, keyed by a SUBSTRING of the prompt title, and
 * FAILS on a title it did not map or an answer that is not one of the offered
 * options. Without that, a Build case would silently take the Publish branch.
 * Assertions read the host capture: the toasts (host.notices), the steers the
 * agent got (host.messages with messageOptions index-aligned) and the prompts.
 * The run's own report is DELIVERED (report.ts notifies and steers it), so what
 * a case pins is what the user is shown, never a return value.
 *
 * T1-T13 build/commit/publish/failure/abort/pane flows; T14-T19 the AzDO
 * monitor and the status pick in the invoking window; T21-T30 the run watch;
 * T31-T47 the VM contract (a failed probe stops the publish and never the
 * commit, a build pre-warms the agents); T48-T53 the commit summary and the
 * pre-push check; T54-T56 the run's build env (the profile owns it, the
 * template adds none, and the no-pane fallback is handed it too).
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/build-tests.ts
 */
// test-timeout: 300000 — one runner build per case on pi's real runtime, plus two real pwsh runs
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
/** Local delay: the test harness owns its own clock. */
const sleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from "node:fs";
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

const handles: any[] = [];
const tempDirs: string[] = [];

/** The harness's build function, filled in by installRoute(): its types sit
 *  behind a bare `@pi/` specifier a plain-tsx process cannot resolve, and a
 *  static import would be linked BEFORE the resolver hook exists. */
let buildRealRunner: (opts: {
  activate: (pi: any) => void;
  cwd?: string;
  ui?: Record<string, unknown>;
}) => Promise<any>;

/** The route, carried by this file rather than shared in a helper (the
 *  per-suite copy the plan calls for): the resolver hook first, then the harness
 *  and the seam-owning modules BY NAME — dynamic imports only, since a static
 *  one is linked before the hook exists. The extension reads `__steer`
 *  (llm-utils) and `__writeFileSync` (tracked-write) off globalThis, so the real
 *  modules have to publish them; a stub would fabricate the observable a case
 *  asserts. Idempotent: the hook is a process-wide registration. */
async function installRoute(): Promise<void> {
  await import(new URL("./resolve.mjs", import.meta.url).href);
  buildRealRunner = (await import("@pi/pi-dev/real-runner.js")).buildRealRunner;
  await import("@pi/pi-dev/llm-utils.js"); // publishes __steer
  await import("@pi/pi-dev/tracked-write.js"); // publishes __writeFileSync
}

/** The toasts the flow showed the user, in order. */
function noticesOf(handle: any): string[] {
  return handle.host.notices.map((n: { message: string }) => n.message);
}

/** The toasts the flow showed AS WARNINGS: report.ts pairs one with a steer,
 *  while steerWatch steers without a toast. */
function warningToasts(handle: any): string[] {
  return handle.host.notices
    .filter((n: { type?: string }) => n.type === "warning")
    .map((n: { message: string }) => n.message);
}

/** The steers the agent got, read off pi's own capture. The shared sender
 *  lands in `host.messages` with the delivery options index-aligned; the
 *  fallback path (no shared sender) passes a plain string, hence the
 *  normalization. */
function steersOf(handle: any): Array<{ content: string; triggerTurn: boolean }> {
  return handle.host.messages.map((m: any, i: number) => ({
    content: String(m?.content ?? m ?? ""),
    triggerTurn: handle.host.messageOptions[i]?.triggerTurn === true,
  }));
}

function steersText(handle: any): string[] {
  return steersOf(handle).map((s) => s.content);
}

/** No warning anywhere: no warning toast, and no steer that forces a turn —
 *  report.ts's warn() is the only caller that asks for one. */
function noWarning(handle: any): boolean {
  return warningToasts(handle).length === 0 && !steersOf(handle).some((s) => s.triggerTurn);
}

/** The handler errors the runner REPORTED instead of throwing; an error is a
 *  no-op unless a case looks for it, so every case looks. */
function errorsOf(handle: any): string {
  return JSON.stringify(handle.host.errors);
}

/** A case's answer for one prompt: a literal option, or a resolver over the
 *  offered options for a title whose list is built at run time (the abort
 *  entry carries the pane and the elapsed minutes). */
type Answer = string | ((options: string[]) => string | undefined);

interface CaseUi {
  ui: Record<string, unknown>;
  asks: string[];
  unexpected: string[];
}

/** The case's own ui.select override, keyed by a SUBSTRING of the prompt title
 *  (the commit prompt spells the whole summary and the DX prompt both
 *  versions, so neither can be named exactly). A title the case did not map,
 *  or a mapped answer that is not one of the offered options, is RECORDED —
 *  the harness would otherwise answer it with the first option — and every
 *  case asserts the list is empty. Without that a Build case would silently
 *  take the Publish branch. The LONGEST matching key wins: the DX prompt's
 *  title spells "…update all DevExpress.* pins?" and would otherwise be
 *  answered by the top menu's own "DevExpress" key. A case that supplies this
 *  override REPLACES the harness's own select, so `host.prompts` stays empty
 *  for it and `sel.asks` is the record of what the flow asked. */
function uiFor(
  answers: Record<string, Answer>,
  observe?: (title: string, options: string[]) => void,
): CaseUi {
  const asks: string[] = [];
  const unexpected: string[] = [];
  return {
    ui: {
      select: async (title: string, options: string[]): Promise<string | undefined> => {
        asks.push(title);
        observe?.(title, options);
        const matching = Object.keys(answers).filter((k) => title.includes(k));
        const key = matching.sort((a, b) => b.length - a.length)[0];
        if (key === undefined) {
          unexpected.push(`unanswered prompt: ${title}`);
          return options?.[0];
        }
        const mapped = answers[key];
        const answer = typeof mapped === "function" ? mapped(options ?? []) : mapped;
        if (typeof answer !== "string" || (options && !options.includes(answer))) {
          unexpected.push(`"${String(answer)}" is not one of: ${options?.join(" | ")}`);
          return options?.[0];
        }
        return answer;
      },
    },
    asks,
    unexpected,
  };
}

/** What a case hands its own assertion halves: the fixture it built, the ports
 *  it scripted and the host it drove. A case is split this way to keep every
 *  function inside the file-size rule, never to hide work. */
interface Parts {
  repo: string;
  runner: any;
  pane: any;
  sel: CaseUi;
  handle: any;
}

/** Build the extension for one case: pi's real loader + ExtensionRunner, the
 *  real command, the case's own ports. `cwd` is what the repo guard reads. */
async function menuHandle(
  cwd: string, seams: Record<string, unknown>, ui: Record<string, unknown>,
): Promise<any> {
  await installRoute();
  const handle = await buildRealRunner({
    cwd,
    ui,
    activate: (pi: any) => registerBuildCommand(pi, seams),
  });
  handles.push(handle);
  return handle;
}

/** The runner reports a throwing handler instead of throwing, so an error is a
 *  no-op unless a case looks for it. Every case looks. */
function noErrors(handle: any, where: string): void {
  check(`${where}: no handler error`, handle.host.errors.length === 0, errorsOf(handle));
}

/** Every prompt the flow showed was answered by the case. */
function answered(sel: CaseUi, where: string): void {
  check(`${where}: every prompt the flow showed was answered by the case`, sel.unexpected.length === 0, sel.unexpected.join(" | "));
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
  tempDirs.push(root);
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
const DX_PINS: Array<[string, string]> = [
  ["DevExpress.ExpressApp", "26.1.3"],
  ["DevExpress.Xpo", "26.1.3"],
  ["DevExpress.Utils", "26.1.3"],
  ["Xpand.Collections", "1.0.4"],
];
/** The prompts the flow shows, keyed by a substring of their title: the commit
 *  prompt spells the whole summary and the DX prompt spells both versions, so
 *  neither can be named exactly. */
const DX_PROMPT = "update all DevExpress";
const COMMIT_PROMPT = "Commit with message";
const PUBLISH_PROMPT = "Publish: ";
const PUSH_PROMPT = "Dirty working tree before pushing to lab";
/** The top menu and the project pick: what every Build | Publish case starts
 *  with. A pick the flow does not offer is recorded, never taken blindly. */
const MENU_PICKS: Record<string, Answer> = { DevExpress: "Build", Project: "RX-XAF" };
const RX_LAB: Record<string, Answer> = { ...MENU_PICKS, "RX-XAF": "Lab" };
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
/** The seams a VM-gate case wires: the probe fixture, the props file and a fast
 *  poll, with no pane opened by the flow itself. */
function gateSeams(repo: string, runner: any): Record<string, unknown> {
  return { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...mkPaneSeams() };
}
/** The seams a running-build case wires: its own pane seams and props file, so
 *  the run's watch reads the fixture repo. */
function buildSeams(repo: string, runner: any, pane: any, fetches: string[]): Record<string, unknown> {
  return { run: runner.run, fetchFeed: mkFetch(fetches), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane };
}

// Section: T1 — /devexpress registration through the real index boot
async function caseT1(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("devexpress command registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "T1");
}

// Section: T2 — repo guard. The refusal is a warning toast now (`build.ts`
// `missingRepo`, delivered through `refuse`), so the case pins BOTH halves: the
// user is told, and outside the repo nothing runs, nothing opens and no run starts.
async function caseT2(): Promise<void> {
  const runner = mkRunner([]);
  const pane = mkPaneSeams();
  const sel = uiFor(RX_LAB);
  const handle = await menuHandle(tmpdir(), { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), ...pane }, sel.ui);
  await handle.runCommand("devexpress", "");
  check("loud error outside the repo, zero commands ran", runner.calls.length === 0 && pane.opened.length === 0 && !isBuildRunActive(), JSON.stringify({ calls: runner.calls, opened: pane.opened }));
  check("T2: the refusal is a warning toast naming the tree", warningToasts(handle).some((n) => n.includes("not inside the Reactive.XAF repo")), warningToasts(handle).join(" | "));
  answered(sel, "T2");
  noErrors(handle, "T2");
}

// Section: T3 — Lab happy path with DX update, build in a pane
async function caseT3(): Promise<void> {
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
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Update", [COMMIT_PROMPT]: "Commit", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.2", "26.1.3", "26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  const parts: Parts = { repo, runner, pane, sel, handle };
  checkT3Start(parts);
  checkT3RunState(parts);
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("prx"));
  checkT3Publish(parts);
}

/** T3's first half: the DX prompt, the rewritten file, the pane it opened. */
function checkT3Start(p: Parts): void {
  const after = propsText(p.repo);
  check("T3: DX update prompt shown", p.sel.asks.some((t) => t.includes(DX_PROMPT)), p.sel.asks.join(" | "));
  check("T3: all DX pins rewritten, non-DX untouched", after.includes('DevExpress.ExpressApp" Version="26.1.4"') && after.includes('DevExpress.Xpo" Version="26.1.4"') && after.includes('Xpand.Collections" Version="1.0.4"'), after);
  check("T3: build pane opened once", p.pane.opened.length === 1, JSON.stringify(p.pane.opened));
  const sent = p.pane.sent[0] ?? "";
  check("T3: the pane got the supervisor script, not a bare command", p.pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(sent) && readFileSync(join(dirname(markerOf(sent)), "run.ps1"), "utf-8").includes("brx"), JSON.stringify(p.pane.sent));
}

/** T3's middle: the run it started, and the env that run was handed. */
function checkT3RunState(p: Parts): void {
  const scriptPath = join(dirname(markerOf(p.pane.sent[0])), "run.ps1");
  check("T54: the run is handed the profile's build env", envOf(scriptPath).MSBuildWarningsAsMessages === "MSB3026", JSON.stringify(envOf(scriptPath)));
  check("T3: the command returned on a STARTED build, nothing published yet", noticesOf(p.handle).some((n) => n.includes("Build started in pane")) && !p.runner.calls.includes("prx"), noticesOf(p.handle).join(" | "));
  check("T3: build.ps1 version bumped with DX", readFileSync(join(p.repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(p.repo, "build.ps1"), "utf-8"));
  check("T3: started notice rides along without forcing a turn", steersOf(p.handle).some((s) => s.content.includes("Build started in pane") && s.triggerTurn === false), JSON.stringify(steersOf(p.handle)));
}

/** T3's second half: what the watch published once the marker went green. */
function checkT3Publish(p: Parts): void {
  check("T3: green marker published", noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | "));
  check("T3: milestones notified", noticesOf(p.handle).some((n) => n.includes("Checking Hyper-V agents")) && noticesOf(p.handle).some((n) => n.includes("Committing build state")) && noticesOf(p.handle).some((n) => n.includes("Publishing via prx")), noticesOf(p.handle).join(" | "));
  check("T3: commit message carries DX", p.runner.calls.some((c) => c.startsWith('git commit -m "Update DX to 26.1.4"')), p.runner.calls.join(" | "));
  check("T3: pane kept, close is conversational", p.pane.closed.length === 0 && !p.sel.asks.some((t) => t.includes("Close build pane")), JSON.stringify(p.pane.closed));
  check("T3: no failure warning on a green build", noWarning(p.handle), JSON.stringify({ toasts: warningToasts(p.handle), steers: steersText(p.handle) }));
  answered(p.sel, "T3");
  noErrors(p.handle, "T3");
}

// Section: T55 — the run template adds no policy of its own
async function caseT55(): Promise<void> {
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
async function caseT4(): Promise<void> {
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
  const sel = uiFor({ ...RX_LAB, [COMMIT_PROMPT]: "Commit", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.3"]), sel.ui);
  await handle.runCommand("devexpress", "");
  checkT4({ repo, runner, pane, sel, handle });
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  checkT4Tail({ repo, runner, pane, sel, handle });
}

/** T4's checks: no prompt because DX is already latest, and a started build. */
function checkT4(p: Parts): void {
  check("no update prompt, props untouched", !p.sel.asks.some((t) => t.includes(DX_PROMPT)) && propsText(p.repo).includes('Version="26.1.3"') && !propsText(p.repo).includes("26.1.4"), "props changed");
  check("build.ps1 untouched when DX already latest", readFileSync(join(p.repo, "build.ps1"), "utf-8").includes('-version "26.1.300.0"'), "build.ps1 changed");
  check("T4: build started in the pane, nothing published yet", p.pane.sent.length === 1 && noticesOf(p.handle).some((n) => n.includes("Build started in pane")) && !p.runner.calls.includes("prx"), JSON.stringify(p.pane.sent) + " " + noticesOf(p.handle).join(" | "));
}

/** T4's tail: the green marker, and the clean registration. */
function checkT4Tail(p: Parts): void {
  check("T4: the green marker published", noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | "));
  answered(p.sel, "T4");
  noErrors(p.handle, "T4");
}

// Section: T4b — DX already latest but build.ps1 stale: repaired once, no creep on rerun
async function caseT4b(): Promise<void> {
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
  const sel = uiFor({ ...RX_LAB, [COMMIT_PROMPT]: "Commit", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T4b: stale build.ps1 repaired to DX base", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
  check("T4b: the command returned on a started build", noticesOf(handle).some((n) => n.includes("Build started in pane")), noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  checkT4bTail({ repo, runner, pane, sel, handle });
}

/** T4b's tail: the repaired file published green, with no failure report. */
function checkT4bTail(p: Parts): void {
  check("T4b: the green marker published", noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | "));
  answered(p.sel, "T4b");
  noErrors(p.handle, "T4b");
}

// Section: T5 — mixed pins left untouched
async function caseT5(): Promise<void> {
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
  const sel = uiFor({ ...RX_LAB, [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  const after = propsText(repo);
  check("T5: mixed versions surfaced, file untouched", noticesOf(handle).some((n) => n.includes("mixed")) && after.includes('Version="26.1.2"'), noticesOf(handle).join(" | ") + " | " + after);
  check("T5: build started in the pane, nothing published yet", pane.sent.length === 1 && noticesOf(handle).some((n) => n.includes("Build started in pane")) && !runner.calls.includes("prx"), JSON.stringify(pane.sent) + " " + noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  check("T5: the green marker published", noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | "));
  answered(sel, "T5");
  noErrors(handle, "T5");
}

// Section: T6 — build failure with warnings (pane kept), reported by the watch
async function caseT6(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } }]);
  const pane = mkPaneSeams({ capture: "warning CS0219: unused variable" });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Update" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  checkT6Start({ repo, runner, pane, sel, handle });
  finishRun(pane, 1);
  await waitFor(() => steersText(handle).some((w) => w.includes("Build FAILED")));
  checkT6Failure({ repo, runner, pane, sel, handle });
}

/** T6's first half: a started build whose outcome is not known yet. */
function checkT6Start(p: Parts): void {
  check("T6: the command returns before the outcome is known", noticesOf(p.handle).some((n) => n.includes("Build started in pane")) && noWarning(p.handle), noticesOf(p.handle).join(" | "));
}

/** T6's second half: the failure report, the pane it kept and the one steer. */
function checkT6Failure(p: Parts): void {
  const msg = steersText(p.handle).join("\n");
  check("T6: FAILED surfaced with the exit code", msg.includes("Build FAILED (exit 1)"), msg);
  check("T6: captured pane tail shown", msg.includes("warning CS0219"), msg);
  check("T6: pane KEPT, no close on failure", p.pane.closed.length === 0 && !noticesOf(p.handle).some((n) => n.includes("Close build pane")), JSON.stringify(p.pane.closed) + " | " + noticesOf(p.handle).join(" | "));
  check("T6: no publish commands", !p.runner.calls.some((c) => c.startsWith("git") || c.includes("prx")), p.runner.calls.join(" | "));
  check("T6: one warning steer, forces a turn", warningToasts(p.handle).length === 1 && steersOf(p.handle).filter((s) => s.triggerTurn).length === 1, JSON.stringify({ toasts: warningToasts(p.handle), steers: steersOf(p.handle) }));
  answered(p.sel, "T6");
  noErrors(p.handle, "T6");
}

// Section: T7 — Release flow (DX update skipped)
async function caseT7(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: "git status --short", result: okResult("") },
    { match: "prx -Release", result: okResult() },
  ]);
  const pane = mkPaneSeams();
  const sel = uiFor({ DevExpress: "Build", Project: "RX-XAF", "RX-XAF": "Release", [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T7: the supervisor script runs brx -Release, the command returned started", pane.sent.length === 1 && readFileSync(join(dirname(markerOf(pane.sent[0])), "run.ps1"), "utf-8").includes("brx -Release") && noticesOf(handle).some((n) => n.includes("Build started in pane")), JSON.stringify(pane.sent) + " " + noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("prx -Release"));
  check("T7: prx -Release ran (def 23 on master), published", runner.calls.includes("prx -Release") && !runner.calls.some((c) => c.startsWith("$cred") && c.includes("Push-GitSSH")) && noticesOf(handle).some((n) => n.includes("published")), runner.calls.join(" | "));
  answered(sel, "T7");
  noErrors(handle, "T7");
}

// Section: T8 — abort at the DX prompt
async function caseT8(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([]);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Abort" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("abort surfaced", noticesOf(handle).some((n) => n.includes("aborted at the DX update prompt")), noticesOf(handle).join(" | "));
  check("nothing ran, user abort: no delivery", runner.calls.length === 0 && pane.opened.length === 0 && handle.host.messages.length === 0, JSON.stringify({ calls: runner.calls, opened: pane.opened, msgs: handle.host.messages }));
  answered(sel, "T8");
  noErrors(handle, "T8");
}

// Section: T9 — green publish: the VMs are already running
async function caseT9(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const runner = mkRunner(GREEN_PUBLISH);
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T9: the command returned on a started build", noticesOf(handle).some((n) => n.includes("Build started in pane")), noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  check("T9: no Start-VM", !runner.calls.some((c) => c.startsWith("Start-VM")), runner.calls.join(" | "));
  check("T9: already-running noted", noticesOf(handle).some((n) => n.includes("already running")), noticesOf(handle).join(" | "));
  check("T9: published", noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | "));
  answered(sel, "T9");
  noErrors(handle, "T9");
}

/** T10 continues in T9's repo: a clean tree publishes without a commit prompt. */
async function caseT10(p: { repo: string }): Promise<void> {
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const runner = mkRunner(GREEN_PUBLISH);
  const handle = await menuHandle(p.repo, buildSeams(p.repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T10: the command returned on a started build", noticesOf(handle).some((n) => n.includes("Build started in pane")), noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("prx"));
  check("T10: no commit prompt, prx still ran, published", !sel.asks.some((t) => t.includes(COMMIT_PROMPT)) && !runner.calls.includes("git add -A") && runner.calls.includes("prx") && noticesOf(handle).some((n) => n.includes("published")), sel.asks.join(" | ") + " | " + runner.calls.join(" | "));
  answered(sel, "T10");
  noErrors(handle, "T10");
}

// Section: T11 — pane open fails → in-process fallback
async function caseT11(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: "brx", result: okResult("Build succeeded") },
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: "git status --short", result: okResult("") },
    { match: "prx", result: okResult() },
  ]);
  const pane = mkPaneSeams({ open: null });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  checkT11({ repo, runner, pane, sel, handle });
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  checkT11Tail({ repo, runner, pane, sel, handle });
}

/** T11's checks: the fallback note, the in-process run and its env. */
function checkT11(p: Parts): void {
  check("T11: fallback note notified", noticesOf(p.handle).some((n) => n.includes("building in-process")), noticesOf(p.handle).join(" | "));
  check("T11: in-process brx ran", p.runner.calls.includes("brx"), p.runner.calls.join(" | "));
  const brxOpts = p.runner.runOpts[p.runner.calls.indexOf("brx")] ?? {};
  check("T56: the in-process run is handed the profile's build env", brxOpts.env?.MSBuildWarningsAsMessages === "MSB3026", JSON.stringify(p.runner.runOpts));
  check("T11: no pane sent, the start message says in-process", p.pane.sent.length === 0 && noticesOf(p.handle).some((n) => n.includes("Build started in-process")), JSON.stringify(p.pane.sent) + " " + noticesOf(p.handle).join(" | "));
}

/** T11's tail: the in-process run reports through the same watch. */
function checkT11Tail(p: Parts): void {
  check("T11: the in-process run still reports its outcome", noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | "));
  answered(p.sel, "T11");
  noErrors(p.handle, "T11");
}

// Section: T12 — /devexpress → Close build pane
async function caseT12(): Promise<void> {
  (globalThis as any)[Symbol.for("reactive-xaf-build.build-pane")] = "paneX";
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([]);
  const pane = mkPaneSeams();
  const sel = uiFor({ DevExpress: "Close build pane" });
  const handle = await menuHandle(repo, { run: runner.run, fetchFeed: mkFetch(["26.1.4"]), repoRoot: repo, ...pane }, sel.ui);
  await handle.runCommand("devexpress", "");
  check("close result + notified", noticesOf(handle).some((n) => n.includes("paneX") && n.includes("closed")), noticesOf(handle).join(" | "));
  check("pane closed", pane.closed.length === 1 && pane.closed[0] === "paneX", JSON.stringify(pane.closed));
  check("no build ran", pane.sent.length === 0 && runner.calls.length === 0, JSON.stringify({ sent: pane.sent, calls: runner.calls }));
  check("pane state cleared", (globalThis as any)[Symbol.for("reactive-xaf-build.build-pane")] === undefined);
  answered(sel, "T12");
  noErrors(handle, "T12");
}

// Section: T13 — a Starting VM is not Start-VM'd; the flow waits for it
async function caseT13(): Promise<void> {
  stopBuildRun();
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
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T13: the command returned on a started build", noticesOf(handle).some((n) => n.includes("Build started in pane")), noticesOf(handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  checkT13Tail({ repo, runner, pane, sel, handle });
}

/** T13's tail: the booting agent was waited for, never re-started. */
function checkT13Tail(p: Parts): void {
  check("T13: no Start-VM for the booting VM, waited then published", !p.runner.calls.some((c) => c.startsWith("Start-VM")) && noticesOf(p.handle).some((n) => n.includes("already booting")) && noticesOf(p.handle).some((n) => n.includes("published")), p.runner.calls.join(" | ") + " | " + noticesOf(p.handle).join(" | "));
  answered(p.sel, "T13");
  noErrors(p.handle, "T13");
}

// Section: T14-T16 — publish starts the background watcher and returns immediately
/** One publish monitor case: its own repo, pane seams and picks. */
async function monitorCase(): Promise<Parts> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.3"]), sel.ui);
  return { repo, runner, pane, sel, handle };
}

async function caseT14(): Promise<void> {
  stopBuildRun();
  const p = await monitorCase();
  const registered = await p.handle.runCommand("devexpress", "");
  check("T14: the command returned on a started build", noticesOf(p.handle).some((n) => n.includes("Build started in pane")), noticesOf(p.handle).join(" | "));
  check("T14: pi ran the registered command", registered === true, String(registered));
  finishRun(p.pane, 0);
  const done = await waitFor(() => noticesOf(p.handle).some((n) => n.includes("published")));
  check("T14: publish runs from the watch, monitoring in background", done && noticesOf(p.handle).some((n) => n.includes("monitoring in background")), noticesOf(p.handle).join(" | "));
  check("T14: watcher started once", p.pane.watcherStarts.length === 1, JSON.stringify(p.pane.watcherStarts));
  check("T14: no failure steer from the flow (the watcher steers at the end)", noWarning(p.handle), JSON.stringify(steersText(p.handle)));
  answered(p.sel, "T14");
  noErrors(p.handle, "T14");
}

// Section: T15 — a second publish starts its own watcher and publishes
async function caseT15(): Promise<void> {
  const p = await monitorCase();
  await p.handle.runCommand("devexpress", "");
  check("T15: the command returned on a started build", noticesOf(p.handle).some((n) => n.includes("Build started in pane")), noticesOf(p.handle).join(" | "));
  finishRun(p.pane, 0);
  await waitFor(() => noticesOf(p.handle).some((n) => n.includes("published")));
  check("T15: second publish also starts the watcher and publishes", p.pane.watcherStarts.length === 1 && noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | "));
  answered(p.sel, "T15");
  noErrors(p.handle, "T15");
}

// Section: T16 — the status pick parses the STATUS line
async function caseT16(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([{ match: "$cred = @{ Project*", result: { code: 0, stdout: "STATUS=35735;completed;failed;Artifact TestAssemblies was not found for build 35735", stderr: "" } }]);
  const sel = uiFor({ DevExpress: "Last build status" });
  const handle = await menuHandle(repo, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: repo, ...mkPaneSeams() }, sel.ui);
  await handle.runCommand("devexpress", "");
  const status = noticesOf(handle).join("\n");
  check("T16: status shows id + reason + link", status.includes("35735") && status.includes("Artifact TestAssemblies") && status.includes("definitionId=23"), noticesOf(handle).join(" | "));
  answered(sel, "T16");
  noErrors(handle, "T16");
}

// Section: T18-T19 — menu picks run in the invoking window (no delegation)
async function caseT18(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: "$cred = @{ Project*", result: okResult("STATUS=35735;completed;succeeded;") },
    ...GREEN_PUBLISH,
  ]);
  const pane = mkPaneSeams();
  const sel = uiFor({ DevExpress: "Last build status" });
  const handle = await menuHandle(repo, { run: runner.run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: repo, pollMs: 1, ...pane }, sel.ui);
  await handle.runCommand("devexpress", "");
  const status = noticesOf(handle).join("\n");
  check("T18: status pick runs in this window (no delegation)", status.includes("35735") && status.includes("succeeded"), noticesOf(handle).join(" | "));
  answered(sel, "T18");
  noErrors(handle, "T18");
}

/** T19 continues in T18's repo: the Lab pick opens the pane and publishes. */
async function caseT19(p: { repo: string; runner: any; pane: any }): Promise<void> {
  const sel = uiFor({ ...RX_LAB, [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(p.repo, { run: p.runner.run, fetchFeed: mkFetch(["26.1.3"]), repoRoot: p.repo, pollMs: 1, ...p.pane }, sel.ui);
  await handle.runCommand("devexpress", "");
  check("T19: Lab pick runs the flow here (pane opened, build started)", noticesOf(handle).some((n) => n.includes("Build started in pane")) && p.pane.opened.length === 1 && p.pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(p.pane.sent[0]), noticesOf(handle).join(" | ") + " | " + JSON.stringify(p.pane.opened) + " | " + JSON.stringify(p.pane.sent));
  finishRun(p.pane, 0);
  await waitFor(() => p.runner.calls.includes("prx") || p.runner.calls.includes("prx -Release"));
  check("T19: the green build published from the watch", p.runner.calls.includes("prx") || p.runner.calls.includes("prx -Release"), p.runner.calls.join(" | "));
  answered(sel, "T19");
  noErrors(handle, "T19");
}

// Section: T21 — the command returns on a started build, the run is watched
async function caseT21(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([]);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T21: returned with the build still running", isBuildRunActive() && !runner.calls.includes("prx"), noticesOf(handle).join(" | "));
  check("T21: start notice rides along without a model turn", steersOf(handle).some((s) => s.content.includes("Build started in pane") && s.triggerTurn === false), JSON.stringify(steersOf(handle)));
  // The refusal ("build is already running") is a warning toast now; what a
  // second build must not do is open a second pane or take the run over.
  const sel2 = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const twice = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel2.ui);
  await twice.runCommand("devexpress", "");
  check("T21: a second build is refused, no second pane", pane.opened.length === 1 && isBuildRunActive(), JSON.stringify(pane.opened));
  check("T21: the refusal is a warning toast naming the running build", warningToasts(twice).some((n) => n.includes("already running")), warningToasts(twice).join(" | "));
  stopBuildRun();
  answered(sel, "T21");
  answered(sel2, "T21-second");
  noErrors(handle, "T21");
  noErrors(twice, "T21-second");
}

// Section: T22 — no output and no CPU progress reports once, and kills nothing
async function caseT22(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ capture: "unchanged", cpu: () => 5, runCadence: { ...TEST_CADENCE, stallMs: 60 } });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  await sleep(300);
  check("T22: one stall warning", steersText(handle).filter((w) => w.includes("looks stuck")).length === 1, JSON.stringify(steersText(handle)));
  check("T22: nothing killed, the run is still watched", pane.closed.length === 0 && isBuildRunActive(), JSON.stringify(pane.closed));
  stopBuildRun();
  answered(sel, "T22");
  noErrors(handle, "T22");
}

// Section: T23 — a quiet build that keeps burning CPU is not a stall
async function caseT23(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ capture: "unchanged", cpu: (n) => n * 0.5, runCadence: { ...TEST_CADENCE, stallMs: 60 } });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } }]);
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  await sleep(300);
  check("T23: CPU progress suppresses the stall entirely", noWarning(handle), JSON.stringify(steersText(handle)));
  stopBuildRun();
  answered(sel, "T23");
  noErrors(handle, "T23");
}

// Section: T24 — past the overrun window the run reports once, pane untouched
async function caseT24(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ cpu: () => 5, runCadence: { ...TEST_CADENCE, overrunMs: 60 } });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  await sleep(300);
  check("T24: one overrun warning, pane never closed", steersText(handle).filter((w) => w.includes("has been running for")).length === 1 && pane.closed.length === 0, JSON.stringify(steersText(handle)));
  stopBuildRun();
  answered(sel, "T24");
  noErrors(handle, "T24");
}

// Section: T25 — a pane that dies before the marker is terminal, not silence
async function caseT25(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([]);
  const pane = mkPaneSeams({ probeAlive: false, capture: "half a build" });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  await waitFor(() => steersText(handle).some((w) => w.includes("build pane is gone")));
  check("T25: reported as a failure with no exit code", steersText(handle).some((w) => w.includes("build pane is gone")), JSON.stringify(steersText(handle)));
  check("T25: no publish from a dead build", !runner.calls.includes("prx"), runner.calls.join(" | "));
  stopBuildRun();
  answered(sel, "T25");
  noErrors(handle, "T25");
}

/** The abort run: the menu's own "Abort build (pane, N min)" entry, answered by
 *  matching the option the flow built at run time. */
async function abortRunIn(repo: string, pane: any): Promise<{ handle: any; sel: CaseUi }> {
  const sel = uiFor({ DevExpress: (options: string[]) => options.find((o) => o.startsWith("Abort build")) });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  return { handle, sel };
}

// Section: T26 — abort closes the pane, stops the watch, reports no failure
async function caseT26(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  const abort = await abortRunIn(repo, pane);
  check("T26: abort closed the pane and stopped the watch", pane.closed.length === 1 && !isBuildRunActive(), noticesOf(abort.handle).join(" | ") + " | " + JSON.stringify(pane.closed));
  check("T26: a deliberate stop reports no failure", noWarning(abort.handle), JSON.stringify(steersText(abort.handle)));
  const again = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const againRun = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), again.ui);
  await againRun.runCommand("devexpress", "");
  check("T26: the next build starts normally", noticesOf(againRun).some((n) => n.includes("Build started")) && pane.opened.length === 2, noticesOf(againRun).join(" | "));
  stopBuildRun();
  answered(sel, "T26");
  answered(abort.sel, "T26-abort");
  answered(again, "T26-again");
  noErrors(handle, "T26");
  noErrors(abort.handle, "T26-abort");
  noErrors(againRun, "T26-again");
}

// Section: T27 — the failure tail stays bounded
async function caseT27(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ capture: "x".repeat(20000) + "TAILEND" });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  finishRun(pane, 1);
  await waitFor(() => steersText(handle).some((w) => w.includes("Build FAILED")));
  const msg = steersText(handle).join("");
  check("T27: bounded tail that keeps its end", msg.length < 6000 && msg.includes("TAILEND"), `len=${msg.length}`);
  stopBuildRun();
  answered(sel, "T27");
  noErrors(handle, "T27");
}

// Section: T28 — without the shared sender the warning still lands
async function caseT28(): Promise<void> {
  // report.ts falls back to the runtime's message API when llm-utils is not
  // loaded. The real module publishes __steer at import, so the fallback is
  // reached by taking the seam away for this case only and putting it back
  // straight after: what the case asserts is the runtime message pi got.
  stopBuildRun();
  const saved = (globalThis as any).__steer;
  delete (globalThis as any).__steer;
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ capture: "boom" });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  finishRun(pane, 1);
  const textOf = (m: any): string => String(m?.content ?? m ?? "");
  await waitFor(() => handle.host.messages.some((m: any) => textOf(m).includes("Build FAILED")));
  const delivered = handle.host.messages.filter((m: any) => textOf(m).includes("Build FAILED"));
  const steered = handle.host.messageOptions.filter((o: any) => o?.deliverAs === "steer");
  check("T28: the runtime message API carries the failure", delivered.length > 0 && steered.length > 0, JSON.stringify(handle.host.messages));
  (globalThis as any).__steer = saved;
  stopBuildRun();
  answered(sel, "T28");
  noErrors(handle, "T28");
}

// Section: T29 — the supervisor's exit code survives an exit inside the build
async function caseT29(): Promise<void> {
  const failing = writeRunScript(runPaths(`test-fail-${Date.now()}`), "exit 3");
  await runArgv(["pwsh", "-NoLogo", "-File", failing.script], 60000);
  const got = readFileSync(failing.marker, "utf-8").trim();
  check("T29: build exit 3 still writes the code", got === "3", `marker=${got}`);
  const green = writeRunScript(runPaths(`test-ok-${Date.now()}`), "Write-Output ok");
  await runArgv(["pwsh", "-NoLogo", "-File", green.script], 60000);
  check("T29: a green build writes 0", readFileSync(green.marker, "utf-8").trim() === "0", "marker read");
}

// Section: T30 — a corrupt marker is a failure, never silence
async function caseT30(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams({ capture: "out" });
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, buildSeams(repo, mkRunner([]), pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  writeFileSync(markerOf(pane.sent[0]), "not-a-code");
  await waitFor(() => steersText(handle).some((w) => w.includes("no readable code")));
  check("T30: reported as a failure naming the raw marker", steersText(handle).some((w) => w.includes("no readable code")), JSON.stringify(steersText(handle)));
  stopBuildRun();
  answered(sel, "T30");
  noErrors(handle, "T30");
}

// Section: T31 — an unreadable VM probe stops the publish, never the commit
async function caseT31(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
    { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
    { match: "git add -A", result: okResult() },
    { match: "git commit -m *", result: okResult() },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [COMMIT_PROMPT]: "Commit" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T31: the failed probe is retried once, then refused", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 2, runner.calls.join(" | "));
  check("T31: the commit ran without the gate", runner.calls.includes("git add -A") && runner.calls.some((c) => c.startsWith('git commit -m "Publish (1 files)"')), runner.calls.join(" | "));
  check("T31: no queue from an unreadable VM layer", !runner.calls.some((c) => c.includes("prx")), runner.calls.join(" | "));
  checkT31Report({ repo, runner, pane: null, sel, handle });
}

/** T31's delivery half: the warning names the code, the summary names the stop. */
function checkT31Report(p: Parts): void {
  check("T31: exit code and stderr reach the warning", steersText(p.handle).some((w) => w.includes("Get-VM failed (exit 1)") && w.includes("Access is denied")), JSON.stringify(steersText(p.handle)));
  const summary = noticesOf(p.handle).join("\n");
  check("T31: the report commits first, then names the VM error", summary.indexOf("committed: Publish") >= 0 && summary.indexOf("committed: Publish") < summary.indexOf("Get-VM failed (exit 1)"), summary);
  check("T31: the summary says the publish stopped", summary.includes("publish stopped"), summary);
  answered(p.sel, "T31");
  noErrors(p.handle, "T31");
}

// Section: T32 — a probe that reported nothing is loud, never "already running"
async function caseT32(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("") }]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const msg = steersText(handle).join("\n");
  check("T32: every unseen agent named", msg.includes("did not report C11, C12, C13, C14"), msg);
  check("T32: the silent read is named as silent", msg.includes("no output on either stream"), msg);
  check("T32: no already-running claim", !msg.includes("already running"), msg);
  check("T32: no commit, no queue", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
  answered(sel, "T32");
  noErrors(handle, "T32");
}

// Section: T33 — a partial probe names exactly the agent it missed
async function caseT33(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("C12=Running\r\nC13=Running\r\nC14=Running\r\n") }]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const msg = steersText(handle).join("\n");
  check("T33: names C11", msg.includes("did not report C11"), msg);
  check("T33: the agents it did see are not blamed", !msg.includes("C12") && !msg.includes("C13"), msg);
  answered(sel, "T33");
  noErrors(handle, "T33");
}

// Section: T34 — a state nothing can start fails the gate with the state named
async function caseT34(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([{ match: VM_CHECK_PREFIX, result: okResult("C11=Paused\r\nC12=Running\r\nC13=Running\r\nC14=Running\r\n") }]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T34: the state is named, the publish stopped", steersText(handle).some((w) => w.includes("C11 is Paused") && w.includes("publish stopped")), JSON.stringify(steersText(handle)));
  check("T34: no blind Start-VM, no already-running claim", !runner.calls.some((c) => c.startsWith("Start-VM")) && !noticesOf(handle).some((n) => n.includes("already running")), runner.calls.join(" | "));
  answered(sel, "T34");
  noErrors(handle, "T34");
}

// Section: T35 — a Saved agent is startable and gets started
async function caseT35(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_SAVED) },
    { match: "git status --short", result: okResult("") },
    { match: "Start-VM -Name C11", result: okResult() },
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "prx", result: okResult() },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T35: the Saved agent was started", runner.calls.includes("Start-VM -Name C11"), runner.calls.join(" | "));
  check("T35: published once the queue confirm came", noticesOf(handle).some((n) => n.includes("published")) && runner.calls.includes("prx") && noWarning(handle), noticesOf(handle).join(" | ") + " | " + runner.calls.join(" | "));
  answered(sel, "T35");
  noErrors(handle, "T35");
}

// Section: T36 — a build start pre-warms the agents and does not wait
async function caseT36(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_OFF) },
    { match: "Start-VM -Name C11", result: okResult() },
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "git status --short", result: okResult("") },
    { match: "prx", result: okResult() },
  ]);
  const pane = mkPaneSeams();
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, buildSeams(repo, runner, pane, ["26.1.4"]), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T36: the agents are started before the pane takes the build", runner.calls[0].startsWith("Get-VM") && runner.calls[1].startsWith("Start-VM -Name C11") && pane.opened.length === 1, runner.calls.join(" | "));
  check("T36: the pre-warm does not wait", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 1, runner.calls.join(" | "));
  const started = noticesOf(handle).join("\n");
  check("T36: the started notice says they are booting", started.includes("booting during the build: C11") && started.includes("Build started in pane"), started);
  finishRun(pane, 0);
  await waitFor(() => noticesOf(handle).some((n) => n.includes("published")));
  checkT36Tail({ repo, runner, pane, sel, handle });
}

/** T36's tail: the gate found them running, so it never started them twice. */
function checkT36Tail(p: Parts): void {
  check("T36: the gate finds them running and publishes", noticesOf(p.handle).some((n) => n.includes("published")) && p.runner.calls.filter((c) => c.startsWith("Start-VM")).length === 1, p.runner.calls.join(" | "));
  stopBuildRun();
  answered(p.sel, "T36");
  noErrors(p.handle, "T36");
}

// Section: T37 — a broken pre-warm never stops the build
async function caseT37(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: "", stderr: "Get-VM: Access is denied." } },
    { match: "Start-VM -Name C11,C12,C13,C14", result: okResult() },
  ]);
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const started = noticesOf(handle).join("\n");
  check("T37: the build started with the probe broken", started.includes("Build started in pane") && started.includes("Get-VM failed (exit 1)"), started);
  check("T37: all four agents were blind-started anyway", runner.calls.includes("Start-VM -Name C11,C12,C13,C14"), runner.calls.join(" | "));
  check("T37: the broken probe steers a warning that spends no model turn", steersOf(handle).some((s) => s.content.includes("Get-VM failed (exit 1)") && s.triggerTurn === false) && warningToasts(handle).length === 0, JSON.stringify(steersOf(handle)));
  stopBuildRun();
  answered(sel, "T37");
  noErrors(handle, "T37");
}

/** T37b: a probe seam that throws is a note, never a failed build. */
async function caseT37Throwing(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const boom = { run: async () => { throw new Error("pwsh missing"); } };
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, gateSeams(repo, boom as any), sel.ui);
  await handle.runCommand("devexpress", "");
  const r = noticesOf(handle).join("\n");
  check("T37: a probe seam that throws is a note too", r.includes("Build started") && r.includes("Get-VM did not run: pwsh missing"), r);
  stopBuildRun();
  answered(sel, "T37-throwing");
  noErrors(handle, "T37-throwing");
}

// Section: T38 — a failed Start-VM in the pre-warm is a note
async function caseT38(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_OFF) },
    { match: "Start-VM -Name C11", result: { code: 1, stdout: "", stderr: "Start-VM: not enough memory" } },
  ]);
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const started = noticesOf(handle).join("\n");
  check("T38: the build starts, the failed start is a note", started.includes("Build started in pane") && started.includes("Start-VM failed: Start-VM: not enough memory"), started);
  check("T38: the failed start steers a warning that spends no model turn", steersOf(handle).some((s) => s.content.includes("not enough memory") && s.triggerTurn === false), JSON.stringify(steersOf(handle)));
  check("T38: no publish attempt", !runner.calls.some((c) => c.startsWith("git")), runner.calls.join(" | "));
  stopBuildRun();
  answered(sel, "T38");
  noErrors(handle, "T38");
}

// Section: T39 — the publish-only flow probes once and never pre-warms
async function caseT39(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "git status --short", result: okResult("") },
    { match: "prx", result: okResult() },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T39: one probe, then the queue", runner.calls.filter((c) => c.startsWith("Get-VM")).length === 1 && runner.calls.includes("prx"), runner.calls.join(" | "));
  check("T39: published", noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | "));
  answered(sel, "T39");
  noErrors(handle, "T39");
}

// Section: T40 — an agent that never boots fails the publish at the wait timeout
async function caseT40(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const calls: string[] = [];
  const stuck = {
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return okResult(VM_OFF);
      return okResult();
    },
  };
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, stuck), sel.ui);
  await handle.runCommand("devexpress", "");
  const msg = steersText(handle).join("\n");
  check("T40: the wait ends loudly", msg.includes("did not reach Running within 3 minutes"), msg || "(no warning)");
  check("T40: still nothing committed or queued", !calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), calls.join(" | "));
  answered(sel, "T40");
  noErrors(handle, "T40");
}

// Section: T41 — a probe killed mid-list refuses instead of acting on part of a plan
async function caseT41(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const KILLED = "C11=Off\r\nC12=Running\r\n";
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: KILLED, stderr: "" } },
    { match: "git status --short", result: okResult("") },
    { match: VM_CHECK_PREFIX, result: { code: 1, stdout: KILLED, stderr: "" } },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const msg = steersText(handle).join("\n");
  check("T41: the kill is reported, not the truncated list", msg.includes("Get-VM failed (exit 1)"), msg || "(no warning)");
  check("T41: no commit and no queue from a partial probe", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
  answered(sel, "T41");
  noErrors(handle, "T41");
}

/** A probe seam that answers the retry, recording the opts each call carried. */
function flakyProbe(): { calls: string[]; opts: any[]; run: (cmd: string, o?: any) => Promise<any> } {
  const calls: string[] = [];
  const opts: any[] = [];
  return {
    calls,
    opts,
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
}

// Section: T42 — a probe that answers the retry publishes, and asks for a profile-free pwsh
async function caseT42(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const flaky = flakyProbe();
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, flaky), sel.ui);
  await handle.runCommand("devexpress", "");
  const probes = flaky.calls.map((cmd, i) => ({ cmd, o: flaky.opts[i] })).filter((x) => x.cmd.startsWith("Get-VM"));
  check("T42: the retry answered and the publish went through", noticesOf(handle).some((n) => n.includes("published")) && probes.length === 2, noticesOf(handle).join(" | ") + " | " + flaky.calls.join(" | "));
  check("T42: the probe asked for a profile-free, prompt-free pwsh", probes.every((x) => x.o?.noProfile === true), JSON.stringify(probes.map((x) => x.o)));
  check("T42: one retry was enough, no warning", noWarning(handle), JSON.stringify(steersText(handle)));
  answered(sel, "T42");
  noErrors(handle, "T42");
}

// Section: T43 — an exit-0 read that listed nobody is retried and carries the probe's own words
async function caseT43(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const calls: string[] = [];
  const silent = {
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return { code: 0, stdout: "", stderr: "Get-VM: The service cannot be started." };
      return okResult();
    },
  };
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, silent), sel.ui);
  await handle.runCommand("devexpress", "");
  const msg = steersText(handle).join("\n");
  check("T43: the silent read is probed twice before it refuses", calls.filter((c) => c.startsWith("Get-VM")).length === 2, calls.join(" | "));
  check("T43: the refusal names the agents and quotes the probe", msg.includes("did not report C11, C12, C13, C14") && msg.includes("The service cannot be started"), msg || "(no warning)");
  check("T43: nothing committed or queued from a silent read", !calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), calls.join(" | "));
  check("T43: the summary says the publish stopped", noticesOf(handle).join("\n").includes("publish stopped"), noticesOf(handle).join(" | "));
  answered(sel, "T43");
  noErrors(handle, "T43");
}

// Section: T44 — an empty read that answers the retry publishes
async function caseT44(): Promise<void> {
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
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, onceEmpty), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T44: the empty read answered on the retry and the publish went through", noticesOf(handle).some((n) => n.includes("published")) && calls.filter((c) => c.startsWith("Get-VM")).length === 2, noticesOf(handle).join(" | ") + " | " + calls.join(" | "));
  check("T44: no warning out of a recovered read", noWarning(handle), JSON.stringify(steersText(handle)));
  answered(sel, "T44");
  noErrors(handle, "T44");
}

/** A probe seam that always answers one fixed read, recording its calls. */
function fixedProbe(read: string): { calls: string[]; run: (cmd: string) => Promise<any> } {
  const calls: string[] = [];
  return {
    calls,
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) return okResult(read);
      if (cmd === "git status --short") return okResult("");
      return okResult();
    },
  };
}

// Section: T45 — the probe's real CRLF shape reads every agent
async function caseT45(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const probe = fixedProbe(VM_RUN);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, probe), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T45: no agent started from a complete CRLF read", !probe.calls.some((c) => c.startsWith("Start-VM")), probe.calls.join(" | "));
  check("T45: one probe from a complete CRLF read, no warning", probe.calls.filter((c) => c.startsWith("Get-VM")).length === 1 && noWarning(handle), probe.calls.join(" | ") + " | " + steersText(handle).join(" | "));
  check("T45: the CRLF read published", noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | "));
  answered(sel, "T45");
  noErrors(handle, "T45");
}

/** A probe seam whose FIRST read names C11 Off and whose second reads green. */
function oneOffProbe(): { calls: string[]; run: (cmd: string) => Promise<any> } {
  const calls: string[] = [];
  return {
    calls,
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.startsWith("Get-VM -Name C11,C12,C13,C14")) {
        return okResult(calls.filter((c) => c.startsWith("Get-VM")).length === 1 ? VM_OFF : VM_RUN);
      }
      if (cmd === "git status --short") return okResult("");
      return okResult();
    },
  };
}

// Section: T46 — a CRLF read that names one agent Off starts exactly that agent
async function caseT46(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const probe = oneOffProbe();
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, probe), sel.ui);
  await handle.runCommand("devexpress", "");
  const started = probe.calls.filter((c) => c.startsWith("Start-VM"));
  check("T46: a CRLF read that names C11 Off starts exactly C11", started.length === 1 && started[0] === "Start-VM -Name C11", probe.calls.join(" | "));
  check("T46: the wait saw them running and the publish went through", noticesOf(handle).some((n) => n.includes("published")) && noWarning(handle), noticesOf(handle).join(" | ") + " | " + steersText(handle).join(" | "));
  answered(sel, "T46");
  noErrors(handle, "T46");
}

// Section: T47 — a bare-LF read, the other seam shape, still parses
async function caseT47(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const probe = fixedProbe(VM_LF);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, gateSeams(repo, probe), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T47: an LF read is still read as four agents and publishes", noticesOf(handle).some((n) => n.includes("published")) && noWarning(handle) && probe.calls.filter((c) => c.startsWith("Get-VM")).length === 1, noticesOf(handle).join(" | ") + " | " + probe.calls.join(" | "));
  answered(sel, "T47");
  noErrors(handle, "T47");
}

// Section: T48 — the agents boot while the commit prompt is open
async function caseT48(): Promise<void> {
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
  const atPrompt: string[][] = [];
  const sel = uiFor(
    { DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [COMMIT_PROMPT]: "Commit", [PUBLISH_PROMPT]: "Publish" },
    (title: string) => {
      if (title.includes(COMMIT_PROMPT)) atPrompt.push([...runner.calls]);
    },
  );
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  checkT48({ repo, runner, pane: null, sel, handle }, atPrompt);
}

/** T48's checks: what had already run at the prompt, and what the prompt said. */
function checkT48(p: Parts, atPrompt: string[][]): void {
  const seen = atPrompt[0] ?? [];
  check("T48: the probe and the start already ran at the prompt", seen.some((c) => c.startsWith("Start-VM")) && seen.some((c) => c.startsWith("Get-VM")), JSON.stringify(seen));
  check("T48: the commit itself had not started", !seen.some((c) => c.startsWith("git add")), JSON.stringify(seen));
  check("T48: the prompt carries counts and areas, never a path", p.sel.asks.some((t) => t.includes("1 file: 1 modified") && t.includes("areas: src") && !t.includes("src/x.cs")), p.sel.asks.join(" | "));
  check("T48: committed after the answer, then published", p.runner.calls.includes("git add -A") && p.runner.calls.includes("prx") && noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | ") + " | " + p.runner.calls.join(" | "));
  answered(p.sel, "T48");
  noErrors(p.handle, "T48");
}

// Section: T49 — a dirty tree at the push is a prompt, not a silent push
async function caseT49(): Promise<void> {
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
  const sel = uiFor({ DevExpress: "Publish", Project: "eXpand", eXpand: "Lab", [PUBLISH_PROMPT]: "Publish", [PUSH_PROMPT]: "Commit before push" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  checkT49({ repo, runner, pane: null, sel, handle });
}

/** T49's checks: the prompt carries the summary, and the commit precedes the push. */
function checkT49(p: Parts): void {
  const push = p.runner.calls.indexOf("git push lab HEAD:master");
  const add = p.runner.calls.indexOf("git add -A");
  check("T49: the dirty window is asked about, with a summary", p.sel.asks.some((t) => t.includes(PUSH_PROMPT) && t.includes("1 file: 1 modified") && t.includes("areas: Xpand/Xpand.Utils")), p.sel.asks.join(" | "));
  check("T49: Commit before push commits first, then pushes", add >= 0 && push > add, p.runner.calls.join(" | "));
  check("T49: the queue followed the push", p.runner.calls.includes("px") && noticesOf(p.handle).some((n) => n.includes("published")), noticesOf(p.handle).join(" | ") + " | " + p.runner.calls.join(" | "));
  answered(p.sel, "T49");
  noErrors(p.handle, "T49");
}

// Section: T50 — Push as is pushes the tree without committing it
async function caseT50(): Promise<void> {
  const repo = mkRepo(DX_PINS, join("Xpand", "Xpand.ExpressApp.Modules"));
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "git status --short", result: okResult("") },
    { match: "git status --short", result: { code: 0, stdout: " M build.ps1\n", stderr: "" } },
    { match: "git push lab HEAD:master", result: okResult() },
    { match: "px", result: okResult() },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "eXpand", eXpand: "Lab", [PUBLISH_PROMPT]: "Publish", [PUSH_PROMPT]: "Push as is" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  check("T50: a top-level file reads as (root), never as a path", sel.asks.some((t) => t.includes("areas: (root) (1)") && !t.includes("build.ps1")), sel.asks.join(" | "));
  check("T50: no commit from a Push as is answer", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit")), runner.calls.join(" | "));
  check("T50: pushed as it is and queued", runner.calls.includes("git push lab HEAD:master") && runner.calls.includes("px") && noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | ") + " | " + runner.calls.join(" | "));
  answered(sel, "T50");
  noErrors(handle, "T50");
}

// Section: T51 — Abort at the push stops before the push and the queue
async function caseT51(): Promise<void> {
  const repo = mkRepo(DX_PINS, join("Xpand", "Xpand.ExpressApp.Modules"));
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "git status --short", result: okResult("") },
    { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "eXpand", eXpand: "Lab", [PUBLISH_PROMPT]: "Publish", [PUSH_PROMPT]: "Abort" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const summary = noticesOf(handle).join("\n");
  check("T51: no push and no queue after the abort", !runner.calls.some((c) => c.startsWith("git push") || c === "px"), runner.calls.join(" | "));
  check("T51: the abort is named and the summary stopped", summary.includes("push aborted") && summary.includes("publish stopped"), summary);
  check("T51: a user abort is not a warning", noWarning(handle), JSON.stringify(steersText(handle)));
  answered(sel, "T51");
  noErrors(handle, "T51");
}

// Section: T52 — a status read that failed refuses instead of reading as clean
async function caseT52(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_RUN) },
    { match: "git status --short", result: { code: 1, stdout: "", stderr: "fatal: not a git repository" } },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const summary = noticesOf(handle).join("\n");
  check("T52: the failed read is named, never read as clean", summary.includes("git status failed (exit 1)") && summary.includes("not a git repository"), summary);
  check("T52: nothing committed and nothing queued", !runner.calls.some((c) => c.startsWith("git add") || c.startsWith("git commit") || c.includes("prx")), runner.calls.join(" | "));
  check("T52: the publish stopped", summary.includes("publish stopped"), summary);
  answered(sel, "T52");
  noErrors(handle, "T52");
}

// Section: T53 — aborting the commit drops the VM outcome with it
async function caseT53(): Promise<void> {
  const repo = mkRepo(DX_PINS);
  const runner = mkRunner([
    { match: VM_CHECK_PREFIX, result: okResult(VM_SAVED) },
    { match: "git status --short", result: { code: 0, stdout: " M src/x.cs\n", stderr: "" } },
    { match: "Start-VM -Name C11", result: okResult() },
  ]);
  const sel = uiFor({ DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [COMMIT_PROMPT]: "Abort" });
  const handle = await menuHandle(repo, gateSeams(repo, runner), sel.ui);
  await handle.runCommand("devexpress", "");
  const summary = noticesOf(handle).join("\n");
  check("T53: the abort stops before the commit and the queue", !runner.calls.some((c) => c.startsWith("git add") || c.includes("prx")) && summary.includes("commit aborted"), summary + " | " + runner.calls.join(" | "));
  check("T53: the dropped VM outcome is not reported as waited for", !summary.includes("starting Hyper-V agents"), summary);
  check("T53: the summary stopped without a warning", summary.includes("publish stopped") && noWarning(handle), summary + " | " + steersText(handle).join(" | "));
  answered(sel, "T53");
  noErrors(handle, "T53");
}

// Section: T57 — an unanswered seam cannot wedge the watch. The tick marks
// itself busy before its first await and clears it in a `finally`: one seam
// that never settles left the marker unread for good, the run active and every
// later build refused. The three advisory seams get a bound; the marker read is
// a local file read and never waits on them.
async function caseT57(): Promise<void> {
  stopBuildRun();
  const repo = mkRepo(DX_PINS);
  const pane = mkPaneSeams();
  const runner = mkRunner(GREEN_PUBLISH);
  // Seams that answer long after the tick's budget: for the tick that is no
  // answer at all, and the answer is still a plain delay, not a promise built
  // by hand.
  const lateAlive = async (): Promise<{ alive: boolean; pid?: number }> => {
    await sleep(5000);
    return { alive: true, pid: 4242 };
  };
  const lateText = async (): Promise<string> => {
    await sleep(5000);
    return "";
  };
  const lateCpu = async (): Promise<number> => {
    await sleep(5000);
    return 1;
  };
  const seams = buildSeams(repo, runner, pane, ["26.1.4"]);
  seams.sampleCpu = lateCpu;
  seams.capturePane = lateText;
  seams.probePane = lateAlive;
  const sel = uiFor({ ...RX_LAB, [DX_PROMPT]: "Skip", [PUBLISH_PROMPT]: "Publish" });
  const handle = await menuHandle(repo, seams, sel.ui);
  await handle.runCommand("devexpress", "");
  finishRun(pane, 0);
  const done = await waitFor(() => noticesOf(handle).some((n) => n.includes("published")), 3000);
  check("T57: an unanswered sampler, capture and probe cannot wedge the watch", done && !isBuildRunActive(), `active=${isBuildRunActive()} | ` + noticesOf(handle).join(" | "));
  stopBuildRun();
  answered(sel, "T57");
  noErrors(handle, "T57");
}

/** The contract, in the order it runs: T9's repo is fresh per case, so a case
 *  that "continues" an earlier one builds its own clean tree. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["T1", caseT1],
  ["T2", caseT2],
  ["T3", caseT3],
  ["T55", caseT55],
  ["T4", caseT4],
  ["T4b", caseT4b],
  ["T5", caseT5],
  ["T6", caseT6],
  ["T7", caseT7],
  ["T8", caseT8],
  ["T9", caseT9],
  ["T10", () => caseT10({ repo: mkRepo(DX_PINS) })],
  ["T11", caseT11],
  ["T12", caseT12],
  ["T13", caseT13],
  ["T14", caseT14],
  ["T15", caseT15],
  ["T16", caseT16],
  ["T18", caseT18],
  ["T19", () => caseT19({ repo: mkRepo(DX_PINS), runner: mkRunner([...GREEN_PUBLISH]), pane: mkPaneSeams() })],
  ["T21", caseT21],
  ["T22", caseT22],
  ["T23", caseT23],
  ["T24", caseT24],
  ["T25", caseT25],
  ["T26", caseT26],
  ["T27", caseT27],
  ["T28", caseT28],
  ["T29", caseT29],
  ["T30", caseT30],
  ["T31", caseT31],
  ["T32", caseT32],
  ["T33", caseT33],
  ["T34", caseT34],
  ["T35", caseT35],
  ["T36", caseT36],
  ["T37", caseT37],
  ["T37-throwing", caseT37Throwing],
  ["T38", caseT38],
  ["T39", caseT39],
  ["T40", caseT40],
  ["T41", caseT41],
  ["T42", caseT42],
  ["T43", caseT43],
  ["T44", caseT44],
  ["T45", caseT45],
  ["T46", caseT46],
  ["T47", caseT47],
  ["T48", caseT48],
  ["T49", caseT49],
  ["T50", caseT50],
  ["T51", caseT51],
  ["T52", caseT52],
  ["T53", caseT53],
  ["T57", caseT57],
];

/** One case's failure is reported and counted, never a crash that hides the
 *  rest of the contract. */
async function runCase(label: string, run: () => Promise<void>): Promise<void> {
  try {
    await run();
  } catch (e) {
    fail++;
    console.log(`FAIL  ${label} crashed: ${(e as Error)?.stack ?? e}`);
  }
}

async function main(): Promise<void> {
  console.log("reactive-xaf-build/build-tests — pi's own runtime, no fake\n");
  for (const [label, run] of CASES) await runCase(label, run);
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
