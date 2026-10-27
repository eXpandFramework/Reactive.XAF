/**
 * reactive-xaf-build/release-tests — the build.ps1 version-bump contract.
 *
 * Runtime: pi's OWN loader and ExtensionRunner, through the resolver-first route
 * this file carries itself — there is no hand-written pi here. The route is the
 * shape `menu-tests.ts` established (commit 93e1a4754): `./resolve.mjs` installs
 * the `@pi/` name floor first, and the harness plus the seam-owning modules are
 * DYNAMIC imports after it. This suite imports no boot proof, because nothing
 * here spawns.
 *
 * The flow is driven through pi's own command dispatch (Build → eXpand → Lab |
 * Release) with the extension's OWN ports injected: the command runner, the
 * feed fetcher and the pane machinery, so the real nuget.org, the Xpand server,
 * pwsh and psmux are never touched. The version bump is read off the real
 * `build.ps1` in a temp checkout; the flow's report is asserted where the user
 * meets it (`handle.host.notices`), because pi discards a handler's return.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/release-tests.ts
 */
// test-timeout: 120000 — one runner build per case on pi's real runtime
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
/** Local delay: the test harness owns its own clock. */
const sleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
import { stopBuildRun } from "./run.js";

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
 *  LONGEST match first (the DX prompt's title spells "…update all DevExpress.*
 *  pins?" and the top menu's own key is `DevExpress`). A title the case did not
 *  map, or an answer that is not one of the offered options, is RECORDED — the
 *  harness would otherwise answer with the first option — and every case asserts
 *  the list is empty. */
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
function mkPaneSeams(): any {
  const opened: string[] = [];
  const sent: string[] = [];
  const closed: string[] = [];
  const watcherStarts: number[] = [];
  return {
    openBuildPane: async () => {
      opened.push("pane1");
      return "pane1";
    },
    runInPane: async (_pane: string, cmd: string) => { sent.push(cmd); },
    capturePane: async () => "",
    probePane: async () => ({ alive: true, pid: 4242 }),
    // The CPU sampler is a seam like any other: without it the run falls back
    // to the real host sampler (a pwsh call), which never settles in the island
    // and leaves the watch wedged in its first tick. A flat reading keeps the
    // stall rule on output silence alone.
    sampleCpu: async () => 1,
    runCadence: { signalMs: 10, probeMs: 10, cpuMs: 10, stallMs: 3_600_000, overrunMs: 3_600_000 },
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
function okResult(stdout = ""): any {
  return { code: 0, stdout, stderr: "" };
}
/** What a case hands its own assertion half: the fixture, the ports and the host. */
interface Parts {
  repo: string;
  runner: any;
  pane: any;
  sel: CaseUi;
  handle: any;
}
/** The publish half every expand case runs: the two probes, the git read, and
 *  the push/queue pair the Lab | Release choice decides. */
function publishScript(push: string, queue: string): Array<{ match: string; result: any }> {
  return [
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
    { match: "git status --short", result: okResult(" M build.ps1\n") },
    { match: "git add -A", result: okResult() },
    { match: "git commit -m *", result: okResult() },
    { match: push, result: okResult() },
    { match: queue, result: okResult("Queued build 123") },
  ];
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const DX_FEED = "https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json";
const EXPAND_ORG_FEED = "https://api.nuget.org/v3-flatcontainer/expandsystem/index.json";
const EXPAND_XPAND_FEED = "https://xpandnugetserver.azurewebsites.net/nuget/FindPackagesById()?id=%27eXpandSystem%27";
/** The feed seam: an unknown URL THROWS, so a consult the case did not expect
 *  aborts the flow instead of passing silently. */
function mkExpandFeed(v: { dx: string[]; org: string[]; xpand?: string[] | Error }): (url: string) => Promise<string> {
  return async (url: string) => {
    if (url === DX_FEED) return JSON.stringify({ versions: v.dx });
    if (url === EXPAND_ORG_FEED) return JSON.stringify({ versions: v.org });
    if (url === EXPAND_XPAND_FEED) {
      if (v.xpand instanceof Error) throw v.xpand;
      return (v.xpand ?? []).map((ver) => `<entry><id>https://xpandnugetserver.azurewebsites.net/nuget/Packages(Id='eXpandSystem',Version='${ver}')</id></entry>`).join("\n");
    }
    throw new Error(`unexpected feed url: ${url}`);
  };
}
function mkExpandRepo(pins: Array<[string, string]>): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-release-"));
  tempDirs.push(root);
  mkdirSync(join(root, "Xpand", "Xpand.ExpressApp.Modules"), { recursive: true });
  const lines = pins.map(([id, v]) => `    <PackageVersion Include="${id}" Version="${v}" />`);
  const props = "<Project>\n  <ItemGroup>\n" + lines.join("\n") + "\n  </ItemGroup>\n</Project>\n";
  writeFileSync(join(root, "Directory.Packages.props"), props);
  return root;
}
/** The bumps a Release case fills in, so the DX phase and the expand pins both
 *  find the file they expect. */
const EXPAND_PINS: Array<[string, string]> = [
  ["DevExpress.ExpressApp", "26.1.4"],
  ["DevExpress.Xpo", "26.1.4"],
  ["DevExpress.Utils", "26.1.4"],
];
/** The Build menu picks: the top menu, the project, then the flow's own
 *  prompts. `choice` is the Lab | Release pick. */
function buildPicks(choice: string): Record<string, Answer> {
  return { DevExpress: "Build", Project: "eXpand", eXpand: choice, "Commit with message": "Commit", "Publish: ": "Publish" };
}
function propsOf(repo: string): string {
  return readFileSync(join(repo, "build.ps1"), "utf-8");
}
/** pi's own runtime for one case: the real command, the case's picks and ports. */
async function releaseHandle(
  repo: string, seams: Record<string, unknown>, picks: Record<string, Answer>,
): Promise<{ handle: any; sel: CaseUi }> {
  const sel = uiFor(picks);
  await installRoute();
  const handle = await buildRealRunner({
    cwd: repo,
    ui: sel.ui,
    activate: (pi: any) => registerBuildCommand(pi, seams),
  });
  handles.push(handle);
  return { handle, sel };
}
/** The flow's ports for a bump case: the runner, the feed fixture and the pane
 *  seams, over the temp checkout. */
function bumpSeams(repo: string, runner: { run: (cmd: string) => Promise<any> }, pane: any, feeds: { dx: string[]; org: string[]; xpand?: string[] | Error }): Record<string, unknown> {
  return { run: runner.run, fetchFeed: mkExpandFeed(feeds), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane };
}

// Section: R5-R11 — the GitHub RELEASE: published, recorded, never silent.
//
// The pipeline creates the release as a DRAFT on a master build
// (`DevExpress.XAF-GitHub-Release.ps1:121`, `Xpand-GitHub-Release.ps1:150`), so
// unless this flow publishes it the release stays invisible until a human
// visits GitHub. Every case drives the real command and asserts what the user
// meets: the toast, the PATCH GitHub received, and the row "Last build status"
// reads afterwards.
const RX_VERSION_FILE = join("src", "Common", "AssemblyInfoVersion.cs");
const EXPAND_VERSION_FILE = join("Xpand", "Xpand.Utils", "Properties", "XpandAssemblyInfo.cs");
const RX_RELEASES = "https://api.github.com/repos/eXpandFramework/Reactive.XAF/releases?per_page=50";
const EXPAND_RELEASES = "https://api.github.com/repos/eXpandFramework/eXpand/releases?per_page=50";

type GhAnswer = any[] | { status: number };
interface GhFake {
  gh: (url: string, opts?: { method?: string; body?: string }) => Promise<{ ok: boolean; status: number; text: string }>;
  gets: string[];
  patches: string[];
}
/** GitHub's release API, scripted: each GET answers the next list (the last one
 *  repeats), and `{status}` answers an HTTP failure so the retry path is
 *  reachable. A PATCH is recorded and never answered with a list. */
function mkGh(answers: GhAnswer[]): GhFake {
  const gets: string[] = [];
  const patches: string[] = [];
  let i = 0;
  return {
    gets,
    patches,
    gh: async (url: string, opts?: { method?: string; body?: string }) => {
      if (opts?.method === "PATCH") {
        patches.push(opts.body ?? "");
        return { ok: true, status: 200, text: "{}" };
      }
      gets.push(url);
      const answer = answers[Math.min(i, answers.length - 1)];
      i++;
      if (answer && !Array.isArray(answer)) return { ok: false, status: answer.status, text: "" };
      return { ok: true, status: 200, text: JSON.stringify(answer ?? []) };
    },
  };
}
/** A draft as GitHub reports it: the shape the pipeline leaves behind. */
function draftOf(tag: string, id = 424635336): any {
  return { id, tag_name: tag, draft: true, prerelease: false };
}
function writeVersion(root: string, file: string, version: string): void {
  mkdirSync(join(root, dirname(file)), { recursive: true });
  writeFileSync(join(root, file), `internal static class AssemblyInfo {\n    public const string Version = "${version}";\n}\n`);
}
/** The tree an RX release case publishes: `Directory.Packages.props` and
 *  `src/Extensions` are what resolveRepo detects. `version: null` leaves the
 *  version file out, which is the unreadable case. */
function mkRxRepo(version: string | null): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-ghrel-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"DevExpress.ExpressApp\" Version=\"26.1.4\" />\n  </ItemGroup>\n</Project>\n");
  if (version !== null) writeVersion(root, RX_VERSION_FILE, version);
  return root;
}
/** eXpand's tree, off the bump fixture: the release case only needs the
 *  version file the profile names. */
function mkExpandReleaseRepo(version: string | null): string {
  const root = mkExpandRepo(EXPAND_PINS);
  if (version !== null) writeVersion(root, EXPAND_VERSION_FILE, version);
  return root;
}
/** The release item's picks: the top menu, the project, then Lab | Release. */
function releasePicks(project: string, choice: string): Record<string, Answer> {
  return { DevExpress: "Publish GitHub release", Project: project, [project]: choice };
}
/** The flow's ports for a release case: a runner that answers no commands, a
 *  GitHub fake, and this case's own store. */
function releaseOnlySeams(repo: string, gh: GhFake, extra: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    run: async () => okResult(),
    fetchFeed: async (url: string) => { throw new Error(`unexpected feed url: ${url}`); },
    repoRoot: repo,
    stampPath: join(repo, "release.db"),
    ghFetch: gh.gh,
    ghToken: "ghp_case",
    releaseIntervalMs: 5,
    releaseDeadlineMs: 3000,
    pollMs: 1,
    ...extra,
  };
}

// Section: R0 — /devexpress registration through the real index boot
async function caseR0(): Promise<void> {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("R0: devexpress command registered via index.ts", handle.commands().includes("devexpress"), handle.commands().join(", "));
  noErrors(handle, "R0");
}

// Section: R1 — expand Release bumps build.ps1 past the last published version
async function caseR1(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandRepo(EXPAND_PINS);
  writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.400.0\"\n");
  const runner = mkRunner(publishScript("git push eXpand HEAD:master", "px -Release"));
  const pane = mkPaneSeams();
  const feeds = { dx: ["26.1.4"], org: ["26.1.301"], xpand: ["24.2.300", "25.2.800", "26.1.400"] };
  const w = await releaseHandle(repo, bumpSeams(repo, runner, pane, feeds), buildPicks("Release"));
  await w.handle.runCommand("devexpress", "");
  checkR1({ repo, runner, pane, sel: w.sel, handle: w.handle });
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("px -Release"));
  check("R1: expand Release published via px -Release", runner.calls.includes("px -Release"), runner.calls.join(" | ") + " || " + noticesOf(w.handle).join(" | "));
  answered(w.sel, "R1");
  noErrors(w.handle, "R1");
}

/** R1's checks: the bump past the feed's last version, and the started build. */
function checkR1(p: Parts): void {
  check("R1: build.ps1 bumped to the next release (26.1.401.0, past feed 26.1.400)", propsOf(p.repo).includes('-version "26.1.401.0"'), propsOf(p.repo));
  check("R1: bump noted", noticesOf(p.handle).some((n) => n.includes("bumped build.ps1 -version to 26.1.401.0")), noticesOf(p.handle).join(" | "));
  check("R1: the command returned on a started build", noticesOf(p.handle).some((n) => n.includes("Build started in pane")), noticesOf(p.handle).join(" | "));
}

// Section: R2 — nothing published on the DX minor keeps the DX-derived base
async function caseR2(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandRepo(EXPAND_PINS);
  writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.300.0\"\n");
  const runner = mkRunner(publishScript("git push eXpand HEAD:master", "px -Release"));
  const pane = mkPaneSeams();
  const feeds = { dx: ["26.1.4"], org: ["25.2.801"], xpand: ["24.2.800"] };
  const w = await releaseHandle(repo, bumpSeams(repo, runner, pane, feeds), buildPicks("Release"));
  await w.handle.runCommand("devexpress", "");
  checkR2({ repo, runner, pane, sel: w.sel, handle: w.handle });
  finishRun(pane, 0);
  await waitFor(() => noticesOf(w.handle).some((n) => n.includes("published")));
  check("R2: published", noticesOf(w.handle).some((n) => n.includes("published")), noticesOf(w.handle).join(" | "));
  answered(w.sel, "R2");
  noErrors(w.handle, "R2");
}

/** R2's checks: nothing on the DX minor, so the DX-derived base stays. */
function checkR2(p: Parts): void {
  check("R2: other minors ignored → DX base 26.1.400.0", propsOf(p.repo).includes('-version "26.1.400.0"'), propsOf(p.repo));
  check("R2: the command returned on a started build", noticesOf(p.handle).some((n) => n.includes("Build started in pane")), noticesOf(p.handle).join(" | "));
}

// Section: R3 — expand Lab bumps to the DX base without consulting the feeds
async function caseR3(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandRepo(EXPAND_PINS);
  writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.301.0\"\n");
  const runner = mkRunner(publishScript("git push lab HEAD:master", "px"));
  const pane = mkPaneSeams();
  // No `xpand` fixture: the Lab path must not consult the Xpand server at all,
  // and the feed seam THROWS on a URL the case did not set up.
  const feeds = { dx: ["26.1.4"], org: ["26.1.301"] };
  const w = await releaseHandle(repo, bumpSeams(repo, runner, pane, feeds), buildPicks("Lab"));
  await w.handle.runCommand("devexpress", "");
  checkR3({ repo, runner, pane, sel: w.sel, handle: w.handle });
  finishRun(pane, 0);
  await waitFor(() => runner.calls.includes("px"));
  check("R3: lab published via px", runner.calls.includes("px"), runner.calls.join(" | "));
  answered(w.sel, "R3");
  noErrors(w.handle, "R3");
}

/** R3's checks: the Lab base with no feed consult at all. */
function checkR3(p: Parts): void {
  check("R3: lab bumps to DX base 26.1.400.0 (no feed consult — org/xpand urls would throw)", propsOf(p.repo).includes('-version "26.1.400.0"'), propsOf(p.repo));
  check("R3: the command returned on a started build", noticesOf(p.handle).some((n) => n.includes("Build started in pane")), noticesOf(p.handle).join(" | "));
}

// Section: R4 — a failed consultation aborts loudly, with no build
async function caseR4(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandRepo([
    ["DevExpress.ExpressApp", "26.1.4"],
    ["DevExpress.Xpo", "26.1.4"],
  ]);
  writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.400.0\"\n");
  const runner = mkRunner([]);
  const pane = mkPaneSeams();
  const picks: Record<string, Answer> = { DevExpress: "Build", Project: "eXpand", eXpand: "Release" };
  const w = await releaseHandle(repo, bumpSeams(repo, runner, pane, { dx: ["26.1.4"], org: ["26.1.301"], xpand: new Error("xpand server down") }), picks);
  await w.handle.runCommand("devexpress", "");
  const report = noticesOf(w.handle).join("\n");
  check("R4: aborted with the consultation error", report.includes("aborted") && report.includes("could not consult") && report.includes("xpand server down"), report);
  check("R4: no commands ran, nothing written", runner.calls.length === 0 && pane.sent.length === 0, runner.calls.join(" | ") + " " + JSON.stringify(pane.sent));
  answered(w.sel, "R4");
  noErrors(w.handle, "R4");
}

// Section: R5 — RX-XAF Release: the draft becomes a release
async function caseR5(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo("4.261.6.0");
  const gh = mkGh([[draftOf("4.261.6.0")]]);
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), releasePicks("RX-XAF", "Release"));
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R5: the draft is published, not left for a human", text.includes("4.261.6.0") && text.includes("published"), text);
  check("R5: exactly one PATCH, draft=false prerelease=false", gh.patches.length === 1 && JSON.parse(gh.patches[0]).draft === false && JSON.parse(gh.patches[0]).prerelease === false, JSON.stringify(gh.patches));
  check("R5: the release list came from the RX repo", gh.gets[0] === RX_RELEASES, JSON.stringify(gh.gets));
  answered(w.sel, "R5");
  noErrors(w.handle, "R5");
}

// Section: R6 — eXpand Release: the same publish, the other repo (parity)
async function caseR6(): Promise<void> {
  stopBuildRun();
  const repo = mkExpandReleaseRepo("26.1.600.0");
  const gh = mkGh([[draftOf("26.1.600.0")]]);
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), releasePicks("eXpand", "Release"));
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R6: eXpand's draft is published (it was asserted, never published)", text.includes("26.1.600.0") && text.includes("published"), text);
  check("R6: the eXpand repo was queried and patched", gh.gets[0] === EXPAND_RELEASES && gh.patches.length === 1, JSON.stringify(gh.gets) + " " + JSON.stringify(gh.patches));
  answered(w.sel, "R6");
  noErrors(w.handle, "R6");
}

// Section: R7 — a release the pipeline already published: no PATCH
async function caseR7(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo("4.261.6.0");
  const gh = mkGh([[{ id: 1, tag_name: "4.261.6.0", draft: false, prerelease: true }]]);
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), releasePicks("RX-XAF", "Lab"));
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R7: an already-published release is reported, not re-published", text.includes("already published") && gh.patches.length === 0, text + " " + JSON.stringify(gh.patches));
  check("R7: the pre-release word is kept", text.includes("pre-release"), text);
  answered(w.sel, "R7");
  noErrors(w.handle, "R7");
}

// Section: R8 — a late draft (after a refused call) is waited for, not failed
async function caseR8(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo("4.261.6.0");
  const gh = mkGh([{ status: 500 }, [], [draftOf("4.261.6.0")]]);
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), releasePicks("RX-XAF", "Release"));
  await w.handle.runCommand("devexpress", "");
  const published = await waitFor(() => gh.patches.length > 0, 4000);
  const text = noticesOf(w.handle).join("\n");
  check("R8: the draft that appeared late is still published", published && gh.patches.length === 1, text + " " + JSON.stringify(gh.patches));
  check("R8: the first miss is not reported as a failure", !text.includes("gave up") && !text.includes("NOT confirmed"), text);
  check("R8: it kept watching past the refused call", gh.gets.length >= 2, JSON.stringify(gh.gets));
  answered(w.sel, "R8");
  noErrors(w.handle, "R8");
}

// Section: R9 — no token: nothing sent, the URL to publish by hand
async function caseR9(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo("4.261.6.0");
  const gh = mkGh([[draftOf("4.261.6.0")]]);
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh, { ghToken: "" }), releasePicks("RX-XAF", "Release"));
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R9: no GitHub call without a token", gh.gets.length === 0 && gh.patches.length === 0, JSON.stringify(gh.gets) + " " + JSON.stringify(gh.patches));
  check("R9: the toast names GH_TOKEN and the release URL", text.includes("GH_TOKEN") && text.includes("github.com/eXpandFramework/Reactive.XAF/releases"), text);
  answered(w.sel, "R9");
  noErrors(w.handle, "R9");
}

// Section: R10 — no version file: reported at every door, recorded, never pending
async function caseR10(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo(null);
  const gh = mkGh([[draftOf("4.261.6.0")]]);
  let step = 0;
  const picks: Record<string, Answer> = {
    DevExpress: () => (step++ === 0 ? "Publish GitHub release" : "Last build status"),
    Project: "RX-XAF",
    "RX-XAF": "Release",
  };
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), picks);
  await w.handle.runCommand("devexpress", "");
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R10: no version readable → the toast says so", text.includes("no version"), text);
  check("R10: nothing was asked of GitHub", gh.gets.length === 0 && gh.patches.length === 0, JSON.stringify(gh.gets));
  check(
    "R10: the recorded row reads as NOT ATTEMPTED, never as a blank version",
    text.includes("GitHub release: not attempted") && !text.includes("GitHub release  "),
    text,
  );
  answered(w.sel, "R10");
  noErrors(w.handle, "R10");
}

// Section: R11 — the outcome survives the run: Last build status reports it
async function caseR11(): Promise<void> {
  stopBuildRun();
  const repo = mkRxRepo("4.261.6.0");
  const gh = mkGh([[draftOf("4.261.6.0")]]);
  let step = 0;
  const picks: Record<string, Answer> = {
    DevExpress: () => (step++ === 0 ? "Publish GitHub release" : "Last build status"),
    Project: "RX-XAF",
    "RX-XAF": "Release",
  };
  const w = await releaseHandle(repo, releaseOnlySeams(repo, gh), picks);
  await w.handle.runCommand("devexpress", "");
  await w.handle.runCommand("devexpress", "");
  const text = noticesOf(w.handle).join("\n");
  check("R11: Last build status carries the release outcome from the store", text.includes("GitHub release 4.261.6.0") && text.includes("published"), text);
  check("R11: the AzDO line is still there beside it", text.includes("AzDO"), text);
  answered(w.sel, "R11");
  noErrors(w.handle, "R11");
}

/** The contract, in the order it runs. */
const CASES: Array<[string, () => Promise<void>]> = [
  ["R0", caseR0],
  ["R1", caseR1],
  ["R2", caseR2],
  ["R3", caseR3],
  ["R4", caseR4],
  ["R5", caseR5],
  ["R6", caseR6],
  ["R7", caseR7],
  ["R8", caseR8],
  ["R9", caseR9],
  ["R10", caseR10],
  ["R11", caseR11],
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
  console.log("reactive-xaf-build/release-tests — pi's own runtime, no fake\n");
  for (const [label, run] of CASES) await runCase(label, run);
  stopBuildRun();
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exitCode = fail > 0 ? 1 : 0;
}

main().finally(() => {
  for (const handle of handles) handle.dispose();
  for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
});
