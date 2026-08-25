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
a definition: `build/builds?definitions=<id>&$top=5&queryOrder=queueTimeDescending`
(definition 23 = AZDO_BUILD_URL). The queue-time ordering is REQUIRED:
`Get-AzBuilds` omits it and the API default buries in-progress builds, so a
bare `-Top 1` returned the newest COMPLETED build (2026-08-25 fix). The
`minId` filter (`id > minId`) drops builds the chain already passed. On
failure the failed record's log prints between LOGSTART/LOGEND markers
(last 500 lines). Output: `STATUS=<id>;<status>;<result>;<reason>` or
`STATUS=0;none;none;`.

## Cancel script (`cancelAzDoScript`)

Same fetch; `cancelAzDoScript` now takes the definition
(`cancelAzDoScript(definitions = LAB_DEF)` — default Lab def 23). When the
build is in inProgress/notStarted/postponed/cancelling
it PATCHes `{"status":"cancelling"}` (the documented cancel; DELETE is
rejected on running builds — `CannotDeleteRunningBuildException` — which
silently broke prx's cancel and left zombie builds holding the pool, fixed
2026-08-25 in XpandPwsh). Output: `CANCEL=<id>;ok;<status>` /
`CANCEL=<id>;notrunning;<status>` / `CANCEL=0;none;none`.

## Release queue (`prx -Release`)

The Release publish uses `prx -Release` — prx stages, force-pushes
`lab:master` and queues the Reactive.XAF pipeline (def 23) on master with
the Release tag. prx knows the right pipe; the def-39 queue script was
removed (2026-08-25).

## GitHub fetch (`defaultGhFetch`)

`defaultGhFetch(url, {method?, body?})` — the GitHub API fetch with
`Authorization: Bearer` from **GH_TOKEN** / **GITHUB_TOKEN** when set
(GitHub drafts are only returned to authenticated callers) + the required
User-Agent. The token is NEVER logged. The watcher's draft publish uses
`seams.ghFetch` (default = this).

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
