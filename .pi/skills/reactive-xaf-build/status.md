---
name: reactive-xaf-build/status
description: Use when changing the one-shot AzDO status and cancel surface — statusPhase (newest build state + fail reason + link) and cancelPhase (PATCH-cancel). Definition from profile.statusDef.
---

# status.ts — AzDO status + cancel one-shots

Companion of `.pi/extensions/reactive-xaf-build/status.ts`. Both commands
run one pwsh spawn via the run seam and notify the outcome in the invoking
window. Info only — no steering.

## `statusPhase(ctx, seams)` — `/devexpress status`

Runs `azdoStatusScript(definition)` (azdo.md). `definition` defaults to
`profile.statusDef("Lab")` (RX: 23; expand: 94). The AzDO link reflects
the definition.

- running → id + state + AzDO link
- failed → id + `extractFailReason` + link
- canceled / succeeded / none → the plain outcome

## `cancelPhase(ctx, seams)` — `/devexpress cancel`

Runs `cancelAzDoScript()` — PATCH `{"status":"cancelling"}` on every
running/queued build.

- `ok` → "Cancel requested for AzDO build <id>"
- `notrunning` → id + current status, nothing to cancel
- `0;none;none` → no builds found
