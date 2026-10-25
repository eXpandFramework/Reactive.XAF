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
 * THE QUERY IS CLASSIFIED, NOT GUESSED. A refused or expired token used to be
 * written as an error while the script CARRIED ON: `$b` stayed null,
 * `STATUS=0;none;none;` was printed with exit 0, so a dead PAT read as an empty
 * pipeline to every caller and the watch sat on nothing until its deadline
 * (2026-10-25). The call now sits in a try/catch that prints
 * `STATUS=ERR;<auth|transient>;<message>`: auth is the one failure no retry
 * clears, everything else (a 429, a blip, a proxy) keeps the chain retrying.
 * A script-wide `$ErrorActionPreference = 'Stop'` was rejected for exactly that
 * reason — it made a transient blip indistinguishable from a dead token, and
 * the difference was then guessed by matching error prose downstream.
 *
 * The cancel script (cancelAzDoScript) PATCHes {"status":"cancelling"} on
 * EVERY running/queued build of the project — the documented cancel; DELETE
 * is rejected by the API on running builds
 * (CannotDeleteRunningBuildException), which silently broke prx's cancel
 * and left zombie builds holding the pool (2026-08-25, fixed in XpandPwsh
 * Remove-AzBuild). It reports `CANCEL=ERR;<auth|transient>;<message>` the same
 * way.
 *
 * One pwsh spawn per query: the profile loads Invoke-AzureRestMethod
 * (XpandPwsh, on PSModulePath) and sets $env:AzProject / $env:AzOrganization.
 * Scripts print a single STATUS= / CANCEL= line and exit. STATUS is
 * `id;status;result;buildNumber;reason` (the parser also accepts the older
 * 4-field line with no buildNumber).
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
  buildNumber: string;
  reason: string;
  /** Set while the build is in flight: the task it is inside and when that task
   *  last wrote a log line. Absent when the script could not tell. */
  progress?: BuildProgress;
}

/** Where an in-flight build actually is. `lastOutputAt` is the ONLY stall
 *  signal AzDO offers: its `status` field reads `inProgress` for a hung job
 *  too. 2026-10-25 build 36111 — `Tests Workflow` wrote nothing for 80 minutes
 *  while every status field said the build was fine. */
export interface BuildProgress {
  activeName: string;
  activeSince: number | null;
  lastOutputAt: number | null;
}

/** Why a call could not be read. `auth` is final (renew the token); anything
 *  else is worth another poll. */
export type AzDoFailure = "auth" | "transient";

/** One poll, classified. `null` from parsePoll means the script printed no
 *  STATUS= line at all — a crash, not a failure it could classify. */
export type AzDoPoll =
  | { kind: "empty" }
  | { kind: "build"; build: AzDoStatus }
  | { kind: "error"; failure: AzDoFailure; message: string };

export interface AzDoCancel {
  id: number;
  ok: boolean;
  status: string;
  /** Set when the cancel script reached AzDO and was refused. */
  failure?: AzDoFailure;
  message?: string;
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

/** PS: the catch body that classifies a refused call. The message is flattened
 *  first — a newline or a `;` inside it would break the field protocol the TS
 *  side parses. `auth` is matched on the server's own wording. */
function errBlock(kind: "STATUS" | "CANCEL"): string {
  return [
    `  $m = "$($_.Exception.Message)" -replace "\\r?\\n", " " -replace ";", ","`,
    `  $k = if ($m -match '401|Unauthorized|Access Denied|expired') { 'auth' } else { 'transient' }`,
    `  "${kind}=ERR;$k;$m"`,
  ].join("\n");
}

/** PS: fetch the failed TASK record's log (the Job record's log is a summary
 *  without the real error) and print a bounded tail between LOGSTART/LOGEND
 *  markers for TS-side extraction; $reason carries only the fetch-failure
 *  fallback. */
const LOG_BLOCK = `
  try {
    $t = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/timeline") @cred
    $rec = $t.records | Where-Object { $_.type -eq "Task" -and $_.result -eq "failed" -and $_.log.id } | Select-Object -First 1
    if ($rec) {
      $log = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs/" + $rec.log.id) @cred
      "LOGSTART"
      @($log -split "\\r?\\n") | Select-Object -Last 500
      "LOGEND"
    }
  } catch {
    $reason = "log fetch failed: " + $_.Exception.Message
  }`;

/** PS: while the build is in flight, name the task it is inside and the
 *  timestamp of that task's LAST log line. A job that stays `inProgress` while
 *  it writes nothing is the hang no status field shows. PROGRESS=0 means the
 *  timeline could not be read: silence about a task is never evidence that it
 *  is stalled. */
const PROGRESS_BLOCK = `
if ($b.status -in @("inProgress","cancelling")) {
  try {
    $t = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/timeline") @cred -ErrorAction Stop
    $a = $t.records | Where-Object { $_.type -eq "Task" -and $_.state -eq "inProgress" -and $_.log.id } | Sort-Object startTime | Select-Object -First 1
    if ($a) {
      $st = if ($a.startTime -is [datetime]) { $a.startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } else { "$($a.startTime)" }
      $p = $t.records | Where-Object { $_.id -eq $a.parentId } | Select-Object -First 1
      $name = if ($p) { "$($p.name) / $($a.name)" } else { "$($a.name)" }
      $last = ""
      try {
        $lg = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs/" + $a.log.id) @cred -ErrorAction Stop
        $last = (@($lg -split "\\r?\\n") | Where-Object { $_ } | Select-Object -Last 1)
      } catch { }
      $ts = if ($last) { ($last -split " ")[0] } else { "" }
      "PROGRESS=1;" + ($name -replace ";", ",") + ";" + $st + ";" + $ts
    } else { "PROGRESS=0;;;" }
  } catch { "PROGRESS=0;;;" }
}`;

/** The one-shot status script: newest build of a definition, current state,
 *  reason on failure. definitions defaults to 23 (Reactive.XAF); minId
 *  filters out builds the chain already passed (id <= minId). A refused call
 *  prints the classified ERR line instead of an empty pipeline. */
export function azdoStatusScript(definitions = LAB_DEF, minId = 0): string {
  return `$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
try {
  $b = (Invoke-AzureRestMethod 'build/builds?definitions=${definitions}&$top=5&queryOrder=queueTimeDescending' @cred -ErrorAction Stop) | Where-Object { $_.id -gt ${minId} } | Select-Object -First 1
} catch {
${errBlock("STATUS")}
  exit 0
}
if ($null -eq $b) { "STATUS=0;none;none;"; exit 0 }
${PROGRESS_BLOCK}
$reason = ""
if ($b.result -eq "failed") {${LOG_BLOCK}
}
"STATUS=$($b.id);$($b.status);$($b.result);$($b.buildNumber);$reason"`;
}

/** The project-wide cancel script: PATCH-cancel EVERY running/queued build of
 *  the project (statusFilter=inProgress,notStarted,postponed, no definition
 *  filter), so "Cancel AzDO build" stops the whole chain at once. CANCEL=
 *  carries the first canceled id and the total count. A refused call prints
 *  the classified ERR line instead of reading as "nothing to cancel". */
export function cancelAzDoScript(): string {
  return `$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
try {
  $bs = Invoke-AzureRestMethod 'build/builds?statusFilter=inProgress,notStarted,postponed&$top=100' @cred -ErrorAction Stop
  $running = @($bs | Where-Object { $_.status -in @("inProgress","notStarted","postponed") })
  if ($running.Count -eq 0) { "CANCEL=0;none;none"; exit 0 }
  foreach ($b in $running) {
    Invoke-AzureRestMethod ("build/builds/" + $b.id) -Method Patch -Body '{"status":"cancelling"}' @cred -ErrorAction Stop | Out-Null
  }
  "CANCEL=$($running[0].id);ok;$($running.Count)"
} catch {
${errBlock("CANCEL")}
}`;
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

/** The last `<kind>=` line of a script's output, or "" when none was printed. */
function lastLine(stdout: string, kind: "STATUS" | "CANCEL" | "PROGRESS"): string {
  let line = "";
  for (const l of stdout.split(/\r?\n/)) {
    if (l.startsWith(`${kind}=`)) line = l;
  }
  return line;
}

/** The classified failure a line carries, or null when it is a normal line. */
function classifiedFailure(line: string, kind: "STATUS" | "CANCEL"): { failure: AzDoFailure; message: string } | null {
  const m = line.match(new RegExp(`^${kind}=ERR;(auth|transient);(.*)$`));
  if (!m) return null;
  return { failure: m[1] as AzDoFailure, message: m[2].trim() };
}

/** The build a STATUS= line carries, or null when the line is malformed.
 *  CRLF — the caller splits on /\r?\n/ so the trailing \r never reaches the
 *  regex (a bare \n split silently broke every real parse; 2026-08-25 fix,
 *  contract: azdo-tests.ts). Five-field
 *  (id;status;result;buildNumber;reason) first; four-field (no buildNumber)
 *  still parses so existing fixtures keep working. */
function buildFromLine(line: string): AzDoStatus | null {
  const five = line.match(/^STATUS=([^;]*);([^;]*);([^;]*);([^;]*);(.*)$/);
  if (five) {
    return { id: Number(five[1]), status: five[2], result: five[3], buildNumber: five[4], reason: five[5].trim() };
  }
  const four = line.match(/^STATUS=([^;]*);([^;]*);([^;]*);(.*)$/);
  if (!four) return null;
  return { id: Number(four[1]), status: four[2], result: four[3], buildNumber: "", reason: four[4].trim() };
}

/** The in-flight progress a PROGRESS= line carries, or undefined when the script
 *  said it could not tell (PROGRESS=0, or no line at all). A field that does not
 *  parse to a time is null, never zero: an unknown time must never read as a
 *  very old one. */
function progressFromStdout(stdout: string): BuildProgress | undefined {
  const m = lastLine(stdout, "PROGRESS").match(/^PROGRESS=1;([^;]*);([^;]*);([^;]*)$/);
  if (!m) return undefined;
  const at = (s: string): number | null => {
    const t = Date.parse(s);
    return Number.isFinite(t) ? t : null;
  };
  return { activeName: m[1].trim(), activeSince: at(m[2]), lastOutputAt: at(m[3]) };
}

/** One poll, classified: a build, an empty pipeline, or the reason the call
 *  could not be read. null means the script printed NO STATUS= line — a crash
 *  nothing can classify, and the caller reports the stderr it has. */
export function parsePoll(stdout: string): AzDoPoll | null {
  const line = lastLine(stdout, "STATUS");
  if (!line) return null;
  const failed = classifiedFailure(line, "STATUS");
  if (failed) return { kind: "error", failure: failed.failure, message: failed.message };
  const build = buildFromLine(line);
  if (!build) return null;
  if (build.id === 0) return { kind: "empty" };
  build.progress = progressFromStdout(stdout);
  return { kind: "build", build };
}

/** Parse the cancel script's last CANCEL= line; null when none was printed. */
export function parseCancel(stdout: string): AzDoCancel | null {
  const line = lastLine(stdout, "CANCEL");
  if (!line) return null;
  const failed = classifiedFailure(line, "CANCEL");
  if (failed) return { id: 0, ok: false, status: "", failure: failed.failure, message: failed.message };
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
