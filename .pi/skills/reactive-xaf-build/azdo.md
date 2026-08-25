---
name: reactive-xaf-build/azdo
description: The AzDO monitor/status scripts behind /devexpress — the RESULT=/STATUS= line protocol, the CRLF parse contract, and fail-reason extraction. Read when changing waitForAzDoBuild, the monitor poll loop, outcome/status parsing, or the ##[error] reason scoring.
---

# azdo.ts / status.ts — AzDO monitor + status

Companion of `.pi/extensions/reactive-xaf-build/azdo.ts` (the one-shot
status surface lives in `status.ts`). One pwsh spawn per query; the profile
loads `Get-AzBuilds` + `Invoke-AzureRestMethod` (XpandPwsh) and sets
`$env:AzProject` / `$env:AzOrganization`.

## Monitor (`defaultWaitForAzDoBuild`)

- Spawns `pwsh -Command <monitorScript>` via the run seam (pane.ts
  `runArgv` default); the spawn timeout carries a 5-minute margin over the
  script's own deadline so the final RESULT= print never loses the race to
  the taskkill.
- The script polls `Get-AzBuilds -Definition Reactive.XAF -Top 1` every 30 s
  until the build leaves inProgress/notStarted/cancelling, or the deadline
  passes. prx cancels in-progress builds before queueing, so the newest
  build is ours.
- On a failed build, the failed timeline record's log is fetched and printed
  between LOGSTART/LOGEND markers (bounded to the last 500 lines).
- Prints one `RESULT=<id>;<result>;<reason>` line, or
  `RESULT=timeout;;` / `RESULT=0;other;no AzDO build found`.

## Parsing (the CRLF contract)

pwsh pipe output is CRLF: `parseOutcome` / `parseStatus` split on
`/\r?\n/`. A bare `\n` split left `\r` on every line and the `(.*)$` regex
could not cross it — every real monitor run reported the fake "no RESULT=
line in monitor output" (2026-08-25 fix, pinned by `azdo-tests.ts`).

## Fail-reason extraction

`extractFailReason` scores the fetched log for the ONE real error: tier-1
compiler/MSBuild/DX lines (`error CS|MSB|DX|NU...`) beat the generic
"Build FAILED" marker; wrapper noise (ScriptHalted, Approve-LastExitCode,
`##[error]Exception`, "PowerShell exited with code", retry lines) is
filtered; identical retry lines dedupe; the nearest "Executing <task>"
line becomes context.

## Status (`status.ts`)

`statusPhase` runs the one-shot `azdoStatusScript` (same fetch, same
protocol: `STATUS=<id>;<status>;<result>;<reason>`) and notifies the
outcome with the AzDO definition link. Info only — no steering.

## Failure policy

On an AzDO failure the agent PLANS a fix and presents it — user permission
is ALWAYS required before any action. No auto-fix, no auto re-run.
