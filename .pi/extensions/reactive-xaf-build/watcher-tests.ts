/**
 * reactive-xaf-build/watcher-tests — the AzDO chain watcher's contract (W1-W60).
 *
 * W59/W60 are the nuget pressure cases: a package nuget.org indexes late is
 * still confirmed inside the shipped window, and one that never lands spends the
 * whole window before the warning. W59 also pins one poll at a time — a
 * multi-minute assert overlaps interval ticks, so `pollTick` skips a tick that
 * arrives mid-tick instead of advancing the chain twice (`watcher.md`).
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (commit 93e1a4754): `./resolve.mjs` installs
 * the `@pi/` name floor first, and the harness plus the seam-owning modules are
 * DYNAMIC imports after it. This suite imports no boot proof, because nothing
 * here spawns.
 *
 * The flow is driven through pi's own command dispatch (Publish → RX-XAF |
 * eXpand → Lab | Release) with the extension's OWN ports injected: the command
 * runner, the feed fetcher, `ghFetch` and a fast `startAzDoWatcher` seam, so the
 * real AzDO, GitHub, nuget.org and pwsh are never touched. watcher.ts delivers
 * through `ctx.ui.notify` (a toast) and `pi.sendUserMessage(msg, { deliverAs:
 * "steer" })` (the agent's message), so a case asserts the toasts
 * (`handle.host.notices`) and the steers (`handle.host.messages` with
 * `messageOptions` index-aligned).
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/watcher-tests.ts
 */
// test-timeout: 120000 — one runner build per case, plus the chain's own 20 ms cadence
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
/** Local delay: the test harness owns its own clock. */
const sleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
import { startAzDoWatcher, stopAzDoWatcher, isAzDoWatcherActive } from "./watcher.js";
import { renderProgressLine } from "./azdo.js";
import { renderTestRefusal, renderTestScan } from "./test-scan.js";
import { newTestWatch, testScan } from "./test-watch.js";
import { expandProfile } from "./profile.js";

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

/** The toasts the flow showed the user, with the type each carried. */
function toastsOf(handle: any): Array<{ message: string; type: string }> {
  return handle.host.notices.map((n: any) => ({ message: String(n.message ?? ""), type: String(n.type ?? "info") }));
}

function toastText(handle: any): string[] {
  return toastsOf(handle).map((t) => t.message);
}

/** The messages the agent got: watcher.ts steers with `pi.sendUserMessage`, so
 *  the payload is a plain string and the delivery sits in `messageOptions`. A
 *  shared-sender payload (`{ content }`) normalizes to the same shape. */
function steersOf(handle: any): Array<{ content: string; deliverAs?: string }> {
  return handle.host.messages.map((m: any, i: number) => ({
    content: String(m?.content ?? m ?? ""),
    deliverAs: handle.host.messageOptions[i]?.deliverAs as string | undefined,
  }));
}

/** The handler errors the runner REPORTED instead of throwing; an error is a
 *  no-op unless a case looks for it, so every case looks. */
function errorsOf(handle: any): string {
  return JSON.stringify(handle.host.errors);
}

/** A case's answer for one prompt: a literal option, or a resolver over the
 *  offered options for a title whose list is built at run time. */
type Answer = string | ((options: string[]) => string | undefined);

interface CaseUi {
  ui: Record<string, unknown>;
  asks: string[];
  unexpected: string[];
}

/** The case's own ui.select override, keyed by a SUBSTRING of the prompt title,
 *  LONGEST match first (the DX prompt's title spells "…update all DevExpress.*
 *  pins?" and the top menu's own key is `DevExpress`). A title the case did not
 *  map, or an answer that is not one of the offered options, is RECORDED — the
 *  harness would otherwise answer with the first option — and every case asserts
 *  the list is empty. A case that supplies this override REPLACES the harness's
 *  own select, so `host.prompts` stays empty and `sel.asks` is the record. */
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

/** Every prompt the flow showed was answered by the case. */
function answered(sel: CaseUi, where: string): void {
  check(`${where}: every prompt the flow showed was answered by the case`, sel.unexpected.length === 0, sel.unexpected.join(" | "));
}

/** The runner reports a throwing handler instead of throwing, so an error is a
 *  no-op unless a case looks for it. Every case looks. */
function noErrors(handle: any, where: string): void {
  check(`${where}: no handler error`, handle.host.errors.length === 0, errorsOf(handle));
}

/** pi's own runtime for one case: the real command, the case's picks and ports,
 *  and the case's own watcher cadence. */
async function watcherHandle(
  repo: string, seams: Record<string, unknown>, picks: Record<string, Answer>,
  observe?: (title: string, options: string[]) => void,
): Promise<{ handle: any; sel: CaseUi }> {
  const sel = uiFor(picks, observe);
  await installRoute();
  const handle = await buildRealRunner({
    cwd: repo,
    ui: sel.ui,
    activate: (pi: any) => registerBuildCommand(pi, { repoRoot: repo, ...seams }),
  });
  handles.push(handle);
  return { handle, sel };
}

function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-watcher-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  mkdirSync(join(root, "src", "Common"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  writeFileSync(join(root, "src", "Common", "AssemblyInfoVersion.cs"), 'class AssemblyInfoVersion { public const string Version = "4.261.2.1"; }');
  return root;
}
function mkExpandRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "xpand-watcher-"));
  tempDirs.push(root);
  mkdirSync(join(root, "Xpand", "Xpand.ExpressApp.Modules"), { recursive: true });
  mkdirSync(join(root, "Xpand", "Xpand.Utils", "Properties"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  writeFileSync(join(root, "Xpand", "Xpand.Utils", "Properties", "XpandAssemblyInfo.cs"), 'public class XpandAssemblyInfo { public const string Version = "26.1.400.0"; }');
  return root;
}
function crlf(lines: string[]): string {
  return lines.join("\r\n") + "\r\n";
}
/** A test scan in which the test records moved since the last poll. The wire is
 *  built by the PRODUCER's own renderer (`renderTestScan`), for the same reason
 *  `renderProgressLine` builds the PROGRESS fixtures: a fixture that re-typed
 *  the format would stay green while the script's own line drifted. */
function testFails(names: string[], at = "2026-10-26T08:00:00Z"): string {
  return renderTestScan(names, at);
}

/** The operator's real role entry, as `.model-roles.json` carries it. */
const TEST_ROLE_ENTRY = { role: "Test-Runner", modelId: "deepseek-flash", provider: "deepseek", thinkingLevel: "max" };

/** Publish a roles store the way pi does: model-role's OWN `initModelRoles`
 *  reads a file and publishes `globalThis.__pi_model_roles`. Hand-assigning that
 *  slot would fabricate the lookup these cases exist to assert — a wrong field
 *  name or a nested store shape would stay green. */
async function publishRoles(roles: Record<string, unknown>): Promise<void> {
  const dir = mkdtempSync(join(tmpdir(), "rxaf-roles-"));
  tempDirs.push(dir);
  const file = join(dir, "roles.json");
  writeFileSync(file, JSON.stringify(roles));
  const { initModelRoles } = await import("@pi/model-role/state.js");
  initModelRoles(file);
}

function running(id: number): string {
  return crlf([`STATUS=${id};inProgress;;`]);
}
/** An in-flight poll carrying a TASK reading, with the task's last log line
 *  `silentMinutes` ago — a real elapsed time, so the threshold is exercised from
 *  BOTH sides instead of only from far above it. The line is built by the
 *  parser's own renderer, so a fixture cannot drift from the wire. */
function runningWithTask(id: number, task: string, silentMinutes: number): string {
  const lastOutputAt = Date.now() - silentMinutes * 60_000;
  const line = renderProgressLine({ kind: "running", activeName: task, activeSince: Date.parse("2026-10-25T14:39:50Z"), lastOutputAt, source: "task" });
  return crlf([line, `STATUS=${id};inProgress;;`]);
}
/** Build 36111's own progress line, captured from the AzDO API (the exact
 *  strings are recorded in `azdo.md`): the `Run Tests` task inside
 *  `Tests Workflow`, last log line 16:04:20Z after 80 minutes of silence.
 *  Replayed verbatim — the wire format must not be this file's invention — and
 *  it carries NO `source` field, which is the older shape W24 pins. */
const CAPTURED_STALL = "PROGRESS=1;Tests Workflow / Run Tests;2026-10-25T14:39:50Z;2026-10-25T16:04:20.1296030Z";
/** Build 36117's own test poll, captured from the live API on 2026-10-26 and
 *  replayed VERBATIM — for the same reason `CAPTURED_STALL` is: the wire format
 *  must not be this file's invention. Two runs' results, out of order exactly as
 *  Azure published them, and the run-level watermark the scan printed. A fixture
 *  built by `renderTestScan` alone cannot catch a pwsh-side format drift, and
 *  that seam is exactly where the phantom name came from. */
const CAPTURED_TESTFAIL = crlf([
  "TESTLINE=1023516;2026-10-26T05:05:48Z;Synchronize_styles_when_document_modified",
  "TESTLINE=1023520;2026-10-26T05:22:44Z;Can_Get_Correct_StackTrace_From_Nested_Method",
  "TESTLINE=1023520;2026-10-26T05:22:44Z;Chained_Action_Helpers_Propagate_Correct_Stack_Trace",
  "TESTSEEN=2026-10-26T06:13:54Z",
]);
/** Build 36120's live poll, which printed NOTHING at all while the build was
 *  inProgress: the empty answer that had named a passing job's log warning as a
 *  failed test. Captured before the `TESTRUNS` count line existed — W58 covers
 *  that line and the counts it carries. */
const CAPTURED_EMPTY = "";
/** An in-flight poll whose live task opened NO log of its own: the reading is
 *  the newest change across every log container, `silentMinutes` ago. This is
 *  build 36112's shape — its `Build` task had `log: null` while the newest
 *  container had last changed 2h03m before the poll. */
function runningWithWatermark(id: number, phase: string, silentMinutes: number): string {
  const lastOutputAt = Date.now() - silentMinutes * 60_000;
  const line = renderProgressLine({ kind: "running", activeName: phase, activeSince: Date.parse("2026-10-25T18:57:46Z"), lastOutputAt, source: "logs" });
  return crlf([line, `STATUS=${id};inProgress;;`]);
}
/** A build waiting for an agent: nothing has been produced yet, so nothing can
 *  be read about it and nothing is stalled. */
function queuedReading(id: number, queuedMinutes: number): string {
  const queuedAt = Date.now() - queuedMinutes * 60_000;
  return crlf([renderProgressLine({ kind: "queue", queuedAt }), `STATUS=${id};notStarted;;`]);
}
function done(id: number, result: string): string {
  return crlf([`STATUS=${id};completed;${result};`]);
}
function doneNum(id: number, result: string, n: string): string {
  return crlf([`STATUS=${id};completed;${result};${n};`]);
}
function failedWithLog(id: number, logLines: string[]): string {
  return crlf(["LOGSTART", ...logLines, "LOGEND", `STATUS=${id};completed;failed;`]);
}
function empty(): string {
  return crlf(["STATUS=0;none;none;"]);
}
function oDataFeed(versions: string[]): string {
  const entries = versions.map((v) => `<entry><id>https://xpandnugetserver.azurewebsites.net/nuget/Packages(Id='Xpand.Extensions',Version='${v}')</id></entry>`);
  return `<?xml version="1.0"?><feed>${entries.join("")}</feed>`;
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
function flowResult(cmd: string): any {
  if (cmd.startsWith("Get-VM")) return { code: 0, stdout: VM_RUN, stderr: "" };
  if (cmd === "git status --short" || cmd === "git push lab HEAD:master") return { code: 0, stdout: "", stderr: "" };
  if (cmd === "prx" || cmd === "px") return { code: 0, stdout: "Queued build", stderr: "" };
  if (cmd.startsWith("$cred")) return { code: 0, stdout: "QUEUED=4444", stderr: "" };
  return { code: 0, stdout: "", stderr: "" };
}
function mkSeams(statusQueue: string[], testQueue: string[] = []): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const queue = statusQueue.slice();
  const tests = testQueue.slice();
  const calls: string[] = [];
  return {
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.includes("# rxaf-testfail")) return { code: 0, stdout: tests.shift() ?? "", stderr: "" };
      if (cmd.includes("$top=5")) return { code: 0, stdout: queue.shift() ?? running(35760), stderr: "" };
      return flowResult(cmd);
    },
    calls,
  };
}
function fastWatcher(p: any, c: any, s: any, opts?: any): ReturnType<typeof startAzDoWatcher> {
  return startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000 });
}
/** The watcher with a test-sized empty-poll grace, so a real three-minute wait
 *  never enters a run. */
function graceWatcher(emptyGraceMs: number): (p: any, c: any, s: any, opts?: any) => ReturnType<typeof startAzDoWatcher> {
  return (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, emptyGraceMs });
}
/** The watcher with a test-sized stall threshold and NO wall-clock deadline:
 *  the deadline is a backstop a caller asks for, never a default. */
function stallWatcher(stallMs: number): (p: any, c: any, s: any, opts?: any) => ReturnType<typeof startAzDoWatcher> {
  return (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, stallMs });
}
/** The fast watcher plus a delegation spawner the case owns: the spawner is a
 *  watcher option, because the offer is the watcher's, not the flow's. */
function testWatcher(spawnWindow: (repo: string, task: string, launch?: any) => Promise<string | null>) {
  return (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, spawnWindow });
}
function mkFetch(nugetVersions: string[]): (url: string) => Promise<string> {
  return async (url: string) => {
    if (url.includes("xpandnugetserver")) return JSON.stringify({ feed: oDataFeed(nugetVersions) });
    return JSON.stringify({ versions: ["26.1.3"] });
  };
}
/** A nuget.org feed that refuses the flat-container nuspec `lagPolls` times and
 *  answers it afterwards — the indexing lag the assert has to outlast.
 *  `Infinity` never answers: a package that truly did not land. */
function mkLaggingFetch(lagPolls: number, lag: { calls: number }): (url: string) => Promise<string> {
  return async (url: string) => {
    if (url.includes("api.nuget.org")) {
      lag.calls++;
      if (lag.calls <= lagPolls) throw new Error("404 — flatcontainer not indexed yet");
      return "<package><metadata><id>Xpand.Extensions</id></metadata></package>";
    }
    return JSON.stringify({ feed: oDataFeed(LAB) });
  };
}
function mkGh(list: (attempt: number) => string): { gh: (url: string, opts?: any) => Promise<any>; patches: string[] } {
  let calls = 0;
  const patches: string[] = [];
  const gh = async (_url: string, opts: any = {}) => {
    if (opts.method === "PATCH") {
      patches.push(opts.body);
      return { ok: true, status: 200, text: "{}" };
    }
    calls++;
    return { ok: true, status: 200, text: list(calls) };
  };
  return { gh, patches };
}
const GH_DRAFT = () => JSON.stringify([{ id: 111, tag_name: "4.261.2.1", draft: true }]);
const GH_MISSING = () => JSON.stringify([{ id: 222, tag_name: "4.242.3", draft: true }]);
const GH_LATE = (attempt: number) => (attempt === 1 ? "[]" : GH_DRAFT());
const GH_EXPAND = () => JSON.stringify([{ id: 333, tag_name: "26.1.400.0", draft: false }]);
const LAB = ["4.261.2.1"];
const GREEN = [done(35760, "succeeded"), done(35780, "succeeded"), done(35790, "succeeded")];
/** The menu picks a case answers: Publish → RX-XAF | eXpand → Lab | Release,
 *  then the queue confirm. */
const PUBLISH_PROMPT = "Publish: ";
const PUBLISH_LAB: Record<string, Answer> = { DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", [PUBLISH_PROMPT]: "Publish" };
const PUBLISH_RELEASE: Record<string, Answer> = { DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Release", [PUBLISH_PROMPT]: "Publish" };
const PUBLISH_EXPAND: Record<string, Answer> = { DevExpress: "Publish", Project: "eXpand", eXpand: "Lab", [PUBLISH_PROMPT]: "Publish" };
/** The picks that keep the flow quiet while the failing tests are reported: the
 *  shared prologue of W40-W43 and W47/W48. */
const KEEP_TESTS: Record<string, Answer> = { ...PUBLISH_LAB, "Tests failing": "Keep watching" };

// Section: W1 — the full Lab chain, green
async function caseW1(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760), running(35760), ...GREEN]);
  const gh = mkGh(GH_DRAFT);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: gh.gh, startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  check("W1: publish returns immediately, monitoring in background", toastText(w.handle).some((n) => n.includes("monitoring in background")) && toastText(w.handle).some((n) => n.includes("published")), toastText(w.handle).join(" | "));
  check("W1: watcher active right after publish", isAzDoWatcherActive());
  await sleep(400);
  checkW1(w.handle, gh);
  answered(w.sel, "W1");
  noErrors(w.handle, "W1");
}

/** W1's tail: the per-poll toasts, the draft PATCH and the silent success. */
function checkW1(handle: any, gh: { patches: string[] }): void {
  const toasts = toastText(handle);
  check("W1: toast on every poll (both running checks toasted)", toasts.filter((n) => n.includes("inProgress")).length >= 2, JSON.stringify(toasts));
  check("W1: nugets asserted on the eXpand server", toasts.some((n) => n.includes("Nugets published") && n.includes("eXpand nuget server")), JSON.stringify(toasts));
  check("W1: chain advanced to the release consumers pipeline", toasts.some((n) => n.includes("release consumers pipeline")), JSON.stringify(toasts));
  check("W1: lab draft published as pre-release + chain complete + watcher stopped", toasts.some((n) => n.includes("GitHub pre-release 4.261.2.1 published from draft") && n.includes("chain complete")) && !isAzDoWatcherActive(), JSON.stringify(toasts));
  check("W1: PATCH carried draft=false prerelease=true", gh.patches.length === 1 && JSON.parse(gh.patches[0]).prerelease === true && JSON.parse(gh.patches[0]).draft === false, JSON.stringify(gh.patches));
  check("W1: no steer on success", handle.host.messages.length === 0, JSON.stringify(handle.host.messages));
}

// Section: W2 — a failed Reactive.XAF build steers and stops
async function caseW2(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([done(35761, "failed")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(200);
  check("W2: failure toast is a warning", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("FAILED")), JSON.stringify(toastsOf(w.handle)));
  const steers = steersOf(w.handle);
  check("W2: failure steers (deliverAs steer)", steers.length === 1 && steers[0].deliverAs === "steer" && steers[0].content.includes("FAILED"), JSON.stringify(steers));
  check("W2: watcher stopped", !isAzDoWatcherActive());
  answered(w.sel, "W2");
  noErrors(w.handle, "W2");
}

// Section: W3 — the give-up deadline
async function caseW3(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 10, maxMs: 60 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(250);
  const steers = steersOf(w.handle);
  // ONE give-up steer, and only one. The blind-monitor steer is a SEPARATE
  // contract (W26/W28: progress that cannot be read is reported once), so the
  // total steer count is not this case's business — asserting it went red the
  // moment the blind monitor landed, on a flow that still gave up exactly once.
  const gaveUp = steers.filter((s) => s.content.includes("gave up"));
  check("W3: gave-up warning + steer + watcher stopped", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("gave up")) && gaveUp.length === 1 && !isAzDoWatcherActive(), JSON.stringify(toastsOf(w.handle)) + " | " + JSON.stringify(steers));
  answered(w.sel, "W3");
  noErrors(w.handle, "W3");
}

// Section: W4 — a new publish replaces the previous watcher
async function caseW4(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([]);
  const first = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await first.handle.runCommand("devexpress", "");
  check("W4: first watcher active", isAzDoWatcherActive());
  const second = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await second.handle.runCommand("devexpress", "");
  check("W4: second publish keeps exactly one watcher active", isAzDoWatcherActive());
  stopAzDoWatcher();
  check("W4: stopAzDoWatcher stops it", !isAzDoWatcherActive());
  answered(first.sel, "W4-first");
  answered(second.sel, "W4-second");
  noErrors(first.handle, "W4-first");
  noErrors(second.handle, "W4-second");
}

// Section: W5 — the command registers through the real index factory
async function caseW5(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("W5: devexpress command registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "W5");
}

// Section: W6 — a nuget version missing on the feed: steer, chain continues
async function caseW6(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(["4.242.3"]), ghFetch: mkGh(GH_DRAFT).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetries: 1, nugetRetryMs: 0 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const steers = steersOf(w.handle);
  check("W6: missing version → warning + steer", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("Nugets NOT confirmed")) && steers.length === 1 && steers[0].content.includes("NOT found"), JSON.stringify(toastsOf(w.handle)) + " | " + JSON.stringify(steers));
  check("W6: chain continued to the release consumers pipeline", toastText(w.handle).some((n) => n.includes("release consumers pipeline")), JSON.stringify(toastText(w.handle)));
  answered(w.sel, "W6");
  noErrors(w.handle, "W6");
}

// Section: W7 — a failed release consumers build steers and stops
async function caseW7(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([done(35760, "succeeded"), done(35780, "succeeded"), done(35790, "failed")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const steers = steersOf(w.handle);
  check("W7: release failure steers + stops", steers.length === 1 && steers[0].content.includes("FAILED") && !isAzDoWatcherActive(), JSON.stringify(steers));
  answered(w.sel, "W7");
  noErrors(w.handle, "W7");
}

// Section: W8 — a missing GitHub draft steers after its retries
async function caseW8(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_MISSING).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, ghRetries: 2, ghRetryMs: 10 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(500);
  const steers = steersOf(w.handle);
  check("W8: missing GitHub draft → warning + steer", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("GitHub release was NOT confirmed")) && steers.length === 1 && steers[0].content.includes("NOT found after 2 tries"), JSON.stringify(toastsOf(w.handle)) + " | " + JSON.stringify(steers));
  check("W8: watcher stopped", !isAzDoWatcherActive());
  answered(w.sel, "W8");
  noErrors(w.handle, "W8");
}

// Section: W9 — the draft appears on a retry: published, no steer
async function caseW9(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_LATE).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, ghRetries: 3, ghRetryMs: 10 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(500);
  check("W9: draft found on retry → published toast, no steer", toastText(w.handle).some((n) => n.includes("GitHub pre-release 4.261.2.1 published from draft")) && w.handle.host.messages.length === 0, JSON.stringify(toastText(w.handle)) + " | " + JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W9");
  noErrors(w.handle, "W9");
}

// Section: W10 — the Release chain
async function caseW10(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([done(40010, "succeeded"), done(40020, "succeeded"), done(40030, "succeeded")]);
  const gh = mkGh(GH_DRAFT);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: gh.gh, startAzDoWatcher: fastWatcher }, PUBLISH_RELEASE);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W10: release chain polls def 23 (same pipeline as lab)", seams.calls.some((c) => c.includes("definitions=23")), JSON.stringify(seams.calls));
  const toasts = toastText(w.handle);
  check("W10: release nugets asserted on nuget.org (normalized version)", toasts.some((n) => n.includes("Nugets published") && n.includes("nuget.org")), JSON.stringify(toasts));
  check("W10: release draft published as a FULL release + chain complete", toasts.some((n) => n.includes("GitHub release 4.261.2.1 published from draft") && n.includes("chain complete")) && gh.patches.length === 1 && JSON.parse(gh.patches[0]).prerelease === false, JSON.stringify(toasts) + " | " + JSON.stringify(gh.patches));
  check("W10: no steer on success", w.handle.host.messages.length === 0, JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W10");
  noErrors(w.handle, "W10");
}

// Section: W11 — a missing token steers by name, and the env survives the case
async function caseW11(): Promise<void> {
  const repo = mkRepo();
  const savedToken = process.env.GH_TOKEN;
  const savedAlt = process.env.GITHUB_TOKEN;
  delete process.env.GH_TOKEN;
  delete process.env.GITHUB_TOKEN;
  try {
    const seams = mkSeams(GREEN);
    const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
    await w.handle.runCommand("devexpress", "");
    await sleep(400);
    const steers = steersOf(w.handle);
    check("W11: missing token → warning + steer naming GH_TOKEN", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("GH_TOKEN is not set")) && steers.length === 1 && steers[0].content.includes("GH_TOKEN"), JSON.stringify(toastsOf(w.handle)) + " | " + JSON.stringify(steers));
    check("W11: watcher stopped", !isAzDoWatcherActive());
    answered(w.sel, "W11");
    noErrors(w.handle, "W11");
  } finally {
    if (savedToken !== undefined) process.env.GH_TOKEN = savedToken;
    if (savedAlt !== undefined) process.env.GITHUB_TOKEN = savedAlt;
  }
}

// Section: W12 — an empty poll retries, the chain completes
async function caseW12(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([empty(), ...GREEN]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_DRAFT).gh, startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const toasts = toastText(w.handle);
  check("W12: empty poll retried (no build found yet), chain completed, no give-up", toasts.some((n) => n.includes("no build found yet")) && toasts.some((n) => n.includes("chain complete")) && !isAzDoWatcherActive(), JSON.stringify(toasts));
  check("W12: no steer on success", w.handle.host.messages.length === 0, JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W12");
  noErrors(w.handle, "W12");
}

// Section: W13 — Release nugets missing on nuget.org: steer, chain continues
async function caseW13(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const fetchThrow = async (url: string) => {
    if (url.includes("api.nuget.org")) throw new Error("404");
    return JSON.stringify({ feed: oDataFeed(LAB) });
  };
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: fetchThrow, ghFetch: mkGh(GH_DRAFT).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetries: 1, nugetRetryMs: 0 }),
  }, PUBLISH_RELEASE);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const toasts = toastText(w.handle);
  check("W13: release nugets missing on nuget.org → warning + steer, chain continues", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("nuget.org")) && steersOf(w.handle).length === 1 && toasts.some((n) => n.includes("release consumers pipeline")), JSON.stringify(toasts) + " | " + JSON.stringify(steersOf(w.handle)));
  answered(w.sel, "W13");
  noErrors(w.handle, "W13");
}

// Section: W14 — expand Lab polls def 94
async function caseW14(): Promise<void> {
  const repo = mkExpandRepo();
  const seams = mkSeams([done(35805, "succeeded"), done(35806, "succeeded"), done(35807, "succeeded")]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(["26.1.400"]), ghFetch: mkGh(GH_EXPAND).gh,
    profile: expandProfile, startAzDoWatcher: fastWatcher,
  }, PUBLISH_EXPAND);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W14: expand lab polls def 94", seams.calls.some((c) => c.includes("definitions=94")), JSON.stringify(seams.calls));
  const toasts = toastText(w.handle);
  check("W14: 26.1.400.0 matches feed 26.1.400 — nugets confirmed, no steer", toasts.some((n) => n.includes("Nugets published") && n.includes("eXpandSystem")) && w.handle.host.messages.length === 0, JSON.stringify(toasts) + " | " + JSON.stringify(w.handle.host.messages));
  check("W14: GitHub already published + chain complete", toasts.some((n) => n.includes("GitHub release 26.1.400.0 already published") && n.includes("chain complete")), JSON.stringify(toasts));
  answered(w.sel, "W14");
  noErrors(w.handle, "W14");
}

// Section: W15 — the fail reason comes out of the log block
async function caseW15(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([failedWithLog(35802, ["##[error]Release 26.1.301.1 exists"])]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(200);
  const steers = steersOf(w.handle);
  check("W15: failure steer carries the log error, not no-error-lines", steers.length === 1 && steers[0].content.includes("Release 26.1.301.1 exists") && !steers[0].content.includes("no error lines"), JSON.stringify(steers));
  check("W15: watcher stopped", !isAzDoWatcherActive());
  answered(w.sel, "W15");
  noErrors(w.handle, "W15");
}

// Section: W16 — a finished head build of another version ends the watch AT ONCE:
// nothing was queued for this run's version, so waiting out a deadline would be
// the silent two-hour give-up all over again.
async function caseW16(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([doneNum(25698, "succeeded", "22.1.601.2")]);
  const probe = mkSteerProbe();
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  try {
    await w.handle.runCommand("devexpress", "");
    await sleep(300);
  } finally {
    probe.restore();
  }
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W16: the wrong version gives up at once, naming expected and found", warnings.some((n) => n.includes("22.1.601.2") && n.includes("expected 4.261.2.1")), JSON.stringify(warnings));
  check("W16: no waiting line — the watch is over", !toastText(w.handle).some((n) => n.includes("waiting")), JSON.stringify(toastText(w.handle)));
  check("W16: the re-queue instruction rides the give-up", warnings.some((n) => n.includes("Build & Publish")), JSON.stringify(warnings));
  check("W16: the steer is an ERROR and the watcher stopped", probe.calls.length === 1 && probe.calls[0].severity === "error" && !isAzDoWatcherActive(), JSON.stringify(probe.calls));
  answered(w.sel, "W16");
  noErrors(w.handle, "W16");
}

// Section: W17 — the index lag: missing on the first assert, present on retry
async function caseW17(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const lag = { calls: 0 };
  const fetchLate = mkLaggingFetch(1, lag);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: fetchLate, ghFetch: mkGh(GH_DRAFT).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetries: 3, nugetRetryMs: 10 }),
  }, PUBLISH_RELEASE);
  await w.handle.runCommand("devexpress", "");
  await sleep(500);
  const toasts = toastText(w.handle);
  check("W17: nugets confirmed on retry after index lag — no warning, no steer", lag.calls >= 2 && toasts.some((n) => n.includes("Nugets published") && n.includes("nuget.org")) && !toasts.some((n) => n.includes("NOT confirmed")) && w.handle.host.messages.length === 0, JSON.stringify(toasts) + " | " + JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W17");
  noErrors(w.handle, "W17");
}

// Section: W18 — the real AzDO build numbers: the chain head is
// `<version>-<dxVersion>`, its downstream pipelines are date-numbered.
async function caseW18(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([
    doneNum(35926, "succeeded", "4.261.2.1-26.1.3"),
    doneNum(35927, "succeeded", "20260919.1"),
    doneNum(35930, "succeeded", "20260919.1"),
  ]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_DRAFT).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 400 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(600);
  const toasts = toastText(w.handle);
  check("W18: the version-suffixed head and the date-numbered steps run the chain to completion", toasts.some((n) => n.includes("release consumers pipeline")) && toasts.some((n) => n.includes("chain complete")), JSON.stringify(toasts));
  check("W18: no wrong-version wait and no cap give-up on real build numbers", !toasts.some((n) => n.includes("is not this run")) && !toasts.some((n) => n.includes("gave up")) && w.handle.host.messages.length === 0, JSON.stringify(toasts) + " | " + JSON.stringify(w.handle.host.messages));
  check("W18: the downstream polls carried the chain's id baseline", seams.calls.some((c) => c.includes("definitions=72") && c.includes("-gt 35926")) && seams.calls.some((c) => c.includes("definitions=89") && c.includes("-gt 35927")), JSON.stringify(seams.calls));
  answered(w.sel, "W18");
  noErrors(w.handle, "W18");
}

// Section: W19 — empty polls have their own grace budget: inside it they retry,
// past it the watcher says there is nothing to watch instead of reporting
// "waiting…" until the two-hour deadline.
async function caseW19(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(Array.from({ length: 40 }, () => empty()));
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: graceWatcher(60_000),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const toasts = toastText(w.handle);
  check(
    "W19: empty polls inside the grace retry and never stop the watcher",
    isAzDoWatcherActive() && toasts.some((n) => n.includes("no build found yet")) && !toasts.some((n) => n.includes("nothing to watch")),
    JSON.stringify({ active: isAzDoWatcherActive(), toasts }),
  );
  check("W19: the retry does not steer", w.handle.host.messages.length === 0, JSON.stringify(w.handle.host.messages));
  stopAzDoWatcher();
  answered(w.sel, "W19");
  noErrors(w.handle, "W19");
}

/** W20: past the grace the watcher admits it — a warning plus a steer naming the
 *  real problem, and the watcher stops. */
async function caseW20(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(Array.from({ length: 40 }, () => empty()));
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: graceWatcher(0),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(200);
  const warning = toastsOf(w.handle).filter((n: { type: string }) => n.type === "warning").map((n: { message: string }) => n.message);
  const steers = w.handle.host.messages.map((m: any) => String(m?.content ?? m ?? ""));
  check(
    "W20: past the grace it stops with a warning naming the real problem",
    !isAzDoWatcherActive() && warning.some((n: string) => n.includes("no build was queued")),
    JSON.stringify({ active: isAzDoWatcherActive(), warning }),
  );
  check(
    "W20: the agent is steered with the same truth",
    steers.some((s: string) => s.includes("no build was queued") && s.includes("Build & Publish")),
    JSON.stringify(steers),
  );
  answered(w.sel, "W20");
  noErrors(w.handle, "W20");
}

/** W21: a build appearing inside the grace continues the chain — the clock is
 *  cleared, nothing is given up. */
async function caseW21(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([empty(), empty(), ...GREEN]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_DRAFT).gh, startAzDoWatcher: graceWatcher(60_000),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const toasts = toastText(w.handle);
  check(
    "W21: a build inside the grace continues the chain",
    toasts.some((n) => n.includes("chain complete")) && !toasts.some((n) => n.includes("nothing to watch")),
    JSON.stringify(toasts),
  );
  check("W21: no give-up steer", w.handle.host.messages.length === 0, JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W21");
  noErrors(w.handle, "W21");
}

// Section: W22 — a refused token is an ERROR on the FIRST poll, never "no build
// found yet": an expired PAT makes the status script fail, and that failure has
// to reach the user as the token problem it is, with the remedy.
function mkAuthRun(): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const calls: string[] = [];
  return {
    calls,
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.includes("queryOrder=queueTimeDescending")) {
        return { code: 0, stdout: crlf(["STATUS=ERR;auth;Response status code does not indicate success: 401 (Unauthorized)."]), stderr: "" };
      }
      return flowResult(cmd);
    },
  };
}

/** What the extension ASKED the shared sender for. pi's own `steer` keeps the
 *  severity out of the message options (it renders it through the registered
 *  severity renderer), so the honest record of "this one is an error" is the
 *  arguments the extension passes. The real sender still delivers the message. */
function mkSteerProbe(): { calls: Array<{ severity?: string; blockReason: string }>; restore: () => void } {
  const send = (globalThis as any).__steer;
  const calls: Array<{ severity?: string; blockReason: string }> = [];
  (globalThis as any).__steer = (pi: any, customType: string, content: string, blockReason: string, deliverAs: string, options?: any) => {
    calls.push({ severity: options?.severity, blockReason });
    return send(pi, customType, content, blockReason, deliverAs, options);
  };
  return { calls, restore: () => { (globalThis as any).__steer = send; } };
}

async function caseW22(): Promise<void> {
  const repo = mkRepo();
  const seams = mkAuthRun();
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  const probe = mkSteerProbe();
  try {
    await w.handle.runCommand("devexpress", "");
    await sleep(300);
  } finally {
    probe.restore();
  }
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  const steers = steersOf(w.handle);
  check("W22: the refused token is reported on the first poll", warnings.some((n) => n.includes("401")) && warnings.some((n) => n.includes("token")), JSON.stringify(warnings));
  check("W22: the refusal never reads as an empty pipeline", !toastText(w.handle).some((n) => n.includes("no build found yet")), JSON.stringify(toastText(w.handle)));
  check("W22: the give-up reaches the agent as an ERROR", steers.length === 1 && steers[0].content.includes("token") && probe.calls.length === 1 && probe.calls[0].severity === "error" && probe.calls[0].blockReason.length > 0, JSON.stringify(steers) + " | " + JSON.stringify(probe.calls));
  check("W22: watcher stopped", !isAzDoWatcherActive());
  answered(w.sel, "W22");
  noErrors(w.handle, "W22");
}

// Section: W23 — a TRANSIENT failure is retried, not fatal: only the auth kind
// ends the watch, everything else (a 503, a blip) gets the grace budget.
async function caseW23(): Promise<void> {
  const repo = mkRepo();
  const chain = [done(35760, "succeeded"), done(35780, "succeeded"), done(35790, "succeeded")];
  let polls = 0;
  const flakyRun = async (cmd: string) => {
    if (cmd.includes("queryOrder=queueTimeDescending")) {
      polls++;
      if (polls === 1) return { code: 0, stdout: crlf(["STATUS=ERR;transient;Response status code does not indicate success: 503 (Service Unavailable)."]), stderr: "" };
      return { code: 0, stdout: chain.shift() ?? done(35790, "succeeded"), stderr: "" };
    }
    return flowResult(cmd);
  };
  const w = await watcherHandle(repo, { run: flakyRun, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_DRAFT).gh, startAzDoWatcher: fastWatcher }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const toasts = toastText(w.handle);
  check("W23: the transient failure is retried and named", toasts.some((n) => n.includes("poll failed") && n.includes("503")), JSON.stringify(toasts));
  check("W23: the chain completes once the reads recover", toasts.some((n) => n.includes("chain complete")), JSON.stringify(toasts));
  check("W23: no give-up steer for a transient blip", w.handle.host.messages.length === 0, JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W23");
  noErrors(w.handle, "W23");
}

// Section: W24 — a task that has written nothing for a long time is named while
// the build is STILL inProgress, and the watch survives it: the terminal verdict
// is what the agent needs (2026-10-25, `Tests Workflow` went silent for 80
// minutes and the 120-minute deadline killed the watch 30 minutes before the
// build actually failed).
async function caseW24(): Promise<void> {
  const repo = mkRepo();
  const stale = crlf([CAPTURED_STALL, "STATUS=36111;inProgress;;"]);
  const seams = mkSeams([stale, stale, stale, done(36111, "failed")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W24: the silent task is named as hung, once", warnings.filter((n) => n.includes("Run Tests") && n.includes("hung")).length === 1, JSON.stringify(toastsOf(w.handle)));
  check("W24: the stall reaches the agent as a steer", steersOf(w.handle).some((s) => s.content.includes("Run Tests")), JSON.stringify(steersOf(w.handle)));
  check("W24: the 4-field capture reads as a TASK silence, never as frozen streams", warnings.some((n) => n.includes("Run Tests") && n.includes("hung")) && !warnings.some((n) => n.includes("no log stream has changed")), JSON.stringify(warnings));
  check("W24: the watch survives the stall and reports the verdict", warnings.some((n) => n.includes("FAILED")), JSON.stringify(warnings));
  answered(w.sel, "W24");
  noErrors(w.handle, "W24");
}

// Section: W25 — a task that keeps writing is never called hung, however long
// the build runs: progress, not elapsed time, is what says a build is alive.
async function caseW25(): Promise<void> {
  const repo = mkRepo();
  let polls = 0;
  const everyPollWrites = async (cmd: string) => {
    if (cmd.includes("$top=5")) {
      polls++;
      if (polls > 6) return { code: 0, stdout: done(36111, "failed"), stderr: "" };
      return { code: 0, stdout: runningWithTask(36111, "Tests ModelMapper", 0), stderr: "" };
    }
    return flowResult(cmd);
  };
  const w = await watcherHandle(repo, { run: everyPollWrites, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(400) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(350);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W25: the polls saw the build in flight", toastText(w.handle).some((n) => n.includes("inProgress")), JSON.stringify(toastText(w.handle)));
  check("W25: a task that keeps writing is never called hung", !warnings.some((n) => n.includes("hung")), JSON.stringify(warnings));
  answered(w.sel, "W25");
  noErrors(w.handle, "W25");
}

// Section: W26 — progress that cannot be read is NOT evidence of a stall, and
// it is not silence either: a monitor that cannot see must say so, once — the
// alternative is the blind two-hour wait the deadline's removal was meant to end.
async function caseW26(): Promise<void> {
  const repo = mkRepo();
  const blindPolls = async (cmd: string) => {
    if (cmd.includes("$top=5")) return { code: 0, stdout: running(36111), stderr: "" };
    return flowResult(cmd);
  };
  const w = await watcherHandle(repo, { run: blindPolls, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(250);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W26: a blind monitor is named ONCE, not per poll", warnings.filter((n) => n.includes("cannot be read")).length === 1, JSON.stringify(warnings));
  check("W26: blindness is never reported as a hung task", !warnings.some((n) => n.includes("hung")), JSON.stringify(warnings));
  check("W26: the watch is still alive", isAzDoWatcherActive());
  answered(w.sel, "W26");
  noErrors(w.handle, "W26");
}

// Section: W27 — the stall is reported once per episode and re-arms only when
// the task writes again, so a long silence is one message, not one per poll.
async function caseW27(): Promise<void> {
  const repo = mkRepo();
  const stale = runningWithTask(36111, "Run Tests", 130);
  let polls = 0;
  /** A "fresh" stamp built before the runner exists is already older than a
   *  test-sized stallMs, so the resume is stamped per poll, at call time. */
  const intermittentOutput = async (cmd: string) => {
    if (cmd.includes("$top=5")) {
      polls++;
      const quiet = polls <= 2 || polls >= 4;
      return { code: 0, stdout: quiet ? stale : runningWithTask(36111, "Run Tests", 0), stderr: "" };
    }
    return flowResult(cmd);
  };
  const w = await watcherHandle(repo, { run: intermittentOutput, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(400) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W27: the stall re-arms only after output resumes", warnings.filter((n) => n.includes("hung")).length === 2, JSON.stringify(warnings));
  answered(w.sel, "W27");
  noErrors(w.handle, "W27");
}

// Section: W28 — a silence UNDER the threshold is not a stall. The threshold is
// exercised from below, so a detector that warned on every in-flight poll (or
// ignored stallMs altogether) cannot pass this case.
async function caseW28(): Promise<void> {
  const repo = mkRepo();
  const tenMinutesQuiet = async (cmd: string) => {
    if (cmd.includes("$top=5")) return { code: 0, stdout: runningWithTask(36111, "Tests ModelMapper", 10), stderr: "" };
    return flowResult(cmd);
  };
  const w = await watcherHandle(repo, { run: tenMinutesQuiet, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(45 * 60_000) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(250);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W28: a silence under stallMs is not reported", warnings.length === 0, JSON.stringify(warnings));
  check("W28: the polls did run", toastText(w.handle).some((n) => n.includes("inProgress")), JSON.stringify(toastText(w.handle)));
  answered(w.sel, "W28");
  noErrors(w.handle, "W28");
}

// Section: W29 — a build whose live task opened no log of its own is named
// SILENT, not blind: a log container's stamp is real output evidence, and one
// frozen for two hours is the hang (2026-10-25, build 36112: `Build` started
// 18:57:46 with `log: null`, every container unchanged since 18:57:48, and the
// watch said it could not see the build at all).
async function caseW29(): Promise<void> {
  const repo = mkRepo();
  const frozen = runningWithWatermark(36112, "Build / Build", 130);
  let polls = 0;
  const silentTask = async (cmd: string) => {
    if (!cmd.includes("$top=5")) return flowResult(cmd);
    polls++;
    if (polls > 6) return { code: 0, stdout: done(36112, "failed"), stderr: "" };
    return { code: 0, stdout: frozen, stderr: "" };
  };
  const w = await watcherHandle(repo, { run: silentTask, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W29: a frozen log watermark is named as silence, once", warnings.filter((n) => n.includes("no log stream has changed") && n.includes("Build")).length === 1, JSON.stringify(warnings));
  check("W29: the monitor never calls this build blind", !warnings.some((n) => n.includes("cannot be read")), JSON.stringify(warnings));
  check("W29: the silence reaches the agent as a steer", steersOf(w.handle).some((s) => s.content.includes("no log stream has changed")), JSON.stringify(steersOf(w.handle)));
  answered(w.sel, "W29");
  noErrors(w.handle, "W29");
}

// Section: W30 — a QUEUED build is not a blind monitor and not a stall: the
// agent has not started it, so there is nothing to read yet. Counted as
// blindness it would report a problem that does not exist.
async function caseW30(): Promise<void> {
  const repo = mkRepo();
  const queued = async (cmd: string) => (cmd.includes("$top=5") ? { code: 0, stdout: queuedReading(36112, 5), stderr: "" } : flowResult(cmd));
  const w = await watcherHandle(repo, { run: queued, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W30: a queue younger than queueMs says nothing at all", warnings.length === 0, JSON.stringify(warnings));
  check("W30: a queued build is never reported as blind", !warnings.some((n) => n.includes("cannot be read")), JSON.stringify(warnings));
  check("W30: a queued build is never reported as stalled", !warnings.some((n) => n.includes("no log stream has changed")), JSON.stringify(warnings));
  check("W30: the watch is still alive", isAzDoWatcherActive());
  answered(w.sel, "W30");
  noErrors(w.handle, "W30");
}

// Section: W31 — a watermark that keeps moving is a build that keeps writing:
// never silent, never blind.
async function caseW31(): Promise<void> {
  const repo = mkRepo();
  let polls = 0;
  const moving = async (cmd: string) => {
    if (!cmd.includes("$top=5")) return flowResult(cmd);
    polls++;
    if (polls > 6) return { code: 0, stdout: done(36112, "failed"), stderr: "" };
    return { code: 0, stdout: runningWithWatermark(36112, "Build / Build", 0), stderr: "" };
  };
  const w = await watcherHandle(repo, { run: moving, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(400) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W31: a moving watermark is never called silent", !warnings.some((n) => n.includes("no log stream has changed")), JSON.stringify(warnings));
  check("W31: a moving watermark is never called blind", !warnings.some((n) => n.includes("cannot be read")), JSON.stringify(warnings));
  answered(w.sel, "W31");
  noErrors(w.handle, "W31");
}

// Section: W32 — a reading whose times do not parse is blindness, never a very
// old one: an unknown clock must not become evidence of a stall.
async function caseW32(): Promise<void> {
  const repo = mkRepo();
  const garbage = crlf(["PROGRESS=1;Build / Build;not-a-time;also-not-a-time;task", "STATUS=36112;inProgress;;"]);
  const blindTimes = async (cmd: string) => (cmd.includes("$top=5") ? { code: 0, stdout: garbage, stderr: "" } : flowResult(cmd));
  const w = await watcherHandle(repo, { run: blindTimes, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(250);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W32: unparseable times are reported as blindness, once", warnings.filter((n) => n.includes("cannot be read")).length === 1, JSON.stringify(warnings));
  check("W32: an unparseable time is never read as a stall", !warnings.some((n) => n.includes("has changed")), JSON.stringify(warnings));
  answered(w.sel, "W32");
  noErrors(w.handle, "W32");
}

// Section: W33 — a queued build that never gets an agent is the one state with
// nothing at all to read: without its own clock the watch would sit silent
// forever on a starved pool.
async function caseW33(): Promise<void> {
  const repo = mkRepo();
  const waiting = queuedReading(36112, 25);
  const starving = async (cmd: string) => (cmd.includes("$top=5") ? { code: 0, stdout: waiting, stderr: "" } : flowResult(cmd));
  const w = await watcherHandle(repo, { run: starving, fetchFeed: mkFetch(LAB), startAzDoWatcher: stallWatcher(50) }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  check("W33: an agent-starved queue is named once", warnings.filter((n) => n.includes("has been queued for") && n.includes("25 min")).length === 1, JSON.stringify(warnings));
  check("W33: the queue wait reaches the agent as a steer", steersOf(w.handle).some((s) => s.content.includes("no agent has picked it up")), JSON.stringify(steersOf(w.handle)));
  check("W33: a queued build is still never called blind", !warnings.some((n) => n.includes("cannot be read")), JSON.stringify(warnings));
  answered(w.sel, "W33");
  noErrors(w.handle, "W33");
}

// Section: W40 — a failing test is reported as soon as its VSTest task attempt
// publishes a result, not at the build's verdict. The pipeline retries each task
// twice and the job is `continueOnError`, so the timeline waits for the retries
// while the results API already names the failure.
async function caseW40(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [CAPTURED_TESTFAIL]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const reports = toastsOf(w.handle).filter((n) => n.type === "warning" && /(has|have) failed/.test(n.message));
  check("W40: a failed test is reported while the build still runs", reports.length === 1 && reports[0].message.includes("Synchronize_styles_when_document_modified"), JSON.stringify(toastsOf(w.handle)));
  check("W40: every captured name reaches the report", reports.length === 1 && reports[0].message.includes("Can_Get_Correct_StackTrace_From_Nested_Method") && reports[0].message.includes("Chained_Action_Helpers_Propagate_Correct_Stack_Trace"), JSON.stringify(reports));
  check("W40: the report carries the build URL", reports.length === 1 && reports[0].message.includes("definitionId=23"), JSON.stringify(reports));
  check("W40: it reaches the agent as a steer", steersOf(w.handle).some((s) => /(has|have) failed/.test(s.content) && s.deliverAs === "steer"), JSON.stringify(steersOf(w.handle)));
  check("W40: the watch is still running", isAzDoWatcherActive());
  stopAzDoWatcher();
  answered(w.sel, "W40");
  noErrors(w.handle, "W40");
}

// Section: W41 — the same failing test on every poll is ONE message
async function caseW41(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"]), testFails(["Xpand.Tests.Alpha"]), testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(350);
  const reports = toastsOf(w.handle).filter((n) => n.message.includes("a test has failed"));
  check("W41: the same failing test is reported once", reports.length === 1, JSON.stringify(reports));
  check("W41: the prompt was raised once", w.sel.asks.filter((t) => t.includes("Tests failing")).length === 1, JSON.stringify(w.sel.asks));
  stopAzDoWatcher();
  answered(w.sel, "W41");
  noErrors(w.handle, "W41");
}

// Section: W42 — a NEW name later is one more steer, naming only the new one
async function caseW42(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"]), testFails(["Xpand.Tests.Alpha", "Xpand.Tests.Omega"])]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(350);
  const reports = toastsOf(w.handle).filter((n) => n.message.includes("a test has failed"));
  const second = reports[1]?.message ?? "";
  check("W42: a new failing test is its own report", reports.length === 2, JSON.stringify(reports));
  check("W42: the second report names only the new test", second.includes("Xpand.Tests.Omega") && !second.includes("Xpand.Tests.Alpha"), second);
  check("W42: a new name raises no second prompt", w.sel.asks.filter((t) => t.includes("Tests failing")).length === 1, JSON.stringify(w.sel.asks));
  stopAzDoWatcher();
  answered(w.sel, "W42");
  noErrors(w.handle, "W42");
}

// Section: W43 — one prompt per build, and its option names the role
async function caseW43(): Promise<void> {
  const repo = mkRepo();
  const prompts: Array<{ title: string; options: string[] }> = [];
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"]), testFails(["Xpand.Tests.Omega"]), testFails(["Xpand.Tests.Sigma"])]);
  const w = await watcherHandle(
    repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS,
    (title: string, options: string[]) => prompts.push({ title, options }),
  );
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const asks = prompts.filter((p) => p.title.includes("Tests failing"));
  check("W43: exactly one delegation prompt per build", asks.length === 1, JSON.stringify(prompts.map((p) => p.title)));
  check("W43: the prompt offers the Test-Runner role", asks.length === 1 && asks[0].options.some((o) => o.includes("Delegate to Test-Runner")), JSON.stringify(asks));
  stopAzDoWatcher();
  answered(w.sel, "W43");
  noErrors(w.handle, "W43");
}

// Section: W44 — accepting it boots the window as the role, with the brief
async function caseW44(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": TEST_ROLE_ENTRY });
  const spawns: Array<{ task: string; launch: any }> = [];
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: testWatcher(async (_repo: string, task: string, launch: any) => {
      spawns.push({ task, launch });
      return "42";
    }),
  }, { ...PUBLISH_LAB, "Tests failing": "Delegate to Test-Runner" });
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W44: accepting spawns one window", spawns.length === 1, JSON.stringify(spawns.length));
  check("W44: the launch is the role's", spawns[0]?.launch?.provider === "deepseek" && spawns[0]?.launch?.model === "deepseek-flash" && spawns[0]?.launch?.thinkingLevel === "max", JSON.stringify(spawns[0]?.launch));
  check("W44: the brief names the failing test", (spawns[0]?.task ?? "").includes("Xpand.Tests.Alpha"), spawns[0]?.task ?? "");
  const brief = spawns[0]?.task ?? "";
  check("W44: the brief carries the narrowed run", brief.includes("dotnet test src\\Tests\\Tests.sln") && brief.includes('--filter "FullyQualifiedName~Xpand.Tests.Alpha"'), brief);
  check("W44: the window is reported", toastsOf(w.handle).some((n) => n.message.includes("window 42")), JSON.stringify(toastText(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W44");
  noErrors(w.handle, "W44");
}

// Section: W45 — a published store WITHOUT the role: no window, and the role is
// NAMED. The store is empty, published the way pi publishes it.
async function caseW45(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({});
  let spawned = 0;
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: testWatcher(async () => {
      spawned++;
      return "43";
    }),
  }, { ...PUBLISH_LAB, "Tests failing": "Delegate to Test-Runner" });
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W45: a missing role spawns nothing", spawned === 0, String(spawned));
  check("W45: the missing role is named", steersOf(w.handle).some((s) => s.content.includes("no Test-Runner role")), JSON.stringify(steersOf(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W45");
  noErrors(w.handle, "W45");
}

// Section: W46 — a spawn that yields no window: the command to run by hand
async function caseW46(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": TEST_ROLE_ENTRY });
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: testWatcher(async () => null),
  }, { ...PUBLISH_LAB, "Tests failing": "Delegate to Test-Runner" });
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const steers = steersOf(w.handle).map((s) => s.content).join("\n");
  check("W46: no window is reported as no window", toastsOf(w.handle).some((n) => n.message.includes("no delegate window could be opened")), JSON.stringify(toastText(w.handle)));
  check("W46: the exact command is handed over", steers.includes("pi --provider deepseek --model deepseek-flash --thinking max"), steers);
  check("W46: nothing claims a delegation happened", !steers.includes("delegated the failing tests"), steers);
  stopAzDoWatcher();
  answered(w.sel, "W46");
  noErrors(w.handle, "W46");
}

// Section: W47 — the failure does not end the watch: the chain still runs to its
// own verdict. The prologue is W1's whole green chain, with a failing test on
// the first in-flight poll, because the publish flow follows all three legs.
async function caseW47(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": TEST_ROLE_ENTRY });
  const gh = mkGh(GH_DRAFT);
  const seams = mkSeams([running(35760), ...GREEN], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: gh.gh, startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(600);
  check("W47: the failing test was reported", toastsOf(w.handle).some((n) => n.message.includes("a test has failed")), JSON.stringify(toastText(w.handle)));
  check("W47: the chain's verdict still arrived", toastsOf(w.handle).some((n) => n.message.includes("chain complete")), JSON.stringify(toastText(w.handle)));
  check("W47: the watcher ended on its own verdict", !isAzDoWatcherActive());
  answered(w.sel, "W47");
  noErrors(w.handle, "W47");
}

// Section: W48 — the scan is asked for what MOVED, and keeps what it was told
async function caseW48(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"], "2026-10-26T08:00:00Z")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(350);
  const scans = seams.calls.filter((c) => c.includes("# rxaf-testfail"));
  check("W48: the first scan starts with no watermark", scans.length >= 2 && scans[0].includes("$since = ''"), JSON.stringify(scans.slice(0, 2)));
  check("W48: the next scan passes back what the script reported", scans.length >= 2 && scans[1].includes("$since = '2026-10-26T08:00:00Z'"), JSON.stringify(scans.slice(0, 2)));
  stopAzDoWatcher();
  answered(w.sel, "W48");
  noErrors(w.handle, "W48");
}

// Section: W49 — "Don't ask again this run" silences the ASK, not the report
async function caseW49(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": TEST_ROLE_ENTRY });
  const prompts: string[] = [];
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"]), testFails(["Xpand.Tests.Omega"])]);
  const w = await watcherHandle(
    repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher },
    { ...PUBLISH_LAB, "Tests failing": "Don't ask again this run" },
    (title: string) => prompts.push(title),
  );
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W49: muted after the first answer", prompts.filter((t) => t.includes("Tests failing")).length === 1, JSON.stringify(prompts));
  check("W49: the next failure is still reported", toastsOf(w.handle).some((n) => n.message.includes("Xpand.Tests.Omega")), JSON.stringify(toastText(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W49");
  noErrors(w.handle, "W49");
}

// Section: W50 — the launch FOLLOWS the published store. The store carries a
// provider/model no case writes as a literal anywhere else, so a hardcoded
// deepseek default cannot satisfy this: it is the role-name -> flags lookup the
// operator's /model-role assignment relies on.
async function caseW50(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": { role: "Test-Runner", provider: "openrouter", modelId: "moonshotai/kimi-k3", thinkingLevel: "high" } });
  const spawns: Array<{ launch: any }> = [];
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: testWatcher(async (_repo: string, _task: string, launch: any) => {
      spawns.push({ launch });
      return "50";
    }),
  }, { ...PUBLISH_LAB, "Tests failing": "Delegate to Test-Runner" });
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  const launch = spawns[0]?.launch ?? {};
  check("W50: the role's provider reaches the launch", launch.provider === "openrouter", JSON.stringify(launch));
  check("W50: the role's model reaches the launch", launch.model === "moonshotai/kimi-k3", JSON.stringify(launch));
  check("W50: the role's level reaches the launch", launch.thinkingLevel === "high", JSON.stringify(launch));
  stopAzDoWatcher();
  answered(w.sel, "W50");
  noErrors(w.handle, "W50");
}

// Section: W52 — a published entry with no modelId is NOT a role: no launch is
// invented, and the same refusal as a missing role is spoken.
async function caseW52(): Promise<void> {
  const repo = mkRepo();
  await publishRoles({ "Test-Runner": { role: "Test-Runner", provider: "deepseek" } });
  let spawned = 0;
  const seams = mkSeams([running(35760)], [testFails(["Xpand.Tests.Alpha"])]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: testWatcher(async () => {
      spawned++;
      return "52";
    }),
  }, { ...PUBLISH_LAB, "Tests failing": "Delegate to Test-Runner" });
  await w.handle.runCommand("devexpress", "");
  await sleep(400);
  check("W52: an entry without a model resolves to no launch", spawned === 0, String(spawned));
  check("W52: the role is named as unset", steersOf(w.handle).some((s) => s.content.includes("no Test-Runner role")), JSON.stringify(steersOf(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W52");
  noErrors(w.handle, "W52");
}

// Section: W53 — the script the scan actually SENDS. It used to scrape build
// logs, and on build 36120 it read a PASSING job's "Failed to determine the
// https port for redirect." warning as a failed test. These read the emitted
// pwsh text, so a field reorder or a wrong call fails HERE, not in production.
function checkTestFailScript(scan: string): void {
  check("W53: the scan asks for this build's test runs", scan.includes("Get-AzTestRuns -buildIds 35760"), scan.slice(0, 240));
  check("W53: the scan asks for the failed results", scan.includes("test/Runs/") && scan.includes("Results?outcomes=Failed"), scan.slice(0, 240));
  check("W53: the script prints run id, then the run stamp, then the name", scan.includes('"TESTLINE=$($run.id);$stamp;" + ($n -replace'), scan.slice(0, 400));
  check("W53: the script prints what it looked at", scan.includes('"TESTRUNS=$($runs.Count);$broken"'), scan.slice(0, 400));
  check("W53: the watermark is the RUN stamp, not a result time", scan.includes("if ($since -and $stamp -le $since) { continue }"), scan.slice(0, 400));
  check("W53: only runs carrying a failure are opened", scan.includes("if ($failed -le 0) { continue }"), scan.slice(0, 400));
  check("W53: no build log is fetched for tests", !scan.includes("/logs") && !scan.includes("build/builds/"), scan.slice(0, 240));
}

async function caseW53(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [CAPTURED_TESTFAIL]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const scans = seams.calls.filter((c) => c.includes("# rxaf-testfail"));
  check("W53: the test scan ran", scans.length >= 1, String(scans.length));
  checkTestFailScript(scans[0] ?? "");
  stopAzDoWatcher();
  answered(w.sel, "W53");
  noErrors(w.handle, "W53");
}

// Section: W54 — the pipeline retries a VSTest task twice, so the same test
// fails again under a NEW run id. One name is one report, whoever published it.
async function caseW54(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [
    renderTestScan(["Xpand.Tests.Alpha"], "2026-10-26T08:00:00Z", { run: "1023520" }),
    renderTestScan(["Xpand.Tests.Alpha"], "2026-10-26T08:10:00Z", { run: "1023530" }),
  ]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const reports = toastsOf(w.handle).filter((n) => n.message.includes("a test has failed"));
  check("W54: a retried attempt failing the same test is one report", reports.length === 1, JSON.stringify(reports));
  stopAzDoWatcher();
  answered(w.sel, "W54");
  noErrors(w.handle, "W54");
}

// Section: W55 — a scan that found no failed result says nothing at all: the
// watermark is the only payload a green build has, and it is not a report.
async function caseW55(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [CAPTURED_EMPTY]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  check("W55: a green scan reports nothing", !toastsOf(w.handle).some((n) => n.message.includes("a test has failed")), JSON.stringify(toastsOf(w.handle)));
  check("W55: the watch keeps running", isAzDoWatcherActive());
  stopAzDoWatcher();
  answered(w.sel, "W55");
  noErrors(w.handle, "W55");
}

// Section: W56 — a refused scan is ONE classified note, never silence and never
// a failure: a dead token must not read as a green build, and a read that failed
// must not read as a broken test.
async function caseW56(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [renderTestRefusal("auth", "401 Unauthorized")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  const notes = toastsOf(w.handle).filter((n) => n.message.includes("the test scan could not be read"));
  check("W56: a refused scan is said once", notes.length === 1, JSON.stringify(notes));
  check("W56: the note carries the reason", notes.length === 1 && notes[0].message.includes("401"), JSON.stringify(notes));
  check("W56: a refused scan is not a failed test", !toastsOf(w.handle).some((n) => n.message.includes("a test has failed")), JSON.stringify(toastsOf(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W56");
  noErrors(w.handle, "W56");
}

// Section: W57 — a failed result carrying no name says nothing: an empty bullet
// is a report the agent cannot act on.
async function caseW57(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([running(35760)], [renderTestScan([""], "2026-10-26T08:00:00Z")]);
  const w = await watcherHandle(repo, { run: seams.run, fetchFeed: mkFetch(LAB), startAzDoWatcher: fastWatcher }, KEEP_TESTS);
  await w.handle.runCommand("devexpress", "");
  await sleep(300);
  check("W57: a nameless failed result is not reported", !toastsOf(w.handle).some((n) => n.message.includes("a test has failed")), JSON.stringify(toastsOf(w.handle)));
  check("W57: it never reaches the agent as a steer", !steersOf(w.handle).some((s) => s.content.includes("a test has failed")), JSON.stringify(steersOf(w.handle)));
  stopAzDoWatcher();
  answered(w.sel, "W57");
  noErrors(w.handle, "W57");
}

// Section: W58 — an empty answer is FALSIFIABLE. The scan records what it looked
// at, so "no failed test" and "the scan reached nothing" stop being the same
// statement: build 36120's silence was read as a verdict it never made.
async function caseW58(): Promise<void> {
  const io = {
    buildId: 36120, definition: "23", role: "Test-Runner",
    notify: async () => {}, steer: () => {}, select: async () => undefined,
    ended: () => false, spawn: async () => null,
  };
  const looked = newTestWatch();
  await testScan(looked, io, async () => ({ stdout: crlf(["TESTRUNS=25;0", "TESTSEEN=2026-10-26T06:13:54Z"]) }));
  check("W58: the scan records the runs it looked at", looked.runs === 25, String(looked.runs));
  check("W58: the scan records that none carried a failure", looked.failedRuns === 0, String(looked.failedRuns));
  check("W58: a green scan still says nothing", looked.seen.size === 0, String(looked.seen.size));
  const broken = newTestWatch();
  await testScan(broken, io, async () => ({ stdout: renderTestScan(["Xpand.Tests.Alpha"], "2026-10-26T09:00:00Z", { runs: 24, failedRuns: 3 }) }));
  check("W58: a broken scan records both counts", broken.runs === 24 && broken.failedRuns === 3, `${broken.runs}/${broken.failedRuns}`);
  const blind = newTestWatch();
  await testScan(blind, io, async () => ({ stdout: renderTestRefusal("auth", "401 Unauthorized") }));
  check("W58: a refused scan records no count", blind.runs === null && blind.failedRuns === null, `${blind.runs}/${blind.failedRuns}`);
}

// Section: W59 — nuget.org validates a pushed batch before it commits it to its
// catalog, and that wait is minutes: the 2026-10-26 Release pushed 56 packages at
// 18:49 and the last one surfaced at 19:07. The SHIPPED attempt count governs
// this case — only the delay between tries is narrowed — so a budget back at ten
// fails it while the assert as shipped still confirms.
async function caseW59(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const lag = { calls: 0 };
  const gh = mkGh(GH_DRAFT);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkLaggingFetch(12, lag), ghFetch: gh.gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetryMs: 5 }),
  }, PUBLISH_RELEASE);
  try {
    await w.handle.runCommand("devexpress", "");
    await sleep(1500);
  } finally {
    stopAzDoWatcher();
  }
  const toasts = toastText(w.handle);
  check("W59: index lag past the old ten tries is still confirmed", lag.calls > 12 && toasts.some((n) => n.includes("Nugets published") && n.includes("nuget.org")), JSON.stringify(toasts));
  check("W59: a confirmed assert neither warns nor steers", !toasts.some((n) => n.includes("NOT confirmed")) && w.handle.host.messages.length === 0, JSON.stringify(toasts) + " | " + JSON.stringify(w.handle.host.messages));
  answered(w.sel, "W59");
  noErrors(w.handle, "W59");
}

/** W60: the wider window must not become an infinite one — a package that never
 *  reaches the index still ends the assert with a warning and a steer. */
async function caseW60(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  const lag = { calls: 0 };
  const gh = mkGh(GH_DRAFT);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkLaggingFetch(Infinity, lag), ghFetch: gh.gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetryMs: 5 }),
  }, PUBLISH_RELEASE);
  try {
    await w.handle.runCommand("devexpress", "");
    await sleep(2500);
  } finally {
    stopAzDoWatcher();
  }
  const warnings = toastsOf(w.handle).filter((n) => n.type === "warning").map((n) => n.message);
  const steers = steersOf(w.handle);
  check("W60: every shipped attempt is spent before the give-up", lag.calls >= 30, String(lag.calls));
  check("W60: a package that never lands still warns and steers", warnings.some((n) => n.includes("Nugets NOT confirmed") && n.includes("nuget.org")) && steers.some((s) => s.content.includes("Nugets NOT confirmed")), JSON.stringify({ warnings, steers }));
  answered(w.sel, "W60");
  noErrors(w.handle, "W60");
}

/** The contract, in the order it runs. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["W1", caseW1],
  ["W2", caseW2],
  ["W3", caseW3],
  ["W4", caseW4],
  ["W5", caseW5],
  ["W6", caseW6],
  ["W7", caseW7],
  ["W8", caseW8],
  ["W9", caseW9],
  ["W10", caseW10],
  ["W11", caseW11],
  ["W12", caseW12],
  ["W13", caseW13],
  ["W14", caseW14],
  ["W15", caseW15],
  ["W16", caseW16],
  ["W17", caseW17],
  ["W18", caseW18],
  ["W19", caseW19],
  ["W20", caseW20],
  ["W21", caseW21],
  ["W22", caseW22],
  ["W23", caseW23],
  ["W24", caseW24],
  ["W25", caseW25],
  ["W26", caseW26],
  ["W27", caseW27],
  ["W28", caseW28],
  ["W29", caseW29],
  ["W30", caseW30],
  ["W31", caseW31],
  ["W32", caseW32],
  ["W33", caseW33],
  ["W40", caseW40],
  ["W41", caseW41],
  ["W42", caseW42],
  ["W43", caseW43],
  ["W44", caseW44],
  ["W45", caseW45],
  ["W46", caseW46],
  ["W47", caseW47],
  ["W48", caseW48],
  ["W49", caseW49],
  ["W50", caseW50],
  ["W52", caseW52],
  ["W53", caseW53],
  ["W54", caseW54],
  ["W55", caseW55],
  ["W56", caseW56],
  ["W57", caseW57],
  ["W58", caseW58],
  ["W59", caseW59],
  ["W60", caseW60],
];

/** One case's failure is reported and counted, never a crash that hides the
 *  rest of the contract. */
async function runCase(label: string, body: () => Promise<void>): Promise<void> {
  try {
    await body();
  } catch (e) {
    fail++;
    console.log(`FAIL  ${label} crashed: ${(e as Error)?.stack ?? e}`);
  }
}

async function main(): Promise<void> {
  console.log("reactive-xaf-build/watcher-tests — pi's own runtime, no fake\n");
  for (const [label, body] of CASES) await runCase(label, body);
  stopAzDoWatcher();
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
