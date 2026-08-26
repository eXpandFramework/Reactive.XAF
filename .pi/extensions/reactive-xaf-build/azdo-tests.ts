/**
 * reactive-xaf-build/azdo-tests — behavior contract for the AzDO status +
 * cancel parse path against REAL pwsh-shaped output (CRLF line endings).
 *
 * The status/cancel scripts print STATUS=/CANCEL= lines with \r\n (pwsh
 * pipe output). parseStatus/parseCancel split on /\r?\n/ — a regression to a
 * bare \n split leaves \r on every line and the (.*)$ regex cannot cross a
 * line terminator (2026-08-25 fix; the old monitor's "no RESULT= line"
 * symptom). These tests drive the status and cancel commands through the
 * registered command with CRLF fixtures and assert the surfaced messages.
 * Run: npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { mkdtempSync, mkdirSync, writeFileSync } from "node:fs";
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
function mkCtx(repo: string): any {
  return {
    cwd: repo,
    ui: {
      select: async () => "Skip",
      notify: () => {},
    },
  };
}
function register(repo: string, stdout: string): any {
  const pi = mkPi();
  registerBuildCommand(pi, {
    run: async () => ({ code: 0, stdout, stderr: "" }),
    fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
    repoRoot: repo,
  });
  return pi;
}
/** register with a call-capturing run seam (for definition assertions). */
function registerCapture(repo: string, stdout: string): { pi: any; calls: string[] } {
  const pi = mkPi();
  const calls: string[] = [];
  registerBuildCommand(pi, {
    run: async (cmd: string) => {
      calls.push(cmd);
      return { code: 0, stdout, stderr: "" };
    },
    fetchFeed: async () => JSON.stringify({ versions: ["26.1.3"] }),
    repoRoot: repo,
  });
  return { pi, calls };
}

(async () => {
  // Section: T1 — failed build: CRLF STATUS= + LOG block surfaces id and the real reason
  {
    const repo = mkRepo();
    const logLines = ["Executing Compile", "CSC : error DX1003: Expired license key version", "##[error]PowerShell exited with code '1'"];
    const pi = register(repo, crlf(["LOGSTART", ...logLines, "LOGEND", "STATUS=35735;completed;failed;"]));
    const r = await pi._cmds.get("devexpress").handler(["status"], mkCtx(repo));
    check("T1: status surfaces id + extracted reason from CRLF", r.includes("35735") && r.includes("DX1003"), r);
  }
  // Section: T2 — running builds: cancel requests project-wide cancel (CRLF CANCEL=)
  {
    const repo = mkRepo();
    const pi = register(repo, crlf(["CANCEL=35735;ok;3"]));
    const r = await pi._cmds.get("devexpress").handler(["cancel"], mkCtx(repo));
    check("T2: cancel requested for all builds surfaced from CRLF", r.includes("Cancel requested for 3 AzDO builds"), r);
  }
  // Section: T3 — nothing running: cancel is a no-op note
  {
    const repo = mkRepo();
    const pi = register(repo, crlf(["CANCEL=0;none;none"]));
    const r = await pi._cmds.get("devexpress").handler(["cancel"], mkCtx(repo));
    check("T3: no-builds cancel surfaced", r.includes("No AzDO builds found to cancel"), r);
  }
  // Section: T4 — registration through the real index boot
  {
    const pi = mkPi();
    activate(pi);
    const cmd = pi._cmds.get("devexpress");
    check("T4: devexpress command registered via index.ts", typeof cmd?.handler === "function");
  }
  // Section: T5 — status without args queries the Reactive.XAF definition (23)
  {
    const repo = mkRepo();
    const { pi, calls } = registerCapture(repo, crlf(["STATUS=35735;completed;failed;"]));
    await pi._cmds.get("devexpress").handler(["status"], mkCtx(repo));
    check("T5: plain status keeps def 23", calls.some((c) => c.includes("definitions=23")), JSON.stringify(calls));
  }
  // Section: T6 — cancel is project-wide: no definition filter, statusFilter query
  {
    const repo = mkRepo();
    const { pi, calls } = registerCapture(repo, crlf(["CANCEL=0;none;none"]));
    await pi._cmds.get("devexpress").handler(["cancel"], mkCtx(repo));
    check(
      "T6: cancel queries all builds project-wide",
      calls.some((c) => c.includes("statusFilter=inProgress,notStarted,postponed") && !c.includes("definitions=")),
      JSON.stringify(calls)
    );
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
