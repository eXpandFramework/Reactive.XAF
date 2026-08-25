---
name: reactive-xaf-build/azdo-tests
description: Behavior contract for the AzDO status/cancel parse path — CRLF (pwsh-shaped) STATUS=/CANCEL= output must parse through the registered command.
---

# azdo-tests.ts — AzDO parse contract

Companion of `.pi/extensions/reactive-xaf-build/azdo-tests.ts`. Pins the
CRLF parse contract: the status/cancel scripts print STATUS=/CANCEL=
lines with \r\n (pwsh pipe output), and parseStatus/parseCancel must
split on /\r?\n/. A regression to a bare \n split leaves \r on every line
and the (.*)$ regex cannot cross it — the old monitor reported the fake
"no RESULT= line in monitor output" (2026-08-25 fix).

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts`

## Pinned behaviors

- T1 — `/devexpress status` on CRLF output (LOGSTART/LOGEND block +
  `STATUS=35735;completed;failed;`) surfaces the id AND the extracted
  fail reason (`error DX1003` beats the wrapper noise).
- T2 — `/devexpress cancel` on `CANCEL=35735;ok;inProgress` reports the
  cancel request.
- T3 — `/devexpress cancel` on `CANCEL=35735;notrunning;completed`
  reports nothing to cancel.
- T4 — the devexpress command registers through the real index boot.
- T5 — plain `status` queries the Reactive.XAF definition (23) — Lab and
  Release both run def 23, there is no release-arg variant (the def-39
  variants were removed 2026-08-25).

All fixtures are CRLF — bare-LF fakes would not exercise the bug class
this file pins. Real pwsh, AzDO and pi are never spawned.
