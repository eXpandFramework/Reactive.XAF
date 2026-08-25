---
name: reactive-xaf-build/azdo-tests
description: Behavior contract for the AzDO monitor/status parse path — CRLF (pwsh-shaped) RESULT=/STATUS= output must parse. Read when changing parseOutcome/parseStatus in azdo.ts, or when the monitor reports "no RESULT= line in monitor output".
---

# azdo-tests.ts — AzDO parse contract

Companion of `.pi/extensions/reactive-xaf-build/azdo-tests.ts`. Pins the
CRLF parse contract: the monitor/status scripts print RESULT=/STATUS=
lines with \r\n (pwsh pipe output), and parseOutcome/parseStatus must
split on /\r?\n/. A regression to a bare \n split leaves \r on every line
and the (.*)$ regex cannot cross it — every real run then reports the
fake "no RESULT= line in monitor output" (2026-08-25 fix).

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts`

## Pinned behaviors

- T1 — a failed build's CRLF output (LOGSTART/LOGEND block +
  `RESULT=35742;failed;`) parses to the failed outcome, and the fail
  reason is extracted from the CRLF log lines.
- T2 — canceled / succeeded / timeout CRLF outcomes parse to their
  expected outcomes (timeout → id 0, other, "timeout").
- T3 — the devexpress command registers through the real index boot,
  and `/devexpress status` parses CRLF STATUS= output (id + reason
  surfaced).

All fixtures are CRLF — bare-LF fakes would not exercise the bug class
this file pins. Real pwsh, AzDO and pi are never spawned.
