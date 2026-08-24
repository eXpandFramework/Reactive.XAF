/**
 * reactive-xaf-build/azdo — AzDO build monitor + status for the publish step.
 *
 * waitForAzDoBuild polls the newest Reactive.XAF build until its status leaves
 * inProgress/notStarted. prx cancels in-progress builds before queueing
 * (Publish-ReactiveXAF in eXpandFramework.psm1), so the newest build is always
 * ours. On failure the failed timeline record's log is fetched and the last
 * ~3 "##[error]" lines become the reason. azdoStatusScript is the one-shot
 * variant — same extraction, no polling.
 *
 * One pwsh spawn per query: the profile loads Get-AzBuilds +
 * Invoke-AzureRestMethod (XpandPwsh, on PSModulePath) and sets
 * $env:AzProject / $env:AzOrganization. The scripts print a single RESULT= /
 * STATUS= line and exit.
 *
 * Failure policy (agent behavior): on an AzDO failure the agent PLANS a fix
 * and presents it — user permission is ALWAYS required before any action.
 * No auto-fix, no auto re-run.
 */

import { runArgv } from "./pane.js";

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

const POLL_MS = 30000;
const KNOWN_RESULTS = new Set(["succeeded", "failed", "canceled", "other"]);

/** PS: fetch the failed timeline record's log and extract the ##[error] lines
 *  into $reason (shared by the monitor and the status script). */
const REASON_BLOCK = `
  $cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
  try {
    $t = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/timeline") @cred
    $rec = $t.records | Where-Object { $_.result -eq "failed" -and $_.log.id } | Select-Object -First 1
    if ($rec) {
      $log = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs/" + $rec.log.id) @cred
      $lines = @($log | ForEach-Object { $_ -split "\\r?\\n" })
      $errs = @($lines | Where-Object { $_ -match "##\\[error\\]" } | Select-Object -Last 3 | ForEach-Object { $_ -replace "^.*?##\\[error\\]\\s*", "" })
      if ($errs.Count -gt 0) { $reason = $errs -join " | " }
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
if ($b.result -eq "failed") {${REASON_BLOCK}
}
"RESULT=$($b.id);$($b.result);$reason"`;
}

/** The one-shot status script: newest build, current state, reason on failure. */
export function azdoStatusScript(): string {
  return `$b = Get-AzBuilds -Definition Reactive.XAF -Top 1
if ($null -eq $b) { "STATUS=0;none;none;"; exit 0 }
$reason = ""
if ($b.result -eq "failed") {${REASON_BLOCK}
}
"STATUS=$($b.id);$($b.status);$($b.result);$reason"`;
}

/** Parse the script's last RESULT= line; null when the script produced none. */
function parseOutcome(stdout: string): AzDoBuildOutcome | null {
  let line = "";
  for (const l of stdout.split("\n")) {
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

/** Parse the status script's last STATUS= line; null when none was printed. */
export function parseStatus(stdout: string): AzDoStatus | null {
  let line = "";
  for (const l of stdout.split("\n")) {
    if (l.startsWith("STATUS=")) line = l;
  }
  if (!line) return null;
  const m = line.match(/^STATUS=([^;]*);([^;]*);([^;]*);(.*)$/);
  if (!m) return null;
  return { id: Number(m[1]), status: m[2], result: m[3], reason: m[4].trim() };
}

/** Poll the newest Reactive.XAF build until it finishes (default seam). */
export async function defaultWaitForAzDoBuild(timeoutMs: number): Promise<AzDoBuildOutcome> {
  const res = await runArgv(["pwsh", "-Command", monitorScript(timeoutMs)], timeoutMs + POLL_MS + 15000);
  const outcome = parseOutcome(res.stdout);
  if (outcome) return outcome;
  const err = (res.stderr || "no RESULT= line in monitor output").trim().slice(-500);
  return { id: 0, result: "other", reason: `monitor failed: ${err}` };
}
