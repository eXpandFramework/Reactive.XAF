---
name: reactive-xaf-build/azdo-tests
description: Behavior contract for the AzDO status/cancel parse path — CRLF (pwsh-shaped) STATUS=/CANCEL= output must parse through the registered command.
---

# azdo-tests.ts — AzDO parse contract

Companion of `.pi/extensions/reactive-xaf-build/azdo-tests.ts`. Pins the
CRLF parse contract.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts`

## Pinned behaviors

- T1 — `/devexpress status` on CRLF output surfaces the id AND the extracted
  fail reason (`error DX1003` beats the wrapper noise).
- T2 — `/devexpress cancel` on `CANCEL=35735;ok;3` reports the cancel request.
- T3 — `/devexpress cancel` on `CANCEL=0;none;none` reports nothing to cancel.
- T4 — the devexpress command registers through the real index boot.
- T5 — plain `status` queries the Reactive.XAF definition (23).
- T6 — cancel is project-wide: no definition filter, statusFilter query.
- T7 — the status log block targets the failed Task record.
- T8 — 5-field `STATUS=id;status;result;buildNumber;reason` still
  extracts the log reason (`Release 26.1.301.1 exists`).
