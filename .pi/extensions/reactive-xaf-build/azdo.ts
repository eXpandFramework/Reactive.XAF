/**
 * reactive-xaf-build/azdo — AzDO build monitor + status for the publish step.
 *
 * waitForAzDoBuild polls the newest Reactive.XAF build until its status leaves
 * inProgress/notStarted. prx cancels in-progress builds before queueing
 * (Publish-ReactiveXAF in eXpandFramework.psm1), so the newest build is always
 * ours. On failure the failed timeline record's log is fetched and printed
 * between LOGSTART/LOGEND markers; extractFailReason scores it for the ONE
 * real error, filtering wrapper noise (ScriptHalted, ##[error]Exception,
 * "PowerShell exited with code", retry lines) and attaching the nearest
 * "Executing <task>" line as context. azdoStatusScript is the one-shot
 * variant — same fetch, same TS-side extraction.
 *
 * One pwsh spawn per query: the profile loads Get-AzBuilds +
 * Invoke-AzureRestMethod (XpandPwsh, on PSModulePath) and sets
 * $env:AzProject / $env:AzOrganization. The scripts print a single RESULT= /
 * STATUS= line (the log section before it) and exit.
 *
 * Failure policy (agent behavior): on an AzDO failure the agent PLANS a fix
 * and presents it — user permission is ALWAYS required before any action.
 * No auto-fix, no auto re-run.
 */

import { runArgv } from "./pane.js";
import type { RunResult } from "./pane.js";

export type AzDoBuildResult = "succeeded" | "failed" | "canceled" | "other";

export interface AzDoBuildOutcome {
  id: number;
  result: AzDoBuildResult;
  reason: string;
}

export interface AzDoStatus {
  id: number;
  status: string;
  result: string;
  reason: string;
}

export type AzDoBuildWaiter = (timeoutMs: number) => Promise<AzDoBuildOutcome>;

export const AZDO_BUILD_URL = "https://dev.azure.com/eXpandDevOps/eXpandFramework/_build?definitionId=23";

const KNOWN_RESULTS = new Set(["succeeded", "failed", "canceled", "other"]);

/** Wrapper markers that carry no real error: psake/AzDO noise + retry chatter. */
const WRAPPER_NOISE = /ScriptHalted|Approve-LastExitCode|##\[error\]Exception|PowerShell exited with code|The term '|Retrying in/;
/** Actionable compiler/MSBuild/DX failures (tier 1 — beats the generic marker). */
const REAL_ERROR = /CSC : error\b|\berror (?:CS|MSB|DX|NU)\d+\b|\berror :\b/;
/** Generic build-level failure marker (tier 2 — used only when no tier-1 line). */
const BUILD_FAILED = /Build FAILED/i;

/** PS: fetch the failed timeline record's log and print a bounded tail between
 *  LOGSTART/LOGEND markers for TS-side extraction; $reason carries only the
 *  fetch-failure fallback. Shared by the monitor and the status script. */
const LOG_BLOCK = `
  $cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
  try {
    $t = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/timeline") @cred
    $rec = $t.records | Where-Object { $_.result -eq "failed" -and $_.log.id } | Select-Object -First 1
    if ($rec) {
      $log = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs/" + $rec.log.id) @cred
      "LOGSTART"
      @($log -split "\\r?\\n") | Select-Object -Last 500
      "LOGEND"
    }
  } catch {
    $reason = "log fetch failed: " + $_.Exception.Message
  }`;

/** The poll script; timeoutMs is embedded as the poll deadline. */
function monitorScript(timeoutMs: number): string {
  return `$deadline = (Get-Date).AddMilliseconds(${timeoutMs})
$b = $null
while ((Get-Date) -lt $deadline) {
  $b = Get-AzBuilds -Definition Reactive.XAF -Top 1
  if ($null -eq $b) { break }
  if ($b.status -and $b.status -ne "inProgress" -and $b.status -ne "notStarted" -and $b.status -ne "cancelling") { break }
  Start-Sleep -Seconds 30
}
if ($null -eq $b) { "RESULT=0;other;no AzDO build found"; exit 0 }
if (-not $b.status -or $b.status -eq "inProgress" -or $b.status -eq "notStarted" -or $b.status -eq "cancelling") { "RESULT=timeout;;"; exit 0 }
$reason = ""
if ($b.result -eq "failed") {${LOG_BLOCK}
}
"RESULT=$($b.id);$($b.result);$reason"`;
}

/** The one-shot status script: newest build, current state, reason on failure. */
export function azdoStatusScript(): string {
  return `$b = Get-AzBuilds -Definition Reactive.XAF -Top 1
if ($null -eq $b) { "STATUS=0;none;none;"; exit 0 }
$reason = ""
if ($b.result -eq "failed") {${LOG_BLOCK}
}
"STATUS=$($b.id);$($b.status);$($b.result);$reason"`;
}

/** Parse the script's last RESULT= line; null when the script produced none.
 *  pwsh pipe output is CRLF — split on /\r?\n/ so the trailing \r never
 *  reaches the regex below (a bare \n split silently broke every real parse;
 *  2026-08-25 fix, contract: azdo-tests.ts). */
function parseOutcome(stdout: string): AzDoBuildOutcome | null {
  let line = "";
  for (const l of stdout.split(/\r?\n/)) {
    if (l.startsWith("RESULT=")) line = l;
  }
  if (!line) return null;
  if (line.startsWith("RESULT=timeout")) return { id: 0, result: "other", reason: "timeout" };
  const m = line.match(/^RESULT=([^;]*);([^;]*);(.*)$/);
  if (!m) return null;
  return {
    id: Number(m[1]),
    result: (KNOWN_RESULTS.has(m[2]) ? m[2] : "other") as AzDoBuildResult,
    reason: m[3].trim(),
  };
}

/** Parse the status script's last STATUS= line; null when none was printed.
 *  CRLF — same split as parseOutcome so the STATUS= line parses cleanly. */
export function parseStatus(stdout: string): AzDoStatus | null {
  let line = "";
  for (const l of stdout.split(/\r?\n/)) {
    if (l.startsWith("STATUS=")) line = l;
  }
  if (!line) return null;
  const m = line.match(/^STATUS=([^;]*);([^;]*);([^;]*);(.*)$/);
  if (!m) return null;
  return { id: Number(m[1]), status: m[2], result: m[3], reason: m[4].trim() };
}

/** Lines of the failed record's log between the LOGSTART/LOGEND markers. */
export function failLogFromStdout(stdout: string): string[] {
  const lines = stdout.split("\n");
  const start = lines.findIndex((l) => l.trim() === "LOGSTART");
  let end = -1;
  for (let i = start + 1; i < lines.length; i++) {
    if (lines[i].trim() === "LOGEND") end = i;
  }
  return start >= 0 && end >= start ? lines.slice(start + 1, end) : [];
}

/** Score a failed build's log for the ONE real error: walk backward from the
 *  wrapper noise at the end. Tier-1 compiler/MSBuild/DX lines beat the generic
 *  "Build FAILED" marker; identical retry lines dedupe to their first
 *  occurrence; the nearest "Executing <task>" line above becomes context.
 *  Falls back to the last ##[error] lines when nothing real is found. */
export function extractFailReason(lines: string[]): string {
  interface Cand { text: string; lastIdx: number; task: string }
  const tier1 = new Map<string, Cand>();
  const tier2 = new Map<string, Cand>();
  const fallback: string[] = [];
  let executing = "";
  for (let i = 0; i < lines.length; i++) {
    const raw = lines[i];
    if (raw.startsWith("Executing ")) executing = raw.replace(/^Executing\s+/, "").trim();
    if (/##\[error\]/.test(raw)) {
      fallback.push(raw.replace(/^.*?##\[error\]\s*/, ""));
      if (fallback.length > 3) fallback.shift();
    }
    if (WRAPPER_NOISE.test(raw)) continue;
    const line = raw.replace(/^.*?##\[error\]\s*/, "");
    if (!line.trim()) continue;
    const key = line.trim().toLowerCase();
    const bag = REAL_ERROR.test(line) ? tier1 : BUILD_FAILED.test(line) ? tier2 : null;
    if (!bag) continue;
    const prev = bag.get(key);
    if (prev) prev.lastIdx = i;
    else bag.set(key, { text: line, lastIdx: i, task: executing });
  }
  const pick = (bag: Map<string, Cand>): Cand | null => {
    let best: Cand | null = null;
    for (const c of bag.values()) {
      if (!best || c.lastIdx > best.lastIdx) best = c;
    }
    return best;
  };
  const err = pick(tier1) ?? pick(tier2);
  const text = err ? err.text : fallback.join(" | ");
  if (!text) return "";
  return err && err.task ? `${text} [${err.task}]`.slice(0, 600) : text.slice(0, 600);
}

/** Poll the newest Reactive.XAF build until it finishes (default seam).
 *  The spawn timeout carries a 5-minute margin over the script's own deadline
 *  so pwsh boot overhead and the final RESULT= print never lose the race to
 *  the taskkill. */
export async function defaultWaitForAzDoBuild(timeoutMs: number, run: (argv: string[], timeoutMs: number) => Promise<RunResult> = runArgv): Promise<AzDoBuildOutcome> {
  const res = await run(["pwsh", "-Command", monitorScript(timeoutMs)], timeoutMs + 300000);
  const outcome = parseOutcome(res.stdout);
  if (outcome) {
    if (outcome.result === "failed" && !outcome.reason) {
      outcome.reason = extractFailReason(failLogFromStdout(res.stdout));
    }
    return outcome;
  }
  const err = (res.stderr || "no RESULT= line in monitor output").trim().slice(-500);
  return { id: 0, result: "other", reason: `monitor failed: ${err}` };
}
