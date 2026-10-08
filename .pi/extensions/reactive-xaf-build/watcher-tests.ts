/**
 * reactive-xaf-build/watcher-tests — the AzDO chain watcher's contract (W1-W18).
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
function running(id: number): string {
  return crlf([`STATUS=${id};inProgress;;`]);
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
function mkSeams(statusQueue: string[]): { run: (cmd: string) => Promise<any>; calls: string[] } {
  const queue = statusQueue.slice();
  const calls: string[] = [];
  return {
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.includes("$top=5")) return { code: 0, stdout: queue.shift() ?? running(35760), stderr: "" };
      return flowResult(cmd);
    },
    calls,
  };
}
function fastWatcher(p: any, c: any, s: any, opts?: any): ReturnType<typeof startAzDoWatcher> {
  return startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000 });
}
function mkFetch(nugetVersions: string[]): (url: string) => Promise<string> {
  return async (url: string) => {
    if (url.includes("xpandnugetserver")) return JSON.stringify({ feed: oDataFeed(nugetVersions) });
    return JSON.stringify({ versions: ["26.1.3"] });
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
  check("W3: gave-up warning + steer + watcher stopped", toastsOf(w.handle).some((n) => n.type === "warning" && n.message.includes("gave up")) && steers.length === 1 && steers[0].content.includes("gave up") && !isAzDoWatcherActive(), JSON.stringify(toastsOf(w.handle)) + " | " + JSON.stringify(steers));
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

// Section: W16 — a finished head build of another version is not this run
async function caseW16(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams([doneNum(25698, "succeeded", "22.1.601.2")]);
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: mkFetch(LAB),
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 10, maxMs: 80 }),
  }, PUBLISH_LAB);
  await w.handle.runCommand("devexpress", "");
  await sleep(250);
  const toasts = toastText(w.handle);
  const steers = steersOf(w.handle);
  check("W16: wrong-version toast, no chain advance", toasts.some((n) => n.includes("is not this run")) && !toasts.some((n) => n.includes("release consumers pipeline")), JSON.stringify(toasts));
  check("W16: give-up steers + watcher stopped", steers.length === 1 && steers[0].content.includes("gave up") && !isAzDoWatcherActive(), JSON.stringify(steers));
  answered(w.sel, "W16");
  noErrors(w.handle, "W16");
}

// Section: W17 — the index lag: missing on the first assert, present on retry
async function caseW17(): Promise<void> {
  const repo = mkRepo();
  const seams = mkSeams(GREEN);
  let calls = 0;
  const fetchLate = async (url: string) => {
    if (url.includes("api.nuget.org")) {
      calls++;
      if (calls === 1) throw new Error("404 — flatcontainer not indexed yet");
      return "<package><metadata><id>Xpand.Extensions</id></metadata></package>";
    }
    return JSON.stringify({ feed: oDataFeed(LAB) });
  };
  const w = await watcherHandle(repo, {
    run: seams.run, fetchFeed: fetchLate, ghFetch: mkGh(GH_DRAFT).gh,
    startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, nugetRetries: 3, nugetRetryMs: 10 }),
  }, PUBLISH_RELEASE);
  await w.handle.runCommand("devexpress", "");
  await sleep(500);
  const toasts = toastText(w.handle);
  check("W17: nugets confirmed on retry after index lag — no warning, no steer", calls >= 2 && toasts.some((n) => n.includes("Nugets published") && n.includes("nuget.org")) && !toasts.some((n) => n.includes("NOT confirmed")) && w.handle.host.messages.length === 0, JSON.stringify(toasts) + " | " + JSON.stringify(w.handle.host.messages));
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
  console.log("reactive-xaf-build/watcher-tests — pi's own runtime, no fake\n");
  for (const [label, run] of CASES) await runCase(label, run);
  stopAzDoWatcher();
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
