/**
 * reactive-xaf-build/test-scan — the AzDO test scan: its script and its parser.
 *
 * Split out of `azdo.ts`, which sits at the 400-line cap, and never folded into
 * `azdo-tests.ts`, which is that module's SUITE and no place to hide an
 * implementation. The refusal grammar is not re-invented here: a dead token
 * prints the same `TEST=ERR;<auth|transient>;<msg>` line because it comes from
 * `azdo.ts`'s own `errBlock`, and is read back by its own `classifiedFailure`.
 *
 * WHY RESULTS AND NOT LOGS. This scan used to download the console log of every
 * record whose name carried "test" and guess the failures out of it by looking
 * for a line starting with `Failed `. That guess was wrong on a PASSING job: an
 * XAF test hosts a web server, ASP.NET Core prints "Failed to determine the
 * https port for redirect." as a warning, and build 36120's report named that
 * sentence as a failed test. Azure answers the question directly —
 * `Get-AzTestRuns` for the build, then `test/Runs/{id}/Results?outcomes=Failed`
 * for the names — so the name arrives as a FIELD and no grammar exists to be
 * wrong. The price is latency: a result appears when its VSTest task attempt
 * ends, not the instant the test dies, which was the whole reason for reading
 * logs. Correctness won.
 *
 * COST IS BOUNDED BY A WATERMARK. The caller passes the newest result time the
 * previous scan saw, and only runs whose statistics count a failure are opened:
 * a green build costs ONE call, and a result already reported costs nothing.
 */

import { classifiedFailure, errBlock } from "./azdo-refusal.js";
import type { AzDoFailure } from "./azdo-refusal.js";

/** The run id a FIXTURE labels its results with. Nothing filters on it: the scan
 *  reads whatever runs Azure returns, and this constant exists only so a rendered
 *  fixture carries the same run column a live poll does. The value is build
 *  36117's last run — the one the results API answered the three names from. */
export const TEST_RUN = "1023530";

/** One failed result: the test run it belongs to, when it completed, and the
 *  test's name as the API states it. The wire is the script's TESTLINE= line;
 *  `line` is a FIELD read from Azure, never prose to be parsed. */
export interface TestFailLine {
  record: string;
  at: string;
  line: string;
}

/** One test scan, classified. `none` is a build where the scan found the build's
 *  runs and none of them carried a failed result — a fact, not a failure to read
 *  — and `error` carries the same auth/transient split the status query uses.
 *  `runs`/`failedRuns` are what the scan LOOKED AT, so an empty answer is
 *  falsifiable: "24 runs, 0 with failures" is not the same statement as a scan
 *  that reached nothing. `watermark` is the newest run update the scan saw and is
 *  what the next call passes as `since` (null when nothing was readable). */
export interface TestFailPoll {
  kind: "lines" | "none" | "error";
  lines: TestFailLine[];
  watermark: string | null;
  runs?: number;
  failedRuns?: number;
  failure?: AzDoFailure;
  message?: string;
}

/** PS: the failed test results published for the build so far, one TESTLINE per
 *  result, and the newest result time seen. A refused call prints the
 *  classified line instead of reading as "no tests failed". */
export function azdoTestFailScript(buildId: number, since: string): string {
  const safeSince = since.replace(/[^0-9T:.Z-]/g, "");
  return `# rxaf-testfail
$cred = @{ Project = $env:AzProject; Organization = $env:AzOrganization }
$since = '${safeSince}'
$water = ''
try {
  $runs = @(Get-AzTestRuns -buildIds ${buildId} -ErrorAction Stop)
  $broken = 0
  foreach ($run in $runs) {
    $failed = 0
    foreach ($s in @($run.runStatistics)) { if ($s.outcome -eq 'Failed') { $failed += [int]$s.count } }
    if ($failed -le 0) { continue }
    $broken++
    $stamp = if ($run.lastUpdatedDate -is [datetime]) { $run.lastUpdatedDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") } else { "$($run.lastUpdatedDate)" }
    if ($stamp -gt $water) { $water = $stamp }
    if ($since -and $stamp -le $since) { continue }
    foreach ($x in @(Invoke-AzureRestMethod ("test/Runs/" + $run.id + "/Results?outcomes=Failed") @cred -ErrorAction Stop)) {
      if ("$($x.outcome)" -ne 'Failed') { continue }
      $n = "$($x.testCase.name)".Trim()
      if (-not $n) { continue }
      "TESTLINE=$($run.id);$stamp;" + ($n -replace ';', ',')
    }
  }
  "TESTRUNS=$($runs.Count);$broken"
} catch {
${errBlock("TEST")}
  exit 0
}
if ($water) { "TESTSEEN=$water" }`;
}

/** Every `<kind>=` line the script printed, in order. */
function markerLines(stdout: string, kind: string): string[] {
  return stdout.split(/\r?\n/).filter((l) => l.startsWith(`${kind}=`));
}

/** What the scan LOOKED AT, read off `TESTRUNS=<runs>;<failedRuns>`. A line that
 *  is missing or unreadable contributes nothing rather than a fabricated zero:
 *  "looked at nothing yet" and "looked at 24 runs and found no failure" are
 *  different answers, and the empty one is the answer that has to be legible. */
function parseRunCounts(line: string | undefined): { runs?: number; failedRuns?: number } {
  const m = line?.match(/^TESTRUNS=(\d+);(\d+)\s*$/);
  if (!m) return {};
  return { runs: Number(m[1]), failedRuns: Number(m[2]) };
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
  const counts = parseRunCounts(markerLines(stdout, "TESTRUNS").pop());
  return { kind: lines.length ? "lines" : "none", lines, watermark, ...counts };
}

/** The names the results carry, deduped in the order they were read. The name
 *  is a field Azure wrote, so nothing is interpreted here: a result whose name
 *  is empty says nothing and is dropped, and two results for the same test (the
 *  pipeline retries every VSTest task twice) collapse into one. */
export function failedTestNames(lines: TestFailLine[]): string[] {
  const names: string[] = [];
  for (const l of lines) {
    const name = l.line.trim();
    if (!name || names.includes(name)) continue;
    names.push(name);
  }
  return names;
}

/** The wire a scan travels in, rendered from its parts. Exported for the SAME
 *  reason `renderProgressLine` is: the producer is the only place that knows the
 *  field order, so a fixture builds through it instead of re-typing the format
 *  and silently staying green when the script's own line drifts. `at` is the run
 *  stamp the results were read at, which is also the watermark reported back.
 *  `runs`/`failedRuns` are the counts the script prints for `TESTRUNS`. */
export function renderTestScan(
  failing: string[],
  at: string,
  opts: { run?: string; runs?: number; failedRuns?: number } = {},
): string {
  const run = opts.run ?? TEST_RUN;
  const runs = opts.runs ?? 1;
  const failedRuns = opts.failedRuns ?? (failing.length ? 1 : 0);
  const lines = failing.map((n) => `TESTLINE=${run};${at};${n}`);
  return [...lines, `TESTRUNS=${runs};${failedRuns}`, `TESTSEEN=${at}`].join("\r\n") + "\r\n";
}

/** A refusal line, rendered from its parts. */
export function renderTestRefusal(failure: AzDoFailure, message: string): string {
  return `TEST=ERR;${failure};${message}`;
}
