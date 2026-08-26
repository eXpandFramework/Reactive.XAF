/**
 * reactive-xaf-build/release-tests — behavior contract for the build.ps1
 * version bump: the expand Release flow consults the feeds (Xpand server +
 * nuget.org) and writes the next release after the last published version;
 * Lab keeps the DX base; a failed consultation aborts loudly. Mock-pi harness
 * with injected seams — no real nuget/pwsh/git/pane.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/release-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";

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
    waitForPaneExit: async () => ({ code: 0, timedOut: false }),
    capturePane: async () => "",
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
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const DX_FEED = "https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json";
const EXPAND_ORG_FEED = "https://api.nuget.org/v3-flatcontainer/expandsystem/index.json";
const EXPAND_XPAND_FEED = "https://xpandnugetserver.azurewebsites.net/nuget/FindPackagesById()?id=%27eXpandSystem%27";
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
  mkdirSync(join(root, "Xpand", "Xpand.ExpressApp.Modules"), { recursive: true });
  const lines = pins.map(([id, v]) => `    <PackageVersion Include="${id}" Version="${v}" />`);
  const props = "<Project>\n  <ItemGroup>\n" + lines.join("\n") + "\n  </ItemGroup>\n</Project>\n";
  writeFileSync(join(root, "Directory.Packages.props"), props);
  return root;
}
function register(pi: any, runner: { run: (cmd: string) => Promise<any> }, pane: any, repo: string, feeds: { dx: string[]; org: string[]; xpand?: string[] | Error }): void {
  registerBuildCommand(pi, { run: runner.run, fetchFeed: mkExpandFeed(feeds), propsPath: join(repo, "Directory.Packages.props"), repoRoot: repo, pollMs: 1, ...pane });
}

(async () => {
  // Section: R0 — /devexpress registration through the real index boot
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("R0: devexpress command registered via index.ts", typeof cmd?.handler === "function");
  }
  // Section: R1 — expand Release bumps build.ps1 past the last published version on the feeds
  {
    const repo = mkExpandRepo([
      ["DevExpress.ExpressApp", "26.1.4"],
      ["DevExpress.Xpo", "26.1.4"],
      ["DevExpress.Utils", "26.1.4"],
    ]);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.400.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult(" M build.ps1\n") },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "git push eXpand HEAD:master", result: okResult() },
      { match: "px -Release", result: okResult("Queued build 123") },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    register(pi, runner, pane, repo, { dx: ["26.1.4"], org: ["26.1.301"], xpand: ["24.2.300", "25.2.800", "26.1.400"] });
    const ctx = mkCtx(["Build", "eXpand", "Release", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("R1: build.ps1 bumped to the next release (26.1.401.0, past feed 26.1.400)", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.401.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("R1: bump noted", ctx._notifies.some((n) => n.includes("bumped build.ps1 -version to 26.1.401.0")), ctx._notifies.join(" | "));
    check("R1: expand Release published via px -Release", result.includes("published") && runner.calls.includes("px -Release"), result + " | " + runner.calls.join(" | "));
  }
  // Section: R2 — expand Release with nothing published on the DX minor keeps the DX base
  {
    const repo = mkExpandRepo([
      ["DevExpress.ExpressApp", "26.1.4"],
      ["DevExpress.Xpo", "26.1.4"],
    ]);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.300.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult(" M build.ps1\n") },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "git push eXpand HEAD:master", result: okResult() },
      { match: "px -Release", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    register(pi, runner, pane, repo, { dx: ["26.1.4"], org: ["25.2.801"], xpand: ["24.2.800"] });
    const ctx = mkCtx(["Build", "eXpand", "Release", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("R2: other minors ignored → DX base 26.1.400.0", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("R2: published", result.includes("published"), result);
  }
  // Section: R3 — expand Lab bumps to the DX base without consulting the feeds
  {
    const repo = mkExpandRepo([
      ["DevExpress.ExpressApp", "26.1.4"],
      ["DevExpress.Xpo", "26.1.4"],
    ]);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.301.0\"\n");
    const runner = mkRunner([
      { match: VM_CHECK_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
      { match: "git status --short", result: okResult(" M build.ps1\n") },
      { match: "git add -A", result: okResult() },
      { match: "git commit -m *", result: okResult() },
      { match: "git push lab HEAD:master", result: okResult() },
      { match: "px", result: okResult() },
    ]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    register(pi, runner, pane, repo, { dx: ["26.1.4"], org: ["26.1.301"] });
    const ctx = mkCtx(["Build", "eXpand", "Lab", "Commit", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("R3: lab bumps to DX base 26.1.400.0 (no feed consult — org/xpand urls would throw)", readFileSync(join(repo, "build.ps1"), "utf-8").includes('-version "26.1.400.0"'), readFileSync(join(repo, "build.ps1"), "utf-8"));
    check("R3: lab published via px", result.includes("published") && runner.calls.includes("px"), result + " | " + runner.calls.join(" | "));
  }
  // Section: R4 — expand Release feed consultation failure aborts loudly, no build
  {
    const repo = mkExpandRepo([
      ["DevExpress.ExpressApp", "26.1.4"],
      ["DevExpress.Xpo", "26.1.4"],
    ]);
    writeFileSync(join(repo, "build.ps1"), "& .\\support\\build\\go.ps1 -version \"26.1.400.0\"\n");
    const runner = mkRunner([]);
    const pane = mkPaneSeams();
    const pi = mkPi();
    register(pi, runner, pane, repo, { dx: ["26.1.4"], org: ["26.1.301"], xpand: new Error("xpand server down") });
    const ctx = mkCtx(["Build", "eXpand", "Release"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("R4: aborted with the consultation error", result.includes("aborted") && result.includes("could not consult") && result.includes("xpand server down"), result);
    check("R4: no commands ran, nothing written", runner.calls.length === 0 && pane.sent.length === 0, runner.calls.join(" | ") + " " + JSON.stringify(pane.sent));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
