/**
 * reactive-xaf-build/profile-tests — the RepoProfile contract (P0-P4).
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (commit 93e1a4754): `./resolve.mjs` installs
 * the `@pi/` name floor first, and the harness plus the seam-owning modules are
 * DYNAMIC imports after it. This suite imports no boot proof, because nothing
 * here spawns.
 *
 * The flows are driven through pi's own command dispatch (Build | Last build
 * status → RX-XAF | eXpand → Lab | Release) with the extension's OWN ports
 * injected: the command runner, the feed fetcher and the pane seams, so the real
 * nuget.org, pwsh, psmux and the AzDO API are never touched. The flow's report is
 * asserted where the user meets it (`handle.host.notices`), because pi discards
 * a command handler's return value.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/profile-tests.ts
 */
// test-timeout: 120000 — one runner build per case, plus the run watch's 10 ms cadence
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
/** Local delay: the test harness owns its own clock. */
const sleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
import { stopBuildRun } from "./run.js";
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
 *  and the seam-owning modules BY NAME. menu-tests.ts's installRoute is the same
 *  shape with one difference — it also imports boot-proof and hands that file's
 *  ledger proof back; this suite spawns nothing, so it does not. The extension
 *  reads `__steer` (llm-utils) and `__writeFileSync` (tracked-write) off
 *  globalThis, so the real modules have to publish them. Idempotent: the hook is
 *  a process-wide registration. */
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
 *  own select, so `sel.asks` is the record of what the flow asked. */
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

/** What a case hands its own assertion halves: the fixture, the ports, the picks
 *  it answered and the host it drove. */
interface Parts {
  repo: string;
  runner: any;
  pane: any;
  sel: CaseUi;
  handle: any;
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
async function waitFor(cond: () => boolean, ms = 4000): Promise<boolean> {
  const deadline = Date.now() + ms;
  while (Date.now() < deadline) {
    if (cond()) return true;
    await sleep(20);
  }
  return cond();
}
/** Finish a started run the way the supervisor would. */
function finishRun(pane: any, code: number): void {
  const script = /-File "([^"]+)"/.exec(pane.sent[0])?.[1] ?? "";
  writeFileSync(script.replace(/run\.ps1$/, "exit.code"), String(code));
}
function mkPane(): any {
  const sent: string[] = [];
  return {
    openBuildPane: async () => "pane1",
    runInPane: async (_pane: string, cmd: string) => { sent.push(cmd); },
    capturePane: async () => "",
    probePane: async () => ({ alive: true, pid: 4242 }),
    // The CPU sampler is a seam like any other: without it the run falls back to
    // the real host sampler, whose call never settles here, and the watch's
    // first tick wedges with `polling` still true.
    sampleCpu: async () => 1,
    runCadence: { signalMs: 10, probeMs: 10, cpuMs: 10, stallMs: 3_600_000, overrunMs: 3_600_000 },
    closePane: async () => {},
    startAzDoWatcher: () => ({ stop: () => {}, active: () => false, lastBuildId: () => null }),
    sent,
  };
}
function mkRxRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-prof-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
function mkExpandRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "xpand-prof-"));
  tempDirs.push(root);
  mkdirSync(join(root, "Xpand", "Xpand.ExpressApp.Modules"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const okResult = (stdout = ""): any => ({ code: 0, stdout, stderr: "" });
const GREEN_RX = [
  { match: VM_PREFIX, result: okResult(VM_RUN) },
  { match: VM_PREFIX, result: okResult(VM_RUN) },
  { match: "git status --short", result: okResult("") },
  { match: "prx", result: okResult("") },
];
const GREEN_EXPAND = [
  { match: VM_PREFIX, result: okResult(VM_RUN) },
  { match: VM_PREFIX, result: okResult(VM_RUN) },
  { match: "git status --short", result: okResult("") },
  { match: "git push lab HEAD:master", result: okResult("") },
  { match: "px", result: okResult("") },
];
/** The picks a profile case answers: its project, then the queue confirm. */
const RX_PICKS: Record<string, Answer> = { DevExpress: "Build", Project: "RX-XAF", "RX-XAF": "Lab", "Publish: ": "Publish" };
const EXPAND_PICKS: Record<string, Answer> = { DevExpress: "Build", Project: "eXpand", eXpand: "Lab", "Publish: ": "Publish" };
/** pi's own runtime for one case: the real command, the case's picks and ports. */
async function profileHandle(
  repo: string, seams: Record<string, unknown>, picks: Record<string, Answer>,
): Promise<{ handle: any; sel: CaseUi }> {
  const sel = uiFor(picks);
  await installRoute();
  const handle = await buildRealRunner({
    cwd: repo,
    ui: sel.ui,
    activate: (pi: any) => registerBuildCommand(pi, { repoRoot: repo, ...seams }),
  });
  handles.push(handle);
  return { handle, sel };
}

// Section: P0 — the command registers through the real index boot
async function caseP0(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("P0: devexpress registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "P0");
}

// Section: P1 — the RX profile loud-rejects a foreign tree
async function caseP1(): Promise<void> {
  const other = mkdtempSync(join(tmpdir(), "neither-"));
  tempDirs.push(other);
  const runner = mkRunner([]);
  const picks: Record<string, Answer> = { DevExpress: "Build", Project: "RX-XAF", "RX-XAF": "Lab" };
  const w = await profileHandle(other, { run: runner.run, fetchFeed: async () => "[]" }, picks);
  await w.handle.runCommand("devexpress", "");
  // The refusal itself is return-only (build.ts missingRepo), so the case pins
  // the behavior it protects: nothing runs and no pane opens.
  check("P1: RX loud-rejects foreign tree, zero commands", runner.calls.length === 0, runner.calls.join(" | "));
  answered(w.sel, "P1");
  noErrors(w.handle, "P1");
}

// Section: P2 — the expand profile drives its own menu
async function caseP2(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandRepo();
  const runner = mkRunner(GREEN_EXPAND);
  const pane = mkPane();
  const seams = { run: runner.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), propsPath: join(repo, "Directory.Packages.props"), profile: expandProfile, pollMs: 1, ...pane };
  const w = await profileHandle(repo, seams, EXPAND_PICKS);
  await w.handle.runCommand("devexpress", "");
  check("P2: menu offered Project pick", w.sel.asks.includes("Project"), w.sel.asks.join(" | "));
  check("P2: the pane got the supervisor script running bx lab", pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(pane.sent[0]) && noticesOf(w.handle).some((n) => n.includes("Build started in pane")), JSON.stringify(pane.sent) + " " + noticesOf(w.handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => noticesOf(w.handle).some((n) => n.includes("published")));
  checkP2({ repo, runner, pane, sel: w.sel, handle: w.handle });
  answered(w.sel, "P2");
  noErrors(w.handle, "P2");
}

/** P2's tail: the expand publish half ran in order, and the flow reported it. */
function checkP2(p: Parts): void {
  const push = p.runner.calls.indexOf("git push lab HEAD:master");
  const queue = p.runner.calls.indexOf("px");
  check("P2: git push lab then px, published", noticesOf(p.handle).some((n) => n.includes("published")) && push >= 0 && queue > push, p.runner.calls.join(" | ") + " || " + noticesOf(p.handle).join(" | "));
}

// Section: P3 — the status pick queries the profile's own definition
async function caseP3(): Promise<void> {
  const expand = mkExpandRepo();
  const rx = mkRxRepo();
  const expCalls: string[] = [];
  const rxCalls: string[] = [];
  const statusRun = (sink: string[]) => async (cmd: string) => {
    sink.push(cmd);
    return okResult("STATUS=1;completed;succeeded;");
  };
  const exp = await profileHandle(expand, { run: statusRun(expCalls), fetchFeed: async () => "[]", profile: expandProfile }, { DevExpress: "Last build status" });
  await exp.handle.runCommand("devexpress", "");
  const rxd = await profileHandle(rx, { run: statusRun(rxCalls), fetchFeed: async () => "[]" }, { DevExpress: "Last build status" });
  await rxd.handle.runCommand("devexpress", "");
  check("P3: expand status queries def 94", expCalls.some((c) => c.includes("definitions=94")), JSON.stringify(expCalls));
  check("P3: RX status still queries def 23", rxCalls.some((c) => c.includes("definitions=23")), JSON.stringify(rxCalls));
  answered(exp.sel, "P3-expand");
  answered(rxd.sel, "P3-rx");
  noErrors(exp.handle, "P3-expand");
  noErrors(rxd.handle, "P3-rx");
}

// Section: P4 — the default profile is unchanged: brx in the pane, prx after
async function caseP4(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo();
  const runner = mkRunner(GREEN_RX);
  const pane = mkPane();
  const seams = { run: runner.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), propsPath: join(repo, "Directory.Packages.props"), pollMs: 1, ...pane };
  const w = await profileHandle(repo, seams, RX_PICKS);
  await w.handle.runCommand("devexpress", "");
  check("P4: RX still starts brx in a pane", /-File ".*run\.ps1"$/.test(pane.sent[0] ?? "") && noticesOf(w.handle).some((n) => n.includes("Build started in pane")), JSON.stringify(pane.sent) + " | " + noticesOf(w.handle).join(" | "));
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("prx"));
  check("P4: prx still ran", runner.calls.includes("prx"), runner.calls.join(" | "));
  answered(w.sel, "P4");
  noErrors(w.handle, "P4");
}

/** The contract, in the order it runs. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["P0", caseP0],
  ["P1", caseP1],
  ["P2", caseP2],
  ["P3", caseP3],
  ["P4", caseP4],
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
  console.log("reactive-xaf-build/profile-tests — pi's own runtime, no fake\n");
  for (const [label, run] of CASES) await runCase(label, run);
  stopBuildRun();
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
