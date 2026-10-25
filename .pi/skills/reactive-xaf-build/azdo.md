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
