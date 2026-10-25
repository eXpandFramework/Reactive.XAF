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

While the build is in flight the script also prints
`PROGRESS=<ok>;<task>;<taskStart>;<lastOutput>`. `ok=1` carries the task the
build is inside — the oldest `inProgress` Task record that has a `log.id` — when
it started, and the timestamp of that task's LAST log line (the first token of
the last non-empty line). `ok=0`, or no line at all, means the timeline or the
log could not be read.

Why: AzDO's `status` reads `inProgress` for a hung job too, so silence is the
only early evidence of a hang (2026-10-25: `Tests Workflow` wrote nothing for 80
minutes while every status field said the build was fine). `parsePoll` attaches
it to the build as `progress`; `progressFromStdout` turns an unparseable stamp
into `null`, never `0` — an unknown time must never read as a very old one. The
watcher's `stallMs` decision is the only consumer (`watcher.md`).

### Captured output (build 36111, 2026-10-25)

Verbatim from the AzDO API, so the wire format is not a fixture's invention:

```
STATUS=36111;completed;failed;4.261.5.1-26.1.3;
PROGRESS=1;Tests Workflow / Run Tests;2026-10-25T14:39:50Z;2026-10-25T16:04:20.1296030Z
```

The `PROGRESS` line IS the hang: `Run Tests` inside `Tests Workflow`, started
14:39:50Z, whose last log line (`##[section]Finishing: Run Tests`) was written at
16:04:20Z — 84 minutes later, when the 90-minute job timeout canceled it. W24 in
`watcher-tests.ts` replays these exact strings.

The script prints `PROGRESS=0;;;` when the timeline cannot be read, and leaves
the timestamp field empty when the last log line carries no leading timestamp: it
never invents a time, because an unknown time must never read as a very old one.
Both shapes make the progress unknown, and `watcher.ts` reports a monitor that
cannot see (`blindPoll`) rather than guessing.

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
