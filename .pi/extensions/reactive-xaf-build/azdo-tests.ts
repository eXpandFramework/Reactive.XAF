/**
 * reactive-xaf-build/azdo-tests — behavior contract for the AzDO monitor's
 * parse path against REAL pwsh-shaped output (CRLF line endings).
 *
 * The monitor/status scripts print RESULT=/STATUS= lines with \r\n (pwsh
 * pipe output). parseOutcome/parseStatus split on /\r?\n/ — a regression to a
 * bare \n split leaves \r on every line and the (.*)$ regex cannot cross a
 * line terminator, so every real run reports "no RESULT= line in monitor
 * output" (fixed 2026-08-25). These tests drive defaultWaitForAzDoBuild and
 * the status command with CRLF fixtures and assert the parsed outcomes.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { registerBuildCommand } from "./build.js";
import { defaultWaitForAzDoBuild } from "./azdo.js";

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
  const root = mkdtempSync(join(tmpdir(), "rxaf-azdo-"));
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  writeFileSync(join(root, "Directory.Packages.props"), "<Project />");
  return root;
}
function crlf(lines: string[]): string {
  return lines.join("\r\n") + "\r\n";
}
function mkPi(): any {
  const cmds = new Map<string, any>();
  return {
    registerCommand: (n: string, d: any) => {
      cmds.set(n, d);
    },
    _cmds: cmds,
  };
}

(async () => {
  // Section: T1 — failed build: CRLF RESULT= + LOG block parses with reason
  {
    const logLines = ["Executing Compile", "CSC : error DX1003: Expired license key version", "##[error]PowerShell exited with code '1'"];
    const out = await defaultWaitForAzDoBuild(60000, async () => ({
      code: 0,
      stdout: crlf(["LOGSTART", ...logLines, "LOGEND", "RESULT=35742;failed;"]),
      stderr: "",
    }));
    check("failed outcome parsed from CRLF output", out.id === 35742 && out.result === "failed", JSON.stringify(out));
    check("fail reason extracted from the CRLF log", out.reason.includes("DX1003"), JSON.stringify(out));
  }
  // Section: T2 — canceled / succeeded / timeout CRLF outcomes parse
  {
    const canceled = await defaultWaitForAzDoBuild(60000, async () => ({ code: 0, stdout: crlf(["RESULT=35741;canceled;"]), stderr: "" }));
    check("canceled parsed from CRLF", canceled.id === 35741 && canceled.result === "canceled", JSON.stringify(canceled));
    const okBuild = await defaultWaitForAzDoBuild(60000, async () => ({ code: 0, stdout: crlf(["RESULT=35459;succeeded;"]), stderr: "" }));
    check("succeeded parsed from CRLF", okBuild.id === 35459 && okBuild.result === "succeeded", JSON.stringify(okBuild));
    const timed = await defaultWaitForAzDoBuild(60000, async () => ({ code: 0, stdout: crlf(["RESULT=timeout;;"]), stderr: "" }));
    check("timeout parsed from CRLF", timed.id === 0 && timed.result === "other" && timed.reason === "timeout", JSON.stringify(timed));
  }
  // Section: T3 — command registered via real boot; status parses CRLF output
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("devexpress command registered via index.ts", typeof cmd?.handler === "function");
    const repo = mkRepo();
    const pi2 = mkPi();
    registerBuildCommand(pi2, {
      run: async () => ({ code: 0, stdout: crlf(["STATUS=35735;completed;failed;Artifact TestAssemblies was not found"]), stderr: "" }),
      fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
      repoRoot: repo,
    });
    const ctx: any = {
      cwd: repo,
      ui: {
        select: async () => "Skip",
        notify: () => {},
      },
    };
    const r = await pi2._cmds.get("devexpress").handler(["status"], ctx);
    check("status shows id + reason from CRLF output", r.includes("35735") && r.includes("Artifact TestAssemblies"), r);
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
