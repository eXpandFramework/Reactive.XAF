---
name: reactive-xaf-build/azdo
description: The AzDO status/cancel scripts behind /devexpress — the STATUS=/CANCEL= line protocol, the CRLF parse contract, the PATCH cancel, the one-pipeline (def 23) model, the Release queue via prx -Release, the GitHub fetch seam, and fail-reason extraction.
---

# azdo.ts / status.ts — AzDO status + cancel

Companion of `.pi/extensions/reactive-xaf-build/azdo.ts` (one-shot status +
cancel surface in `status.ts`). One pwsh spawn per query; the profile loads
`Invoke-AzureRestMethod` (XpandPwsh) and sets `$env:AzProject` /
`$env:AzOrganization`.

## One pipeline (def 23)

Lab and Release builds run the SAME Reactive.XAF pipeline (`LAB_DEF =
"23"`); Release (`prx -Release`) queues branch master. There is no separate
Release definition — the def-39 premise was wrong (reverted 2026-08-25);
the chain's PublishNugets
trigger listens to 23. `azdoBuildUrl(definition)` builds the AzDO link
(`AZDO_BUILD_URL` = def 23).

## Status script (`azdoStatusScript`)

`azdoStatusScript(definitions = "23", minId = 0)` queries the newest build of
a definition: `build/builds?definitions=<id>&$top=5&queryOrder=queueTimeDescending`.
The `minId` filter (`id > minId`) drops builds the chain already passed. On
failure the failed record's log prints between LOGSTART/LOGEND markers
(last 500 lines). Output: `STATUS=<id>;<status>;<result>;<buildNumber>;<reason>`
or `STATUS=0;none;none;`. `parseStatus` also accepts the older 4-field
line (no buildNumber) so existing fixtures keep working.

## The progress line (`PROGRESS=`)

Three shapes, one per state the build can be in:

- `PROGRESS=queue;<status>;<queueTime>` — `notStarted`/`postponed`: no agent has
  picked the build up, so nothing can be produced and nothing is stalled.
- `PROGRESS=1;<record>;<recordStart>;<lastOutput>;<source>` — in flight.
- `PROGRESS=0;;;` — the timeline could not be read. The ONLY blindness shape.

`<source>` is the LAST field and is OPTIONAL: absent means `task`. That is why
the four-field line captured from build 36111 below still parses, and W24 in
`watcher-tests.ts` replays it verbatim to keep it that way. The parser turns the
line into the `BuildProgress` union (`queue` | `running` | `blind`), and
`renderProgressLine` is the one place that turns a reading back INTO the wire
line — the watcher's tests build their fixtures through it rather than re-typing
the format (`watcher-tests.md`).

`<record>` is the deepest record still in flight: an `inProgress` Task, else a
Phase, else a Job (the parent's name is prefixed when there is one). A `log.id`
is NOT required — build 36112's live `Build` task reported `log: null` for two
hours, and requiring one made a silent build look unreadable.

`<source>` says where `<lastOutput>` came from:

- `task` — the record's own log. The last 30 lines are scanned for the last
  AzDO stamp (`YYYY-MM-DDTHH:mm:ss[.fffffff]Z`), so a trailing continuation line
  carrying no timestamp no longer hides the real one.
- `logs` — the task opened no log of its own, so the reading is the newest
  `lastChangedOn` across `build/builds/{id}/logs` (one extra REST call, made only
  on this path). A frozen watermark is still output evidence: it is silence, not
  blindness.

Why: AzDO's `status` reads `inProgress` for a hung job too, so silence is the
only early evidence of a hang (2026-10-25: `Tests Workflow` wrote nothing for 80
minutes while every status field said the build was fine). `parsePoll` attaches
the reading to the build as `progress` (the `BuildProgress` union);
`progressFromStdout` degrades an unparseable stamp to `blind`, never to a very
old reading. A 4-field line from an older script parses as `source=task`. The
`stallMs` and `queueMs` verdicts in `watcher-notice.ts` (`reportProgress`) are
the only consumers (`watcher.md`), and only `PROGRESS=0` ever reaches
`blindPoll`.

### Captured output (build 36111, 2026-10-25)

Verbatim from the AzDO API, so the wire format is not a fixture's invention:

```
STATUS=36111;completed;failed;4.261.5.1-26.1.3;
PROGRESS=1;Tests Workflow / Run Tests;2026-10-25T14:39:50Z;2026-10-25T16:04:20.1296030Z
```

The `PROGRESS` line IS the hang: `Run Tests` inside `Tests Workflow`, started
14:39:50Z, whose last log line (`##[section]Finishing: Run Tests`) was written at
16:04:20Z — 84 minutes later, when the 90-minute job timeout canceled it. W24 in
`watcher-tests.ts` replays these exact strings (4 fields, no `source` — it parses
as a task reading, which is why the older shape keeps working).

### Observed record shape (build 36112, 2026-10-25) — the case that forced `logs`

Measured from AzDO while the build sat in flight: task `Build` `inProgress`
since 18:57:46Z with `log: null`, every pending task `log: null`, and all five
log containers unchanged since 18:57:48Z. The old script found no candidate
record and printed `PROGRESS=0;;;` — so the monitor called itself blind for two
hours while the newest container's `lastChangedOn` was stating exactly how long
the silence had been.

### Line this block prints for it — RECONSTRUCTED, not captured

Nothing was observed on the wire: the build was canceled before the new script
ever ran against it. What follows is the output of `azdoStatusScript` under a
replayed `Invoke-AzureRestMethod` returning those records, run by hand:

```
PROGRESS=1;Build / Build;2026-10-25T18:57:46Z;2026-10-25T18:57:48Z;logs
```

`PROGRESS=0;;;` is now the only unknown: it appears when the timeline call
itself fails, and that is the one reading `watcher.ts` (`reportProgress`) hands to `blindPoll`. A
time is never invented, and a log line without a stamp is skipped in favour of
the last stamped line.

## The call is classified, not guessed (2026-10-25)

The query sits in a `try/catch` in both scripts and a refusal prints
`STATUS=ERR;<auth|transient>;<message>` (`CANCEL=ERR;…` for the cancel). Before
this, a refused or expired token was written as an error while the script
CARRIED ON: `$b` stayed null, `STATUS=0;none;none;` was printed with exit 0, so
a dead PAT read as an empty pipeline to every caller — the watcher waited on
nothing until its deadline and \"Last build status\" answered \"No AzDO builds
found\".

- **`auth`** — matched on the server's own wording (`401|Unauthorized|Access
  Denied|expired`). No retry clears it, so the watcher gives up on the FIRST
  poll and names the remedy (renew the PAT, re-queue).
- **`transient`** — anything else (a 429, a blip, a proxy). The watch keeps
  retrying on the empty-poll grace clock and only reports if it never clears.

A script-wide `$ErrorActionPreference = 'Stop'` was rejected for exactly that
reason: it made a transient blip indistinguishable from a dead token, and the
difference then had to be guessed from error prose downstream. The classifier is
in the script that sees the failure; the consumers switch on the kind.

Both halves live in `azdo-refusal.ts` — `errBlock(kind)` and
`classifiedFailure(line, kind)` — because every AzDO query in this extension is
the same call and must fail the same way. The `kind` union carries three
members, `STATUS`, `CANCEL` and `TEST`, so `test-scan.ts` prints
`TEST=ERR;<auth|transient>;<message>` without inventing a second grammar. The
type is re-exported here, so an importer of this module keeps one name for it.
Detail: `azdo-refusal.md`.

## The test scan is NOT here

The query that finds a failing test while the build still runs
(`azdoTestFailScript`, `TESTLINE=`/`TESTSEEN=` lines) lives in `test-scan.ts`,
which owns its script, its parser AND its renderer: this module sits at the
400-line cap, and one protocol belongs in one place. What it borrows from here is
the refusal grammar above. Detail: `test-scan.md`.

## Cancel script (`cancelAzDoScript`)

Project-wide PATCH `{"status":"cancelling"}`. Output: `CANCEL=<id>;ok;<status>` /
`CANCEL=<id>;notrunning;<status>` / `CANCEL=0;none;none`.

## Parsing (the CRLF contract)

pwsh pipe output is CRLF: `parseStatus` / `parseCancel` split on `/\r?\n/`.

## Fail-reason extraction

`extractFailReason` scores the log for the ONE real error. Consumed by
`statusPhase` and the watcher's terminal toast.

## Failure policy

On an AzDO failure the agent PLANS a fix and presents it — user permission
is ALWAYS required before any action. No auto-fix, no auto re-run.
