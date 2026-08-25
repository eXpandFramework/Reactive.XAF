---
name: reactive-xaf-build/azdo
description: The AzDO status/cancel scripts behind /devexpress — the STATUS=/CANCEL= line protocol, the CRLF parse contract, the PATCH cancel, and fail-reason extraction.
---

# azdo.ts / status.ts — AzDO status + cancel

Companion of `.pi/extensions/reactive-xaf-build/azdo.ts` (one-shot status +
cancel surface in `status.ts`). One pwsh spawn per query; the profile loads
`Invoke-AzureRestMethod` (XpandPwsh) and sets `$env:AzProject` /
`$env:AzOrganization`.

## Status script (`azdoStatusScript`)

Queries the newest Reactive.XAF build via
`build/builds?definitions=23&$top=1&queryOrder=queueTimeDescending`
(definition 23 = AZDO_BUILD_URL). The queue-time ordering is REQUIRED:
`Get-AzBuilds` omits it and the API default buries in-progress builds, so a
bare `-Top 1` returned the newest COMPLETED build (2026-08-25 fix). On
failure the failed record's log prints between LOGSTART/LOGEND markers
(last 500 lines). Output: `STATUS=<id>;<status>;<result>;<reason>` or
`STATUS=0;none;none;`.

## Cancel script (`cancelAzDoScript`)

Same fetch; when the build is in inProgress/notStarted/postponed/cancelling
it PATCHes `{"status":"cancelling"}` (the documented cancel; DELETE is
rejected on running builds — `CannotDeleteRunningBuildException` — which
silently broke prx's cancel and left zombie builds holding the pool, fixed
2026-08-25 in XpandPwsh). Output: `CANCEL=<id>;ok;<status>` /
`CANCEL=<id>;notrunning;<status>` / `CANCEL=0;none;none`.

## Parsing (the CRLF contract)

pwsh pipe output is CRLF: `parseStatus` / `parseCancel` split on `/\r?\n/`.
A bare `\n` split leaves `\r` on every line and the `(.*)$` regex cannot
cross it (2026-08-25 fix, pinned by `azdo-tests.ts`).

## Fail-reason extraction

`extractFailReason` scores the log for the ONE real error: tier-1
compiler/MSBuild/DX lines (`error CS|MSB|DX|NU...`) beat the generic
"Build FAILED" marker; wrapper noise (ScriptHalted, Approve-LastExitCode,
`##[error]Exception`, "PowerShell exited with code", retry lines) is
filtered; identical retry lines dedupe; the nearest "Executing <task>" line
becomes context. Consumed by `statusPhase` and the watcher's terminal toast.

## Status / cancel (`status.ts`)

`statusPhase` notifies the status outcome with the AzDO link. `cancelPhase`
notifies whether the cancel was requested. Info only — no steering.

## Failure policy

On an AzDO failure the agent PLANS a fix and presents it — user permission
is ALWAYS required before any action. No auto-fix, no auto re-run.
