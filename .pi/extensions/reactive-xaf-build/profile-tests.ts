/**
 * reactive-xaf-build/profile-tests — behavior contract for RepoProfile.
 * Drives /devexpress through registerBuildCommand. Menu always picks
 * RX-XAF | eXpand then Lab | Release. Mock-pi; no real nuget/pwsh/git.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/profile-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync, existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./menu.js";
import { activeBuildRun, isBuildRunActive } from "./run.js";
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
function mkPi(): any {
  const cmds = new Map<string, any>();
  return {
    registerCommand: (n: string, d: any) => {
      cmds.set(n, d);
    },
    sendUserMessage: () => {},
    _cmds: cmds,
    _userMessages: [],
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
      notify: (m: string) => {
        notifies.push(m);
      },
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
async function waitFor(cond: () => boolean, ms = 4000): Promise<boolean> {
  const deadline = Date.now() + ms;
  while (Date.now() < deadline) {
    if (cond()) return true;
    await new Promise((resolve) => setTimeout(resolve, 20));
  }
  return cond();
}
/** Finish a started run the way the supervisor would; returns the marker it
 *  wrote so a failing check can show which run it finished. */
function finishRun(pane: any, code: number): string {
  const run = activeBuildRun();
  const script = /-File "([^"]+)"/.exec(pane?.sent?.[0] ?? "")?.[1] ?? "";
  const marker = run ? run.marker : script.replace(/run\.ps1$/, "exit.code");
  if (!marker) return "no run and no supervisor line";
  writeFileSync(marker, String(code));
  return `run=${run?.runId ?? "none"} marker=${marker}`;
}
function mkPane(): any {
  const sent: string[] = [];
  return {
    openBuildPane: async () => "pane1",
    runInPane: async (_p: string, cmd: string) => {
      sent.push(cmd);
    },
    capturePane: async () => "",
    probePane: async () => ({ alive: true, pid: 4242 }),
    runCadence: { signalMs: 10, probeMs: 10, cpuMs: 10, stallMs: 3_600_000, overrunMs: 3_600_000 },
    closePane: async () => {},
    startAzDoWatcher: () => ({
      stop: () => {},
      active: () => false,
      lastBuildId: () => null,
    }),
    sent,
  };
}
function mkRxRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-prof-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
function mkExpandRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "xpand-prof-"));
  mkdirSync(join(root, "Xpand", "Xpand.ExpressApp.Modules"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />\n");
  return root;
}
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_PREFIX = "Get-VM -Name C11,C12,C13,C14*";
const GREEN_RX = [
  { match: VM_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: VM_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: "", stderr: "" } },
  { match: "prx", result: { code: 0, stdout: "", stderr: "" } },
];
const GREEN_EXPAND = [
  { match: VM_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: VM_PREFIX, result: { code: 0, stdout: VM_RUN, stderr: "" } },
  { match: "git status --short", result: { code: 0, stdout: "", stderr: "" } },
  { match: "git push lab HEAD:master", result: { code: 0, stdout: "", stderr: "" } },
  { match: "px", result: { code: 0, stdout: "", stderr: "" } },
];

(async () => {
  {
    const pi = mkPi();
    activate(pi);
    check("P0: devexpress registered via index.ts", typeof pi._cmds.get("devexpress")?.handler === "function");
  }
  {
    const other = mkdtempSync(join(tmpdir(), "neither-"));
    const runner = mkRunner([]);
    const pi = mkPi();
    registerBuildCommand(pi, { run: runner.run, fetchFeed: async () => "[]", repoRoot: other });
    const result = await pi._cmds.get("devexpress").handler([], mkCtx(["Build", "RX-XAF", "Lab"], other));
    check("P1: RX loud-rejects foreign tree, zero commands", result.includes("not inside the Reactive.XAF repo") && runner.calls.length === 0, result);
  }
  {
    const repo = mkExpandRepo();
    const runner = mkRunner(GREEN_EXPAND);
    const pane = mkPane();
    const pi = mkPi();
    registerBuildCommand(pi, {
      run: runner.run,
      fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
      propsPath: join(repo, "Directory.Packages.props"),
      repoRoot: repo,
      profile: expandProfile,
      pollMs: 1,
      ...pane,
    });
    const ctx = mkCtx(["Build", "eXpand", "Lab", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("P2: menu offered Project pick", ctx._prompts.includes("Project"), ctx._prompts.join(" | "));
    check("P2: the pane got the supervisor script running bx lab", pane.sent.length === 1 && /-File ".*run\.ps1"$/.test(pane.sent[0]) && result.includes("Build started in pane"), JSON.stringify(pane.sent) + " " + result);
    const run = activeBuildRun();
    const where = finishRun(pane, 0);
    const published = await waitFor(() => ctx._notifies.some((n) => n.includes("published")));
    const left = run ? existsSync(run.marker) : false;
    check("P2: git push lab then px, published", published && runner.calls.includes("git push lab HEAD:master") && runner.calls.includes("px"), `${where} active=${isBuildRunActive()} markerLeft=${left} calls=${runner.calls.join("|")} notifies=${ctx._notifies.join("|")}`);
  }
  {
    const expand = mkExpandRepo();
    const rx = mkRxRepo();
    const expCalls: string[] = [];
    const rxCalls: string[] = [];
    const expPi = mkPi();
    registerBuildCommand(expPi, {
      run: async (cmd: string) => {
        expCalls.push(cmd);
        return { code: 0, stdout: "STATUS=1;completed;succeeded;", stderr: "" };
      },
      fetchFeed: async () => "[]",
      repoRoot: expand,
      profile: expandProfile,
    });
    await expPi._cmds.get("devexpress").handler([], mkCtx(["Last build status"], expand));
    const rxPi = mkPi();
    registerBuildCommand(rxPi, {
      run: async (cmd: string) => {
        rxCalls.push(cmd);
        return { code: 0, stdout: "STATUS=1;completed;succeeded;", stderr: "" };
      },
      fetchFeed: async () => "[]",
      repoRoot: rx,
    });
    await rxPi._cmds.get("devexpress").handler([], mkCtx(["Last build status"], rx));
    check("P3: expand status queries def 94", expCalls.some((c) => c.includes("definitions=94")), JSON.stringify(expCalls));
    check("P3: RX status still queries def 23", rxCalls.some((c) => c.includes("definitions=23")), JSON.stringify(rxCalls));
  }
  {
    const repo = mkRxRepo();
    const runner = mkRunner(GREEN_RX);
    const pane = mkPane();
    const pi = mkPi();
    registerBuildCommand(pi, {
      run: runner.run,
      fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
      propsPath: join(repo, "Directory.Packages.props"),
      repoRoot: repo,
      pollMs: 1,
      ...pane,
    });
    const ctx = mkCtx(["Build", "RX-XAF", "Lab", "Publish"], repo);
    const result = await pi._cmds.get("devexpress").handler([], ctx);
    check("P4: RX still starts brx in a pane", /-File ".*run\.ps1"$/.test(pane.sent[0] ?? "") && result.includes("Build started in pane"), JSON.stringify(pane.sent) + " | " + result);
    finishRun(pane, 0);
    await waitFor(() => runner.calls.includes("prx"));
    check("P4: prx still ran", runner.calls.includes("prx"), runner.calls.join(" | "));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
