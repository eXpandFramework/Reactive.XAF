/**
 * reactive-xaf-build/azdo — AzDO status + cancel scripts for the publish step.
 *
 * The one-shot status script (azdoStatusScript) queries the newest build of a
 * definition by QUEUE TIME (queryOrder=queueTimeDescending) — the module's
 * Get-AzBuilds omits the ordering, and the API default buries in-progress
 * builds, so a bare -Top 1 returned the newest COMPLETED build instead of the
 * just-queued one (2026-08-25 fix). The optional minId filters out builds the
 * chain has already passed (the next pipeline's build is the newest with
 * id > the previous pipeline's build id). On a failed build, the failed
 * timeline record's log is fetched and printed between LOGSTART/LOGEND
 * markers (bounded to the last 500 lines); extractFailReason scores it for
 * the ONE real error, filtering wrapper noise (ScriptHalted,
 * ##[error]Exception, "PowerShell exited with code", retry lines) and
 * attaching the nearest "Executing <task>" line as context.
 *
 * The cancel script (cancelAzDoScript) PATCHes {"status":"cancelling"} on
 * EVERY running/queued build of the project — the documented cancel; DELETE
 * is rejected by the API on running builds
 * (CannotDeleteRunningBuildException), which silently broke prx's cancel
 * and left zombie builds holding the pool (2026-08-25, fixed in XpandPwsh
 * Remove-AzBuild).
 *
 * One pwsh spawn per query: the profile loads Invoke-AzureRestMethod
 * (XpandPwsh, on PSModulePath) and sets $env:AzProject / $env:AzOrganization.
 * Scripts print a single STATUS= / CANCEL= line and exit.
 *
 * The publish chain runs on ONE pipeline — the Reactive.XAF definition
 * (def 23); Lab builds queue branch lab, Release (prx -Release) queues
 * branch master. There is no separate Release definition (the def-39
 * premise was wrong — the chain's PublishNugets trigger listens to 23).
 *
 * defaultGhFetch is the GitHub API fetch with Authorization from
 * GH_TOKEN / GITHUB_TOKEN when set (drafts are invisible to unauthenticated
 * callers). The token is never logged.
 */

export type AzDoBuildResult = "succeeded" | "failed" | "canceled" | "other";

export interface AzDoStatus {
  id: number;
  status: string;
  result: string;
  reason: string;
}

export interface AzDoCancel {
  id: number;
  ok: boolean;
  status: string;
}

export const LAB_DEF = "23";

export function azdoBuildUrl(definition: string): string {
  return `https://dev.azure.com/eXpandDevOps/eXpandFramework/_build?definitionId=${definition}`;
}

export const AZDO_BUILD_URL = azdoBuildUrl(LAB_DEF);

/** Wrapper markers that carry no real error: psake/AzDO noise + retry chatter. */
const WRAPPER_NOISE = /ScriptHalted|Approve-LastExitCode|##\[error\]Exception|PowerShell exited with code|The term '|Retrying in/;
/** Actionable compiler/MSBuild/DX failures (tier 1 — beats the generic marker). */
const REAL_ERROR = /CSC : error\b|\berror (?:CS|MSB|DX|NU)\d+\b|\berror :\b/;
/** Generic build-level failure marker (tier 2 — used only when no tier-1 line). */
const BUILD_FAILED = /Build FAILED/i;

/** PS: fetch the failed timeline record's log and print a bounded tail between
 *  LOGSTART/LOGEND markers for TS-side extraction; $reason carries only the
 *  fetch-failure fallback. */
const LOG_BLOCK = `
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

/** The one-shot status script: newest build of a definition, current state,
 *  reason on failure. definitions defaults to 23 (Reactive.XAF); minId
 *  filters out builds the chain already passed (id <= minId). */
export function azdoStatusScript(definitions = LAB_DEF, minId = 0): string {
  return `$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
$b = (Invoke-AzureRestMethod 'build/builds?definitions=${definitions}&$top=5&queryOrder=queueTimeDescending' @cred) | Where-Object { $_.id -gt ${minId} } | Select-Object -First 1
if ($null -eq $b) { "STATUS=0;none;none;"; exit 0 }
$reason = ""
if ($b.result -eq "failed") {${LOG_BLOCK}
}
"STATUS=$($b.id);$($b.status);$($b.result);$reason"`;
}

/** The project-wide cancel script: PATCH-cancel EVERY running/queued build of
 *  the project (statusFilter=inProgress,notStarted,postponed, no definition
 *  filter), so "Cancel AzDO build" stops the whole chain at once. CANCEL=
 *  carries the first canceled id and the total count. */
export function cancelAzDoScript(): string {
  return `$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
$bs = Invoke-AzureRestMethod 'build/builds?statusFilter=inProgress,notStarted,postponed&$top=100' @cred
$running = @($bs | Where-Object { $_.status -in @("inProgress","notStarted","postponed") })
if ($running.Count -eq 0) { "CANCEL=0;none;none"; exit 0 }
foreach ($b in $running) {
  Invoke-AzureRestMethod ("build/builds/" + $b.id) -Method Patch -Body '{"status":"cancelling"}' @cred | Out-Null
}
"CANCEL=$($running[0].id);ok;$($running.Count)"`;
}

/** Default GitHub API fetch: Authorization from GH_TOKEN / GITHUB_TOKEN when
 *  set (GitHub drafts are only returned to authenticated callers), plus the
 *  required User-Agent. The token is never logged. */
export async function defaultGhFetch(url: string, opts: { method?: string; body?: string } = {}): Promise<{ ok: boolean; status: number; text: string }> {
  const token = process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN;
  const headers: Record<string, string> = { "User-Agent": "rxaf-watcher", Accept: "application/vnd.github+json" };
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await globalThis.fetch(url, { method: opts.method ?? "GET", headers, body: opts.body });
  return { ok: res.ok, status: res.status, text: await res.text() };
}

/** Parse the status script's last STATUS= line; null when none was printed.
 *  CRLF — split on /\r?\n/ so the trailing \r never reaches the regex (a bare
 *  \n split silently broke every real parse; 2026-08-25 fix, contract:
 *  azdo-tests.ts). */
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

/** Parse the cancel script's last CANCEL= line; null when none was printed. */
export function parseCancel(stdout: string): AzDoCancel | null {
  let line = "";
  for (const l of stdout.split(/\r?\n/)) {
    if (l.startsWith("CANCEL=")) line = l;
  }
  if (!line) return null;
  const m = line.match(/^CANCEL=([^;]*);([^;]*);([^;]*)$/);
  if (!m) return null;
  return { id: Number(m[1]), ok: m[2] === "ok", status: m[3] };
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
