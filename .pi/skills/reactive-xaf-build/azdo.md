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
