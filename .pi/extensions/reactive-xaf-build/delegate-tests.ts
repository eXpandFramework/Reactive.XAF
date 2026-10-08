/**
 * reactive-xaf-build/delegate-tests — the delegation module's contract
 * (S0-S3), plus the flow publishing in the invoking session.
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (commit 93e1a4754): `./resolve.mjs` installs
 * the `@pi/` name floor first, and the harness plus the seam-owning modules are
 * DYNAMIC imports after it. This suite imports no boot proof, because nothing
 * here spawns.
 *
 * `delegateWindow` is dormant: the /devexpress flow runs in the invoking session
 * and never consults it. What stays pinned here is the helper's OWN liveness
 * contract — the real `defaultDelegateWindow` with injected `DelegateDeps`
 * (`run`/`windowExists`/`killWindow`/`graceMs`), so the real psmux CLI is never
 * touched — and the flow publishing locally through pi's own dispatch.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/delegate-tests.ts
 */
// test-timeout: 60000 — one runner build per pi-driven case, no run watch
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
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
 *  LONGEST match first. A title the case did not map, or an answer that is not
 *  one of the offered options, is RECORDED — the harness would otherwise answer
 *  with the first option — and every case asserts the list is empty. */
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
    capturePane: async () => "",
    closePane: async () => {},
  };
}
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-delegate-"));
  tempDirs.push(root);
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
const PUBLISH_PICKS: Record<string, Answer> = { DevExpress: "Publish", Project: "RX-XAF", "RX-XAF": "Lab", "Publish: ": "Publish" };
/** pi's own runtime for one case: the real command, the case's picks and ports. */
async function delegateHandle(
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

// Section: S0 — extension boots through the real index
async function caseS0(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("S0: devexpress command registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "S0");
}

// Section: S1 — a window that dies inside the grace window is killed, helper answers null
async function caseS1(): Promise<void> {
  const repo = mkRepo();
  const killed: string[] = [];
  const deps: DelegateDeps = {
    run: async () => ({ code: 0, stdout: "7\n", stderr: "" }),
    windowExists: async () => false,
    killWindow: async (idx: string) => { killed.push(idx); },
    graceMs: 100,
  };
  const result = await defaultDelegateWindow(repo, "task", deps);
  check("S1: dead window inside the grace window killed, helper answers null", result === null && killed.length === 1 && killed[0] === "7", String(result) + " | killed: " + JSON.stringify(killed));
}

// Section: S2 — the flow ignores the dormant seam and publishes in this session
async function caseS2(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRunner(GREEN_PUBLISH);
  const w = await delegateHandle(repo, { run: runner.run, fetchFeed: async () => "[]", pollMs: 1, ...mkPaneSeams() }, PUBLISH_PICKS);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("S2: publish-only flow ran here, prx queued", report.includes("published") && runner.calls.includes("prx"), report + " | " + runner.calls.join(" | "));
  check("S2: no build command ran", !runner.calls.some((c) => c.startsWith("brx")), runner.calls.join(" | "));
  answered(w.sel, "S2");
  noErrors(w.handle, "S2");
}

// Section: S3 — outside psmux → null without spawning
async function caseS3(): Promise<void> {
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

/** The contract, in the order it runs. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["S0", caseS0],
  ["S1", caseS1],
  ["S2", caseS2],
  ["S3", caseS3],
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
  console.log("reactive-xaf-build/delegate-tests — pi's own runtime, no fake\n");
  // S1 needs to look like psmux; S3 asserts the helper's answer without it.
  const prevPane = process.env.TMUX_PANE;
  process.env.TMUX_PANE = "%1";
  try {
    for (const [label, run] of CASES) await runCase(label, run);
  } finally {
    if (prevPane === undefined) delete process.env.TMUX_PANE;
    else process.env.TMUX_PANE = prevPane;
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
