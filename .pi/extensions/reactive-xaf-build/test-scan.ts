/**
 * reactive-xaf-build/test-scan — the AzDO test scan: its script and its parser.
 *
 * Split out of `azdo.ts`, which sits at the 400-line cap, and never folded into
 * `azdo-tests.ts`, which is that module's SUITE and no place to hide an
 * implementation. The refusal grammar is not re-invented here: a dead token
 * prints the same `TEST=ERR;<auth|transient>;<msg>` line because it comes from
 * `azdo.ts`'s own `errBlock`, and is read back by its own `classifiedFailure`.
 *
 * WHY LOGS AND NOT RESULTS. The pipeline retries every VSTest task twice
 * (`retryCountOnTaskFailure: 2`, Build/Pipeline.yml) and marks the job
 * `continueOnError`, so the timeline only admits a failure after the retries
 * are spent — precisely the latency this scan exists to remove. The script's
 * line filter is therefore deliberately LOOSE (`Failed `, never `Failed!`): the
 * grammar of a test name lives in `failedTestNames`, where it is testable, and
 * the script stays a fetcher.
 *
 * COST IS BOUNDED BY A WATERMARK. The caller passes the newest log change the
 * previous scan saw; a test record whose log has not moved since costs nothing.
 * At most 12 records are inspected per call, so a twenty-leg matrix cannot turn
 * one poll into twenty log fetches forever.
 */

import { classifiedFailure, errBlock } from "./azdo-refusal.js";
import type { AzDoFailure } from "./azdo-refusal.js";

/** The record name the test scan reads first — build 36111's own (`azdo.md`). */
export const TEST_RECORD = "Tests Workflow / Run Tests";

/** One candidate failure line: the record whose log carried it, when that log
 *  last changed, and the raw line. The grammar is the script's TESTLINE= line. */
export interface TestFailLine {
  record: string;
  at: string;
  line: string;
}

/** One test scan, classified. `none` is a build with no test record in flight —
 *  a fact, not a failure to read; `error` carries the same auth/transient split
 *  the status query uses. `watermark` is the newest log change the scan saw and
 *  is what the next call passes as `since` (null when nothing was readable). */
export interface TestFailPoll {
  kind: "lines" | "none" | "error";
  lines: TestFailLine[];
  watermark: string | null;
  failure?: AzDoFailure;
  message?: string;
}

/** PS: the test records whose log moved since `since`, with the candidate
 *  failure lines in their tails, and the newest change seen. A refused call
 *  prints the classified line instead of reading as "no tests failed". */
export function azdoTestFailScript(buildId: number, since: string): string {
  const safeSince = since.replace(/[^0-9T:.Z-]/g, "");
  return `# rxaf-testfail
$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
$since = '${safeSince}'
$water = ''
try {
  $t = Invoke-AzureRestMethod ("build/builds/${buildId}/timeline") @cred -ErrorAction Stop
  $ls = Invoke-AzureRestMethod ("build/builds/${buildId}/logs") @cred -ErrorAction Stop
} catch {
${errBlock("TEST")}
  exit 0
}
foreach ($r in (@($t.records) | Where-Object { $_.log.id -and $_.name -match 'test' } | Select-Object -First 12)) {
  $e = @($ls) | Where-Object { $_.id -eq $r.log.id } | Select-Object -First 1
  if (-not $e -or -not $e.lastChangedOn) { continue }
  $at = if ($e.lastChangedOn -is [datetime]) { $e.lastChangedOn.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } else { "$($e.lastChangedOn)" }
  if ($at -gt $water) { $water = $at }
  if ($since -and $at -le $since) { continue }
  try {
    $lg = Invoke-AzureRestMethod ("build/builds/${buildId}/logs/" + $r.log.id) @cred -ErrorAction Stop
    $hits = @(@($lg -split "\\r?\\n") | Where-Object { $_ -match 'Failed\\s+\\S' -and $_ -notmatch 'Failed!' } | Select-Object -Last 20)
    foreach ($l in $hits) { "TESTLINE=" + ($r.name -replace ';', ',') + ";$at;$l" }
  } catch { }
}
if ($water) { "TESTSEEN=$water" }`;
}

/** A VSTest console line for a failed test: `  Failed Namespace.Class.Method
 *  [12 ms]`, usually behind the leading timestamp every AzDO log line carries.
 *  Both the timestamp and the duration are optional and stripped. A summary line
 *  (`Failed!  - Failed:  1, Passed: 42`) is not a name and is filtered before
 *  this grammar ever sees it. */
const FAILED_TEST_LINE = /^(?:\s*\d{4}-\d\d-\d\dT\S+\s+)?\s*Failed\s+(.+?)(?:\s*\[[^\]]*\])?\s*$/;

/** Every `<kind>=` line the script printed, in order. */
function markerLines(stdout: string, kind: string): string[] {
  return stdout.split(/\r?\n/).filter((l) => l.startsWith(`${kind}=`));
}

/** Parse the test scan's output. A `TEST=ERR;…` line is the classified refusal
 *  and wins over any lines the same run also printed — a half-read scan is not
 *  a complete answer. */
export function parseTestLines(stdout: string): TestFailPoll {
  const errLine = markerLines(stdout, "TEST").pop();
  const failed = errLine ? classifiedFailure(errLine, "TEST") : null;
  if (failed) {
    return { kind: "error", lines: [], watermark: null, failure: failed.failure, message: failed.message };
  }
  const lines: TestFailLine[] = [];
  for (const l of markerLines(stdout, "TESTLINE")) {
    const m = l.match(/^TESTLINE=([^;]*);([^;]*);(.*)$/);
    if (m) lines.push({ record: m[1], at: m[2], line: m[3] });
  }
  const seen = markerLines(stdout, "TESTSEEN").pop();
  const watermark = seen ? seen.slice("TESTSEEN=".length).trim() || null : null;
  return { kind: lines.length ? "lines" : "none", lines, watermark };
}

/** The fully-qualified names the candidate lines carry, deduped in the order
 *  they were read. A line that is not a name is DROPPED here: the script's
 *  filter is deliberately loose, so this grammar — not the fetch — decides what
 *  counts as a failed test. A bare count and a `Failed:`/`Failed!` marker are
 *  not names. */
export function failedTestNames(lines: TestFailLine[]): string[] {
  const names: string[] = [];
  for (const l of lines) {
    const name = l.line.match(FAILED_TEST_LINE)?.[1]?.trim() ?? "";
    if (!name || /^[:!\d]/.test(name)) continue;
    if (!names.includes(name)) names.push(name);
  }
  return names;
}

/** The wire a scan travels in, rendered from its parts. Exported for the SAME
 *  reason `renderProgressLine` is: the producer is the only place that knows the
 *  field order, so a fixture builds through it instead of re-typing the format
 *  and silently staying green when the script's own line drifts. `stamp` is the
 *  leading timestamp AzDO puts in front of every log line. */
export function renderTestScan(failing: string[], at: string, record = TEST_RECORD, stamp = "2026-10-26T08:00:00.1234567Z"): string {
  const lines = failing.map((n) => `TESTLINE=${record};${at};${stamp}   Failed ${n} [12 ms]`);
  return [...lines, `TESTSEEN=${at}`].join("\r\n") + "\r\n";
}

/** A refusal line, rendered from its parts. */
export function renderTestRefusal(failure: AzDoFailure, message: string): string {
  return `TEST=ERR;${failure};${message}`;
}
