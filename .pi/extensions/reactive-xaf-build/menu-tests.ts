/**
 * reactive-xaf-build/menu-tests — behavior contract for the /devexpress
 * skip-build publish surface (companion of menu.ts): the top-level Publish
 * pick (Publish → RX-XAF → Lab | Release) runs the publish phase (VM check →
 * commit → prx → AzDO monitor) WITHOUT the DX feed check or the local brx
 * build, and a leftover argument never bypasses the menu.
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The extension is
 * built from its real factory (`./index.ts`), which registers the command
 * through `./menu.ts`, and only the extension's own injectable ports are wired
 * in (command runner, pane machinery, AzDO watcher). Assertions read the host
 * capture (notices / errors); the harness answers ui.select with the FIRST
 * option, so every case answers the flow's prompts itself and fails on a prompt
 * it never mapped.
 *
 * This suite used to carry the shared ledger's boot proof (`ensureBootProof`,
 * B0) — the one place a real pi was started. It is gone: nothing here spawns,
 * and every case builds through pi's own loader in-process instead.
 *
 * The shared harness lives in the home tree, another drive this repo cannot
 * reach by a relative import: ./resolve.mjs installs the `@pi/` name floor first
 * and the harness modules are DYNAMIC imports after it (a static one is linked
 * before any hook exists). The real nuget.org, pwsh, psmux, VMs and git are
 * never touched.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/menu-tests.ts
 */
// test-timeout: 120000 — five runner builds on pi's real runtime
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";

let passed = 0;
let failed = 0;
const handles: any[] = [];
const tempDirs: string[] = [];

async function test(desc: string, fn: () => Promise<void> | void): Promise<void> {
  try {
    await fn();
    passed++;
    console.log(`  PASS ${desc}`);
  } catch (e) {
    failed++;
    console.log(`  FAIL ${desc}: ${e}`);
  }
}

function assert(desc: string, c: boolean, detail?: string): void {
  if (!c) throw new Error(desc + (detail ? ` — ${detail}` : ""));
}

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
 *  modules have to publish them — a stub would fabricate the observable a case
 *  asserts. Nothing spawns: this suite reads no ledger boot proof. */
async function installRoute(): Promise<void> {
  await import(new URL("./resolve.mjs", import.meta.url).href);
  buildRealRunner = (await import("@pi/pi-dev/real-runner.js")).buildRealRunner;
  await import("@pi/pi-dev/llm-utils.js"); // publishes __steer
  await import("@pi/pi-dev/tracked-write.js"); // publishes __writeFileSync
}

interface CommandSeams {
  run: (cmd: string) => Promise<any>;
  calls: string[];
}

/** The extension's command-runner port (BuildSeams.run): a scripted stand-in
 *  that records every command, because the real one spawns pwsh. */
function mkRunner(script: Array<{ match: string; result: any }>): CommandSeams {
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

interface PaneSeams {
  openBuildPane: (repo: string) => Promise<string | null>;
  runInPane: (pane: string, cmd: string) => Promise<void>;
  capturePane: (pane: string) => Promise<string>;
  closePane: (pane: string) => Promise<void>;
  startAzDoWatcher: () => { stop: () => void; active: () => boolean; lastBuildId: () => number | null };
  opened: string[];
  sent: string[];
}

/** The extension's pane ports. The publish-only flow must touch none of them,
 *  which is what `opened` / `sent` are asserted for. */
function mkPaneSeams(): PaneSeams {
  const opened: string[] = [];
  const sent: string[] = [];
  return {
    openBuildPane: async () => {
      opened.push("pane1");
      return "pane1";
    },
    runInPane: async (_pane: string, cmd: string) => {
      sent.push(cmd);
    },
    capturePane: async () => "",
    closePane: async () => {},
    startAzDoWatcher: () => ({ stop: () => {}, active: () => false, lastBuildId: () => null }),
    opened,
    sent,
  };
}

/** The repo guard (resolveRepo) needs the props file and src/Extensions: a
 *  temp dir carrying both, never the real repo. */
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-skip-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}

function okResult(stdout = ""): any {
  return { code: 0, stdout, stderr: "" };
}

const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const GREEN_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: "", stderr: "" } },
  { match: "prx", result: { code: 0, stdout: "Queued build 123", stderr: "" } },
];
const DIRTY_PUBLISH = [
  { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: " M Build/nuspec/Xpand.XAF.nuspec\n", stderr: "" } },
  { match: "git add -A", result: okResult() },
  { match: "git commit -m *", result: okResult() },
  { match: "prx", result: okResult() },
];

/** The menu flow's picks, keyed by a SUBSTRING of the prompt title: the commit
 *  prompt spells the whole message and the dirty summary, so it cannot be
 *  named exactly. The harness would answer every select with the FIRST option
 *  ("Build" for the top menu), so the Publish branch only happens because a
 *  case says so here — a case that let the default through would take a branch
 *  it never asked for and pass for the wrong reason. */
const MENU_PICKS: Record<string, string> = {
  DevExpress: "Publish",
  Project: "RX-XAF",
  "RX-XAF": "Lab",
  "Publish: ": "Publish",
  "Commit with message": "Commit",
};

interface CaseUi {
  ui: Record<string, unknown>;
  asks: string[];
  unexpected: string[];
}

/** The case's own ui.select override. A title the case did not map, or a
 *  mapped answer that is not one of the offered options, is RECORDED — the
 *  harness would otherwise answer it with the first option — and every case
 *  asserts the list is empty. The LONGEST matching key wins, so a future title
 *  that spells a menu key cannot be answered by it. */
function uiFor(answers: Record<string, string>): CaseUi {
  const asks: string[] = [];
  const unexpected: string[] = [];
  return {
    ui: {
      select: async (title: string, options: string[]): Promise<string | undefined> => {
        asks.push(title);
        const matching = Object.keys(answers).filter((k) => title.includes(k));
        const key = matching.sort((a, b) => b.length - a.length)[0];
        if (key === undefined) {
          unexpected.push(`unanswered prompt: ${title}`);
          return options?.[0];
        }
        const answer = answers[key];
        if (options && !options.includes(answer)) {
          unexpected.push(`"${answer}" is not one of: ${options.join(" | ")}`);
          return options[0];
        }
        return answer;
      },
    },
    asks,
    unexpected,
  };
}

/** Build the extension for one case: pi's real loader + ExtensionRunner, the
 *  real command, the case's own ports. */
async function menuHandle(
  repo: string, runner: CommandSeams, pane: PaneSeams, ui: Record<string, unknown>,
): Promise<any> {
  const handle = await buildRealRunner({
    cwd: repo,
    ui,
    activate: (pi: any) =>
      registerBuildCommand(pi, {
        run: runner.run,
        fetchFeed: async () => "[]",
        repoRoot: repo,
        pollMs: 1,
        openBuildPane: pane.openBuildPane,
        runInPane: pane.runInPane,
        capturePane: pane.capturePane,
        closePane: pane.closePane,
        startAzDoWatcher: pane.startAzDoWatcher,
      }),
  });
  handles.push(handle);
  return handle;
}

function noticesOf(handle: any): string[] {
  return handle.host.notices.map((n: { message: string }) => n.message);
}

/** The runner reports a throwing handler instead of throwing, so an error is
 *  a no-op unless a case looks for it. Every case looks. */
function assertNoErrors(handle: any, where: string): void {
  assert(`menu: no handler error during ${where}`, handle.host.errors.length === 0, JSON.stringify(handle.host.errors));
}

// Section: R1 — the process seams the extension reads at run time
async function caseRouteSeams(): Promise<void> {
  const g = globalThis as any;
  assert(
    "menu R1: __steer and __writeFileSync are the real modules' publications",
    typeof g.__steer === "function" && typeof g.__writeFileSync === "function",
    `__steer=${typeof g.__steer}, __writeFileSync=${typeof g.__writeFileSync}`,
  );
}

// Section: R2 — the resolver's whitelist is the floor under the @pi prefix
async function caseResolverFloor(): Promise<void> {
  let refused = false;
  try {
    await import("@pi/not-a-shared-utility/probe.js");
  } catch (err) {
    refused = String(err).includes("not-a-shared-utility");
  }
  assert("menu R2: an unlisted @pi name does not resolve", refused);
}

// Section: S0 — the command registers through the real index factory
async function caseS0(): Promise<void> {
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  assert(
    "S0: devexpress command registered via index.ts",
    handle.commands().includes("devexpress"),
    handle.commands().join(", "),
  );
  assertNoErrors(handle, "the index boot");
}

// Section: S1 — Publish → RX-XAF → Lab: no pane, no brx, prx runs, watcher started
async function caseS1(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const sel = uiFor(MENU_PICKS);
  const handle = await menuHandle(repo, runner, pane, sel.ui);
  await handle.runCommand("devexpress", "");
  const notices = noticesOf(handle);
  assert(
    "S1: no brx, no pane opened or sent",
    !runner.calls.some((c) => c.startsWith("brx")) && pane.opened.length === 0 && pane.sent.length === 0,
    JSON.stringify({ calls: runner.calls, opened: pane.opened, sent: pane.sent }),
  );
  assert(
    "S1: prx ran, watcher started, published",
    runner.calls.includes("prx") && notices.some((n) => n.includes("monitoring in background")) && notices.some((n) => n.includes("published")),
    notices.join(" | "),
  );
  assert("menu S1: every prompt the flow showed was answered by the case", sel.unexpected.length === 0, sel.unexpected.join(" | "));
  assertNoErrors(handle, "S1");
}

// Section: S2 — an argument left over from the retired word forms is inert
async function caseS2(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const sel = uiFor(MENU_PICKS);
  const handle = await menuHandle(repo, runner, pane, sel.ui);
  await handle.runCommand("devexpress", "publish lab");
  const notices = noticesOf(handle);
  assert(
    "S2: the arg is inert — the menu drove the flow",
    sel.asks.includes("DevExpress") && !runner.calls.some((c) => c.startsWith("brx")),
    JSON.stringify({ asks: sel.asks, calls: runner.calls }),
  );
  assert(
    "S2: no brx, prx ran, published",
    runner.calls.includes("prx") && notices.some((n) => n.includes("published")),
    notices.join(" | "),
  );
  assertNoErrors(handle, "S2");
}

// Section: S3 — the skip-build commit label is "Publish (N files)"
async function caseS3(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRunner(DIRTY_PUBLISH);
  const pane = mkPaneSeams();
  const sel = uiFor(MENU_PICKS);
  const handle = await menuHandle(repo, runner, pane, sel.ui);
  await handle.runCommand("devexpress", "");
  assert(
    "S3: commit labeled Publish, not Build fixes",
    runner.calls.some((c) => c.startsWith('git commit -m "Publish (1 files)"')),
    runner.calls.join(" | "),
  );
  assert("S3: published", noticesOf(handle).some((n) => n.includes("published")), noticesOf(handle).join(" | "));
  assert("menu S3: every prompt the flow showed was answered by the case", sel.unexpected.length === 0, sel.unexpected.join(" | "));
  assertNoErrors(handle, "S3");
}

// Section: S4 — the publish pick runs in the invoking window (no delegation)
async function caseS4(): Promise<void> {
  const repo = mkRepo();
  const runner = mkRunner(GREEN_PUBLISH);
  const pane = mkPaneSeams();
  const sel = uiFor(MENU_PICKS);
  const handle = await menuHandle(repo, runner, pane, sel.ui);
  await handle.runCommand("devexpress", "");
  assert(
    "S4: publish pick ran in this window, published",
    sel.asks.includes("DevExpress") && runner.calls.includes("prx") && noticesOf(handle).some((n) => n.includes("published")),
    noticesOf(handle).join(" | "),
  );
  assertNoErrors(handle, "S4");
}

/** The route's own evidence: the two process seams and the @pi name floor. The
 *  boot proof that used to sit here is gone — see the file header. */
async function routeCases(): Promise<void> {
  await test("R1: the real modules publish the seams the extension reads", caseRouteSeams);
  await test("R2: the @pi floor refuses a name the platform does not share", caseResolverFloor);
}

/** The extension's own surface: the /devexpress skip-build publish pick. */
async function surfaceCases(): Promise<void> {
  await test("S0: the command registers through the real index factory", caseS0);
  await test("S1: the Publish pick runs the publish-only flow", caseS1);
  await test("S2: a leftover argument never bypasses the menu", caseS2);
  await test("S3: a dirty tree commits with the Publish label", caseS3);
  await test("S4: the publish pick runs in the invoking window", caseS4);
}

async function main(): Promise<void> {
  await installRoute();
  console.log("reactive-xaf-build/menu-tests — pi's own runtime, no fake\n");
  await routeCases();
  await surfaceCases();
  console.log(`\n${passed + failed} total: ${passed} passed, ${failed} failed`);
  process.exitCode = failed > 0 ? 1 : 0;
}

main()
  .catch((e: unknown) => {
    console.log(`FAIL  menu-tests crashed: ${(e as Error)?.stack ?? e}`);
    process.exitCode = 1;
  })
  .finally(() => {
    for (const handle of handles) handle.dispose();
    for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
  });
