/**
 * reactive-xaf-build/azdo-tests — the AzDO status and cancel parse contract
 * (T1-T8), against REAL pwsh-shaped output (CRLF line endings).
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (commit 93e1a4754): `./resolve.mjs` installs
 * the `@pi/` name floor first, and the harness plus the seam-owning modules are
 * DYNAMIC imports after it. This suite imports no boot proof, because nothing
 * here spawns.
 *
 * The two menu items ("Last build status", "Cancel AzDO build") are driven
 * through pi's own command dispatch with the extension's OWN ports injected, and
 * the run seam answers each case's CRLF fixture, so the real AzDO API and pwsh
 * are never touched. `status.ts` reports through `ctx.ui.notify` and steers
 * nothing, so a case asserts the toasts (`handle.host.notices`) — pi discards a
 * command handler's return value.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/azdo-tests.ts
 */
// test-timeout: 60000 — one runner build per case, no run watch
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";

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
 *  LONGEST match first. A title the case did not map, or an answer that is not
 *  one of the offered options, is RECORDED — the harness would otherwise answer
 *  with the first option — and every case asserts the list is empty. A case that
 *  supplies this override REPLACES the harness's own select, so `sel.asks` is
 *  the record of what the flow asked. */
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

/** The menu picks: both capabilities are top-level items, so no project pick
 *  follows them. */
const STATUS_PICKS: Record<string, Answer> = { DevExpress: "Last build status" };
const CANCEL_PICKS: Record<string, Answer> = { DevExpress: "Cancel AzDO build" };

function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-azdo-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  return root;
}
function crlf(lines: string[]): string {
  return lines.join("\r\n") + "\r\n";
}
/** The command runner for a parse case: EVERY call answers the same stdout, and
 *  the calls are recorded so a case can pin the query the phase built. */
function mkRun(stdout: string): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const calls: string[] = [];
  return {
    calls,
    run: async (cmd: string) => {
      calls.push(cmd);
      return { code: 0, stdout, stderr: "" };
    },
  };
}
/** pi's own runtime for one case: the real command, the case's stdout fixture
 *  over the run seam, and the picks it answers. */
async function azdoHandle(
  repo: string, runner: { run: (cmd: string) => Promise<any> }, picks: Record<string, Answer>,
): Promise<{ handle: any; sel: CaseUi }> {
  const sel = uiFor(picks);
  await installRoute();
  const handle = await buildRealRunner({
    cwd: repo,
    ui: sel.ui,
    activate: (pi: any) => registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }), repoRoot: repo }),
  });
  handles.push(handle);
  return { handle, sel };
}

// Section: T1 — the status pick surfaces the id and the extracted reason
async function caseT1(): Promise<void> {
  const repo = mkRepo();
  const logLines = ["Executing Compile", "CSC : error DX1003: Expired license key version", "##[error]PowerShell exited with code '1'"];
  const runner = mkRun(crlf(["LOGSTART", ...logLines, "LOGEND", "STATUS=35735;completed;failed;"]));
  const w = await azdoHandle(repo, runner, STATUS_PICKS);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("T1: status surfaces id + extracted reason from CRLF", report.includes("35735") && report.includes("DX1003"), report);
  answered(w.sel, "T1");
  noErrors(w.handle, "T1");
}

// Section: T2 — a cancel request reports the count
async function caseT2(): Promise<void> {
  const repo = mkRepo();
  const w = await azdoHandle(repo, mkRun(crlf(["CANCEL=35735;ok;3"])), CANCEL_PICKS);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("T2: cancel requested for all builds surfaced from CRLF", report.includes("Cancel requested for 3 AzDO builds"), report);
  answered(w.sel, "T2");
  noErrors(w.handle, "T2");
}

// Section: T3 — a cancel with nothing to do says so
async function caseT3(): Promise<void> {
  const repo = mkRepo();
  const w = await azdoHandle(repo, mkRun(crlf(["CANCEL=0;none;none"])), CANCEL_PICKS);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("T3: no-builds cancel surfaced", report.includes("No AzDO builds found to cancel"), report);
  answered(w.sel, "T3");
  noErrors(w.handle, "T3");
}

// Section: T4 — the command registers through the real index boot
async function caseT4(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("T4: devexpress command registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "T4");
}

// Section: T5 — the plain status pick keeps the profile's definition
async function caseT5(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRun(crlf(["STATUS=35735;completed;failed;"]));
  const w = await azdoHandle(repo, runner, STATUS_PICKS);
  await w.handle.runCommand("devexpress", "");
  check("T5: plain status keeps def 23", runner.calls.some((c) => c.includes("definitions=23")), JSON.stringify(runner.calls));
  answered(w.sel, "T5");
  noErrors(w.handle, "T5");
}

// Section: T6 — cancel is project-wide
async function caseT6(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRun(crlf(["CANCEL=0;none;none"]));
  const w = await azdoHandle(repo, runner, CANCEL_PICKS);
  await w.handle.runCommand("devexpress", "");
  check("T6: cancel queries all builds project-wide", runner.calls.some((c) => c.includes("statusFilter=inProgress,notStarted,postponed") && !c.includes("definitions=")), JSON.stringify(runner.calls));
  answered(w.sel, "T6");
  noErrors(w.handle, "T6");
}

// Section: T7 — the status log block targets the failed Task record
async function caseT7(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRun(crlf(["STATUS=35797;completed;failed;"]));
  const w = await azdoHandle(repo, runner, STATUS_PICKS);
  await w.handle.runCommand("devexpress", "");
  check("T7: status log block filters failed Task records", runner.calls.some((c) => c.includes('$_.type -eq "Task" -and $_.result -eq "failed"')), JSON.stringify(runner.calls));
  answered(w.sel, "T7");
  noErrors(w.handle, "T7");
}

// Section: T8 — a 5-field STATUS still surfaces the log's own reason
async function caseT8(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRun(crlf(["LOGSTART", "##[error]Release 26.1.301.1 exists", "LOGEND", "STATUS=35802;completed;failed;26.1.301.1;"]));
  const w = await azdoHandle(repo, runner, STATUS_PICKS);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("T8: 5-field STATUS surfaces id + log reason", report.includes("35802") && report.includes("Release 26.1.301.1 exists"), report);
  answered(w.sel, "T8");
  noErrors(w.handle, "T8");
}

/** The contract, in the order it runs. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["T1", caseT1],
  ["T2", caseT2],
  ["T3", caseT3],
  ["T4", caseT4],
  ["T5", caseT5],
  ["T6", caseT6],
  ["T7", caseT7],
  ["T8", caseT8],
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
  console.log("reactive-xaf-build/azdo-tests — pi's own runtime, no fake\n");
  for (const [label, run] of CASES) await runCase(label, run);
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
