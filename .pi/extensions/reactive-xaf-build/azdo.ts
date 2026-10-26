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

import { classifiedFailure, errBlock } from "./azdo-refusal.js";
import type { AzDoFailure } from "./azdo-refusal.js";

/** The refusal grammar is shared with every other AzDO query; re-exported so
 *  an importer of this module keeps one name for the type. */
export type { AzDoFailure } from "./azdo-refusal.js";

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

/** Where a running build's last output stamp came from: the live record's own
 *  log (`task`), or the newest change across EVERY log container (`logs`) when
 *  that record opened none of its own. */
export type ProgressSource = "task" | "logs";

/** What one poll can say about an in-flight build — a discriminated union, so a
 *  consumer switches on a kind instead of on a nullable time: `blind` is a fact
 *  the script stated, not an absent field.
 *
 *  `queue` is `notStarted`: no agent has picked the build up, so nothing can be
 *  produced and nothing is stalled. `running` is the deepest record in flight
 *  (Task, else Phase, else Job) and when it last produced output — 2026-10-25
 *  build 36111's `Tests Workflow` wrote nothing for 80 minutes while every
 *  status field said the build was fine, and build 36112's live `Build` task
 *  carried `log: null` while every container sat unchanged for two hours. */
export type BuildProgress =
  | { kind: "queue"; queuedAt: number | null }
  | { kind: "running"; activeName: string; activeSince: number | null; lastOutputAt: number; source: ProgressSource }
  | { kind: "blind" };

/** The classified failure a PROGRESS=/STATUS= pair can carry is read by
 *  `classifiedFailure` in `azdo-refusal.ts`; this module only switches on it. */

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

/** PS: the record the build is inside now — the deepest one still in flight,
 *  with or without a log of its own. `log.id` is NOT required: build 36112's
 *  live `Build` task reported `log: null` for two hours, and requiring one made
 *  every such build unreadable instead of recognisably silent. */
const ACTIVE_RECORD_BLOCK = `    $st = ""
    $name = ""
    $last = ""
    $src = "task"
    $a = $t.records | Where-Object { $_.type -eq "Task" -and $_.state -eq "inProgress" } | Sort-Object startTime | Select-Object -First 1
    if (-not $a) { $a = $t.records | Where-Object { $_.type -eq "Phase" -and $_.state -eq "inProgress" } | Sort-Object startTime | Select-Object -First 1 }
    if (-not $a) { $a = $t.records | Where-Object { $_.type -eq "Job" -and $_.state -eq "inProgress" } | Sort-Object startTime | Select-Object -First 1 }
    if ($a) {
      $st = if ($a.startTime -is [datetime]) { $a.startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } else { "$($a.startTime)" }
      $p = $t.records | Where-Object { $_.id -eq $a.parentId } | Select-Object -First 1
      $name = if ($p) { "$($p.name) / $($a.name)" } else { "$($a.name)" }
    }`;

/** PS: when the build last produced ANY output. The active task's own log is the
 *  precise answer, but a task that never opened a log still leaves the newest
 *  change across every container — the watermark this build's hang was visible
 *  in. A last line that carries no timestamp (a continuation, a rewritten
 *  buffer) is skipped rather than read as "no output ever". */
const LAST_OUTPUT_BLOCK = `    if ($a -and $a.log.id) {
      try {
        $lg = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs/" + $a.log.id) @cred -ErrorAction Stop
        foreach ($l in (@($lg -split "\\r?\\n") | Where-Object { $_ } | Select-Object -Last 30)) {
          if ($l -match '^(\\d{4}-\\d\\d-\\d\\dT\\d\\d:\\d\\d:\\d\\d(?:\\.\\d+)?Z)') { $last = $matches[1] }
        }
      } catch { }
    }
    if (-not $last) {
      $src = "logs"
      $ls = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/logs") @cred -ErrorAction Stop
      $w = ($ls | Where-Object { $_.lastChangedOn } | Sort-Object lastChangedOn | Select-Object -Last 1).lastChangedOn
      if ($w -is [datetime]) { $last = $w.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } elseif ($w) { $last = "$($w)" }
    }`;

/** PS: while the build is in flight, name the record it is inside and when it
 *  last produced output. A build that stays `inProgress` while nothing is
 *  written is the hang no status field shows. `PROGRESS=queue` says the build
 *  has not started (nothing to read, nothing stalled); `PROGRESS=0` means not
 *  even the log watermark could be read, and silence about a build is never
 *  evidence that it is stalled. */
const PROGRESS_BLOCK = `
if ($b.status -in @("notStarted","postponed")) {
  $q = if ($b.queueTime -is [datetime]) { $b.queueTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } else { "$($b.queueTime)" }
  "PROGRESS=queue;$($b.status);$q"
} elseif ($b.status -in @("inProgress","cancelling")) {
  try {
    $t = Invoke-AzureRestMethod ("build/builds/" + $b.id + "/timeline") @cred -ErrorAction Stop
${ACTIVE_RECORD_BLOCK}
${LAST_OUTPUT_BLOCK}
    if ($last) { "PROGRESS=1;" + ($name -replace ";", ",") + ";" + $st + ";" + $last + ";" + $src } else { "PROGRESS=0;;;" }
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

/** The reading a PROGRESS= line carries. A time that does not parse degrades to
 *  `blind`, never to a very old reading; a missing line is `blind` too. A queued
 *  line carries no output time at all. A 4-field running line predates `source`
 *  and is a `task` reading — the default the wire documents. */
function progressFromStdout(stdout: string): BuildProgress {
  const line = lastLine(stdout, "PROGRESS");
  const at = (s: string): number | null => {
    const t = Date.parse(s);
    return Number.isFinite(t) ? t : null;
  };
  const queued = line.match(/^PROGRESS=queue;[^;]*;(.*)$/);
  if (queued) return { kind: "queue", queuedAt: at(queued[1]) };
  const m = line.match(/^PROGRESS=1;([^;]*);([^;]*);([^;]*)(?:;([^;]*))?$/);
  if (!m) return { kind: "blind" };
  const lastOutputAt = at(m[3]);
  if (lastOutputAt === null) return { kind: "blind" };
  return { kind: "running", activeName: m[1].trim(), activeSince: at(m[2]), lastOutputAt, source: m[4] === "logs" ? "logs" : "task" };
}

/** The wire line a reading travels in. Exported so the watcher's tests build
 *  their fixtures through the SAME grammar the parser reads — only a verbatim
 *  capture is typed by hand — instead of re-inventing the format a third time
 *  beside the script and the parser. */
export function renderProgressLine(p: BuildProgress): string {
  if (p.kind === "blind") return "PROGRESS=0;;;";
  const iso = (t: number | null): string => (t === null ? "" : new Date(t).toISOString());
  if (p.kind === "queue") return `PROGRESS=queue;notStarted;${iso(p.queuedAt)}`;
  return `PROGRESS=1;${p.activeName};${iso(p.activeSince)};${iso(p.lastOutputAt)};${p.source}`;
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
