---
name: reactive-xaf-build/test-scan
description: Use when the AzDO test scan misreads a failure or is being changed — test-scan.ts queries the running build's test records, prints the candidate failure lines from the logs that moved since the last poll, and parses the VSTest console grammar into test names. Read before editing the scan, its wire lines, or azdo.ts's exported errBlock/classifiedFailure.
---

# test-scan.ts — the AzDO test scan

Companion of `.pi/extensions/reactive-xaf-build/test-scan.ts`. Split out of
`azdo.ts`, which sits at the 400-line cap, and separate from `azdo-tests.ts`,
which is that module's SUITE and no place to hide an implementation.

## Why logs, not results

The Reactive.XAF pipeline retries every VSTest task twice
(`retryCountOnTaskFailure: 2`, `Build/BuildPipeline.yml:215-231`) and the `Tests`
job is `continueOnError`, so the timeline only admits a failure after the retries
are spent. That is the latency the scan removes: the failing test is read from
the log as it is written.

## The script — `azdoTestFailScript(buildId, since)`

One pwsh spawn, printed as marker lines:

- `TESTLINE=<record>;<lastChangedOn>;<raw log line>` — one per candidate failure
  line, for a record whose log moved past `since`.
- `TESTSEEN=<iso>` — the newest log change the scan saw. The caller passes it
  back as `since` on the next poll, so an unchanged leg costs nothing.
- `TEST=ERR;<auth|transient>;<message>` — a refused call. The classification is
  `azdo.ts`'s own `errBlock`, so a dead token reads the same in both scans; both
  helpers are exported from `azdo.ts` for exactly this reuse.

Bounds: at most 12 records are inspected per call (`Select-Object -First 12`),
the line filter is loose (`Failed\s+\S`, never `Failed!`), and only the last 20
candidate lines of a log survive. The script fetches; it does not interpret.

## The parser and the renderer

- `parseTestLines(stdout)` → `{ kind: "lines" | "none" | "error", lines,
  watermark, failure?, message? }`. `none` is a fact — no test record in flight —
  not a failure to read. A `TEST=ERR` line wins over any lines the same run also
  printed: a half-read scan is not a complete answer.
- `failedTestNames(lines)` → the fully-qualified names, deduped in read order.
  `FAILED_TEST_LINE` accepts the leading timestamp every AzDO log line carries
  and the optional `[12 ms]` duration, and DROPS anything that is not a name: an
  empty capture, a `Failed:`/`Failed!` marker, or a bare count.
- `renderTestScan(failing, at, record?, stamp?)` and `renderTestRefusal(failure,
  message)` — the wire rendered from its parts, exported for the SAME reason
  `renderProgressLine` is: the producer is the only place that knows the field
  order, so a fixture builds through it instead of re-typing the format and
  silently staying green when the script's own line drifts.

## Ownership

This module owns the test protocol END TO END — script, grammar, result and the
renderer. `test-watch.ts` consumes only the typed result, and `watcher.ts` never
sees script construction: it hands `test-watch.ts` a shell runner port.

## Contracts

`watcher-tests.ts` W40–W52 (through pi's own runtime) and the `# rxaf-testfail`
routing in that suite's `mkSeams`. The fixtures are built by `renderTestScan`,
which is the producer's own renderer.
