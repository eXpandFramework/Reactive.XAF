---
name: reactive-xaf-build/status
description: The one-shot AzDO status and cancel surface — statusPhase (newest build state + fail reason + link) and cancelPhase (PATCH-cancel the newest running build).
---

# status.ts — AzDO status + cancel one-shots

Companion of `.pi/extensions/reactive-xaf-build/status.ts`. Both commands
run one pwsh spawn via the run seam and notify the outcome in the invoking
window. Info only — no steering.

## `statusPhase(ctx, seams)` — `/devexpress status`

Runs `azdoStatusScript(definition)` (azdo.md), parses the CRLF `STATUS=`
line via `parseStatus` and notifies. `definition` defaults to Lab (def 23);
the direct arg `status release` queries the Release pipeline (def 39). The
AzDO link reflects the definition:

- running (notStarted/inProgress/cancelling) → id + state + AzDO link;
- failed → id + `extractFailReason(failLogFromStdout(...))` when the script
  carried no reason + link;
- canceled / succeeded / none → the plain outcome;
- no STATUS= line → the script's stderr tail.

## `cancelPhase(ctx, seams)` — `/devexpress cancel`

Runs `cancelAzDoScript(definition)` (azdo.md — PATCH
`{"status":"cancelling"}` on a
running build; DELETE is rejected by the API on running builds), parses
`CANCEL=` via `parseCancel` and notifies. `definition` defaults to Lab (def
23); the direct arg `cancel release` targets the Release pipeline (def 39):

- `ok` → "Cancel requested for AzDO build <id> — the agent stops it within
  ~2 min";
- `notrunning` → id + current status, nothing to cancel;
- `0;none;none` → no builds found;
- no CANCEL= line → the script's stderr tail.

Contract: `azdo-tests.ts` (CRLF parse through the registered command).
