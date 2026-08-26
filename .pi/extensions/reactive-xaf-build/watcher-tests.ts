/**
 * reactive-xaf-build/watcher-tests — W1-W16. Mock pi; no real pwsh/AzDO.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { setTimeout as sleep } from "node:timers/promises";
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";
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
function mkRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-watcher-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  mkdirSync(join(root, "src", "Common"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  writeFileSync(join(root, "src", "Common", "AssemblyInfoVersion.cs"), 'class AssemblyInfoVersion { public const string Version = "4.261.2.1"; }');
  return root;
}
function mkExpandRepo(): string {
  const root = mkdtempSync(join(tmpdir(), "xpand-watcher-"));
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
function mkPi(): any {
  const cmds = new Map<string, any>();
  const msgs: Array<{ content: string; opts: any }> = [];
  return {
    registerCommand: (n: string, d: any) => {
      cmds.set(n, d);
    },
    sendUserMessage: (content: string, opts: any) => {
      msgs.push({ content, opts });
    },
    _cmds: cmds,
    _userMessages: msgs,
  };
}
function mkCtx(repo: string): any {
  const notifies: Array<{ msg: string; type: string }> = [];
  return {
    cwd: repo,
    ui: {
      select: async () => "Publish",
      notify: async (msg: string, type: string) => {
        notifies.push({ msg, type });
      },
    },
    _notifies: notifies,
  };
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
  const gh = async (url: string, opts: any = {}) => {
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

(async () => {
  {
    const repo = mkRepo();
    const pi = mkPi();
    const seams = mkSeams([running(35760), running(35760), ...GREEN]);
    const ctx = mkCtx(repo);
    const gh = mkGh(GH_DRAFT);
    registerBuildCommand(pi, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: gh.gh, repoRoot: repo, startAzDoWatcher: fastWatcher });
    const result = await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    check("W1: publish returns immediately, monitoring in background", result.includes("monitoring in background") && result.includes("published"), result);
    check("W1: watcher active right after publish", isAzDoWatcherActive());
    await sleep(400);
    check("W1: toast on every poll (both running checks toasted)", ctx._notifies.filter((n) => n.msg.includes("inProgress")).length >= 2, JSON.stringify(ctx._notifies));
    check("W1: nugets asserted on the eXpand server", ctx._notifies.some((n) => n.msg.includes("Nugets published") && n.msg.includes("eXpand nuget server")), JSON.stringify(ctx._notifies));
    check("W1: chain advanced to the release consumers pipeline", ctx._notifies.some((n) => n.msg.includes("release consumers pipeline")), JSON.stringify(ctx._notifies));
    check("W1: lab draft published as pre-release + chain complete + watcher stopped", ctx._notifies.some((n) => n.msg.includes("GitHub pre-release 4.261.2.1 published from draft") && n.msg.includes("chain complete")) && !isAzDoWatcherActive(), JSON.stringify(ctx._notifies));
    check("W1: PATCH carried draft=false prerelease=true", gh.patches.length === 1 && JSON.parse(gh.patches[0]).prerelease === true && JSON.parse(gh.patches[0]).draft === false, JSON.stringify(gh.patches));
    check("W1: no steer on success", pi._userMessages.length === 0, JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, { run: mkSeams([done(35761, "failed")]).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(200);
    check("W2: failure toast is a warning", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("FAILED")), JSON.stringify(ctx._notifies));
    check("W2: failure steers (deliverAs steer)", pi._userMessages.length === 1 && pi._userMessages[0].opts?.deliverAs === "steer" && pi._userMessages[0].content.includes("FAILED"), JSON.stringify(pi._userMessages));
    check("W2: watcher stopped", !isAzDoWatcherActive());
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: mkSeams([]).run, fetchFeed: mkFetch(LAB), repoRoot: repo,
      startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 10, maxMs: 60 }),
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(250);
    check("W3: gave-up warning + steer + watcher stopped", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("gave up")) && pi._userMessages.length === 1 && pi._userMessages[0].content.includes("gave up") && !isAzDoWatcherActive(), JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi1 = mkPi();
    registerBuildCommand(pi1, { run: mkSeams([]).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi1._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    check("W4: first watcher active", isAzDoWatcherActive());
    const pi2 = mkPi();
    registerBuildCommand(pi2, { run: mkSeams([]).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi2._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    check("W4: second publish keeps exactly one watcher active", isAzDoWatcherActive());
    stopAzDoWatcher();
    check("W4: stopAzDoWatcher stops it", !isAzDoWatcherActive());
  }
  {
    const pi = mkPi();
    activate(pi);
    check("W5: devexpress command registered via index.ts", typeof pi._cmds.get("devexpress")?.handler === "function");
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, { run: mkSeams(GREEN).run, fetchFeed: mkFetch(["4.242.3"]), ghFetch: mkGh(GH_DRAFT).gh, repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(400);
    check("W6: missing version → warning + steer", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("Nugets NOT confirmed")) && pi._userMessages.length === 1 && pi._userMessages[0].content.includes("NOT found"), JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
    check("W6: chain continued to the release consumers pipeline", ctx._notifies.some((n) => n.msg.includes("release consumers pipeline")), JSON.stringify(ctx._notifies));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkSeams([done(35760, "succeeded"), done(35780, "succeeded"), done(35790, "failed")]).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    await sleep(400);
    check("W7: release failure steers + stops", pi._userMessages.length === 1 && pi._userMessages[0].content.includes("FAILED") && !isAzDoWatcherActive(), JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: mkSeams(GREEN).run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_MISSING).gh, repoRoot: repo,
      startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, ghRetries: 2, ghRetryMs: 10 }),
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(500);
    check("W8: missing GitHub draft → warning + steer", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("GitHub release was NOT confirmed")) && pi._userMessages.length === 1 && pi._userMessages[0].content.includes("NOT found after 2 tries"), JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
    check("W8: watcher stopped", !isAzDoWatcherActive());
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: mkSeams(GREEN).run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_LATE).gh, repoRoot: repo,
      startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 20, maxMs: 5000, ghRetries: 3, ghRetryMs: 10 }),
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(500);
    check("W9: draft found on retry → published toast, no steer", ctx._notifies.some((n) => n.msg.includes("GitHub pre-release 4.261.2.1 published from draft")) && pi._userMessages.length === 0, JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const seams = mkSeams([done(40010, "succeeded"), done(40020, "succeeded"), done(40030, "succeeded")]);
    const ctx = mkCtx(repo);
    const gh = mkGh(GH_DRAFT);
    registerBuildCommand(pi, { run: seams.run, fetchFeed: mkFetch(LAB), ghFetch: gh.gh, repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "release"], ctx);
    await sleep(400);
    check("W10: release chain polls def 23 (same pipeline as lab)", seams.calls.some((c) => c.includes("definitions=23")), JSON.stringify(seams.calls));
    check("W10: release nugets asserted on nuget.org (normalized version)", ctx._notifies.some((n) => n.msg.includes("Nugets published") && n.msg.includes("nuget.org")), JSON.stringify(ctx._notifies));
    check("W10: release draft published as a FULL release + chain complete", ctx._notifies.some((n) => n.msg.includes("GitHub release 4.261.2.1 published from draft") && n.msg.includes("chain complete")) && gh.patches.length === 1 && JSON.parse(gh.patches[0]).prerelease === false, JSON.stringify(ctx._notifies) + " | " + JSON.stringify(gh.patches));
    check("W10: no steer on success", pi._userMessages.length === 0, JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    const savedToken = process.env.GH_TOKEN;
    const savedAlt = process.env.GITHUB_TOKEN;
    delete process.env.GH_TOKEN;
    delete process.env.GITHUB_TOKEN;
    try {
      registerBuildCommand(pi, { run: mkSeams(GREEN).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
      await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
      await sleep(400);
      check("W11: missing token → warning + steer naming GH_TOKEN", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("GH_TOKEN is not set")) && pi._userMessages.length === 1 && pi._userMessages[0].content.includes("GH_TOKEN"), JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
      check("W11: watcher stopped", !isAzDoWatcherActive());
    } finally {
      if (savedToken !== undefined) process.env.GH_TOKEN = savedToken;
      if (savedAlt !== undefined) process.env.GITHUB_TOKEN = savedAlt;
    }
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, { run: mkSeams([empty(), ...GREEN]).run, fetchFeed: mkFetch(LAB), ghFetch: mkGh(GH_DRAFT).gh, repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(400);
    check("W12: empty poll retried (no build found yet), chain completed, no give-up", ctx._notifies.some((n) => n.msg.includes("no build found yet")) && ctx._notifies.some((n) => n.msg.includes("chain complete")) && !isAzDoWatcherActive(), JSON.stringify(ctx._notifies));
    check("W12: no steer on success", pi._userMessages.length === 0, JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    const fetchThrow = async (url: string) => {
      if (url.includes("api.nuget.org")) throw new Error("404");
      return JSON.stringify({ feed: oDataFeed(LAB) });
    };
    registerBuildCommand(pi, { run: mkSeams(GREEN).run, fetchFeed: fetchThrow, ghFetch: mkGh(GH_DRAFT).gh, repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "release"], ctx);
    await sleep(400);
    check("W13: release nugets missing on nuget.org → warning + steer, chain continues", ctx._notifies.some((n) => n.type === "warning" && n.msg.includes("nuget.org")) && pi._userMessages.length === 1 && ctx._notifies.some((n) => n.msg.includes("release consumers pipeline")), JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
  }
  {
    const repo = mkExpandRepo();
    const pi = mkPi();
    const seams = mkSeams([done(35805, "succeeded"), done(35806, "succeeded"), done(35807, "succeeded")]);
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: seams.run, fetchFeed: mkFetch(["26.1.400"]), ghFetch: mkGh(GH_EXPAND).gh, repoRoot: repo,
      profile: expandProfile, startAzDoWatcher: fastWatcher,
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(400);
    check("W14: expand lab polls def 94", seams.calls.some((c) => c.includes("definitions=94")), JSON.stringify(seams.calls));
    check("W14: 26.1.400.0 matches feed 26.1.400 — nugets confirmed, no steer", ctx._notifies.some((n) => n.msg.includes("Nugets published") && n.msg.includes("eXpandSystem")) && pi._userMessages.length === 0, JSON.stringify(ctx._notifies) + " | " + JSON.stringify(pi._userMessages));
    check("W14: GitHub already published + chain complete", ctx._notifies.some((n) => n.msg.includes("GitHub release 26.1.400.0 already published") && n.msg.includes("chain complete")), JSON.stringify(ctx._notifies));
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    registerBuildCommand(pi, { run: mkSeams([failedWithLog(35802, ["##[error]Release 26.1.301.1 exists"])]).run, fetchFeed: mkFetch(LAB), repoRoot: repo, startAzDoWatcher: fastWatcher });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], mkCtx(repo));
    await sleep(200);
    check("W15: failure steer carries the log error, not no-error-lines", pi._userMessages.length === 1 && pi._userMessages[0].content.includes("Release 26.1.301.1 exists") && !pi._userMessages[0].content.includes("no error lines"), JSON.stringify(pi._userMessages));
    check("W15: watcher stopped", !isAzDoWatcherActive());
  }
  {
    const repo = mkRepo();
    const pi = mkPi();
    const ctx = mkCtx(repo);
    registerBuildCommand(pi, {
      run: mkSeams([doneNum(25698, "succeeded", "22.1.601.2")]).run, fetchFeed: mkFetch(LAB), repoRoot: repo,
      startAzDoWatcher: (p: any, c: any, s: any, opts?: any) => startAzDoWatcher(p, c, s, { ...opts, intervalMs: 10, maxMs: 80 }),
    });
    await pi._cmds.get("devexpress").handler(["publish", "lab"], ctx);
    await sleep(250);
    check("W16: wrong-version toast, no chain advance", ctx._notifies.some((n) => n.msg.includes("is not this run")) && !ctx._notifies.some((n) => n.msg.includes("release consumers pipeline")), JSON.stringify(ctx._notifies));
    check("W16: give-up steers + watcher stopped", pi._userMessages.length === 1 && pi._userMessages[0].content.includes("gave up") && !isAzDoWatcherActive(), JSON.stringify(pi._userMessages));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
