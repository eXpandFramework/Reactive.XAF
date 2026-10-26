---
name: reactive-xaf-build/test-scan
description: Use when the AzDO test scan misreads a failure or is being changed — test-scan.ts asks Azure for the running build's FAILED TEST RESULTS and prints them as wire lines, so a test name is a field read from Azure and never a guess made from console output. Read before editing the scan, its wire lines, or azdo.ts's exported errBlock/classifiedFailure.
---

# test-scan.ts — the AzDO test scan

Companion of `.pi/extensions/reactive-xaf-build/test-scan.ts`. Split out of
`azdo.ts`, which sits at the 400-line cap, and separate from `azdo-tests.ts`,
which is that module's SUITE and no place to hide an implementation.

## Why logs, not results

That was the old premise, and it is wrong. The scan used to download the console
log of every record whose name carried "test" and guess the failures out of it:
any line starting with `Failed ` became a test name, and the only filter was the
`Failed:`/`Failed!` summary marker. On build 36120 that named a sentence as a
failed test —

```
2026-10-26T10:30:29.9468321Z       Failed to determine the https port for redirect.
```

— an ASP.NET Core warning printed by an XAF test app whose web server has no
HTTPS port, inside the log of a PASSING job (`Tests BulkObjectUpdate`). The build
had 58 tests, 58 passed, 0 failed. Scraping also MISSED real failures: on build
36117 it reported three names and the results held five.

## Why results now

Azure answers the question directly, so the name is read, not inferred:
`Get-AzTestRuns -buildIds <id>`, then `test/Runs/{id}/Results?outcomes=Failed`
for each run whose statistics count a failure.

The price is latency: a result appears when its VSTest task attempt ends, not the
instant the test dies (build 36117: the log line at 05:08, the run at 05:21). The
pipeline retries every VSTest task twice (`retryCountOnTaskFailure: 2`,
`Build/BuildPipeline.yml:215-231`) and Azure creates a NEW run per attempt, so a
retry never rewrites the previous run's verdict — it adds one. Correctness won.

## The script — `azdoTestFailScript(buildId, since)`

One pwsh spawn, printed as marker lines:

- `TESTLINE=<runId>;<runStamp>;<test name>` — one per failed result. `runStamp`
  is the RUN's `lastUpdatedDate`, and the name is `testCase.name` verbatim.
- `TESTRUNS=<runs>;<failedRuns>` — what the scan LOOKED AT. Printed on every
  readable poll, including the quiet one, so an empty answer is falsifiable:
  "25 runs, 0 broken" is a different statement from "the scan reached nothing".
- `TESTSEEN=<iso>` — the newest run stamp the scan saw. The caller passes it
  back as `since`, and a run at or before it is skipped before any results are
  fetched, so an already-reported failure costs nothing on later polls.
- `TEST=ERR;<auth|transient>;<message>` — a refused call. The classification is
  `azdo-refusal.ts`'s own `errBlock`, so a dead token reads the same in every
  scan; the helper is exported for exactly this reuse. A refused poll prints NO
  counts, and `TestWatch.runs` stays null rather than becoming a fabricated zero.

The stamp is RUN level on purpose. Per-result `completedDate` arrives out of
order (run 1023520's three results say 05:08, 05:13, 05:18 while the run settled
at 05:22), so a result-time watermark would silently skip a retry that published
under an older stamp. There is no cap on how many runs are inspected: build
36117 carries 24 of them, and a cap left over from paying for log downloads would
skip results past the bound without ever revisiting them.

Bounds: only runs with a failure are opened, so a green build costs ONE call, and
a result whose name is empty is dropped in the script as well as in the parser.
The script fetches; it does not interpret.

## The parser and the renderer

- `parseTestLines(stdout)` → `{ kind: "lines" | "none" | "error", lines,
  watermark, failure?, message? }`, plus the `runs`/`failedRuns` counts the
  script printed. `none` is a fact — the build's runs carried no failure — not a
  failure to read. A `TEST=ERR` line wins over any lines the same run also
  printed: a half-read scan is not a complete answer.
- `failedTestNames(lines)` → the names, deduped in read order. Nothing is
  interpreted: the name is a field Azure wrote, an empty one says nothing, and
  two results for the same test (the retries above) collapse into one.
- `renderTestScan(failing, at, { run, runs, failedRuns })` and
  `renderTestRefusal(failure,
  message)` — the wire rendered from its parts, exported for the SAME reason
  `renderProgressLine` is: the producer is the only place that knows the field
  order, so a fixture builds through it instead of re-typing the format and
  silently staying green when the script's own line drifts.
- The renderer is NOT the only fixture: `watcher-tests.ts` also replays build
  36117's live poll verbatim (`CAPTURED_TESTFAIL`) and build 36120's empty one
  (`CAPTURED_EMPTY`), the way `CAPTURED_STALL` replays build 36111's PROGRESS
  line. A same-language renderer cannot catch a pwsh-side format drift, and that
  seam is exactly where the phantom name came from.

## Ownership

This module owns the test protocol END TO END — script, wire, result and the
renderer. `test-watch.ts` consumes only the typed result — including the
`runs`/`failedRuns` counts, which it records on the watch — and `watcher.ts`
never sees script construction: it hands `test-watch.ts` a shell runner port.

## Contracts

`watcher-tests.ts` W40–W58 (through pi's own runtime) and the `# rxaf-testfail`
routing in that suite's `mkSeams`. W40 replays the captured live poll; W53 reads
the emitted pwsh TEXT and fails on a wrong call, a reordered `TESTLINE` field, a
missing `TESTRUNS`, or a returned build-log fetch; W54/W55/W56/W57 cover a
retried attempt, the captured empty poll, a refused scan and a nameless result;
W58 pins the recorded counts, including the null they must keep after a refusal.

## Observed output (build 36117, 2026-10-26)

15 `TESTLINE`s over 9 runs and 5 distinct names — including
`Synchronize_styles_when_document_modified` and
`Refresh_TraceEvent_ListView_when_trace`, which log scraping never reported —
ending `TESTSEEN=2026-10-26T06:13:54Z`. A second poll with that watermark printed
only the watermark. Build 36120 (green) printed nothing at all.
