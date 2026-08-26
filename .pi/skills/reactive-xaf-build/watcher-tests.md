---
name: reactive-xaf-build/watcher-tests
description: Behavior contract for the AzDO chain watcher — toast per poll, chain advance, nuget assertion, GitHub draft publish, empty-poll retry, missing-token steer, terminal steer, give-up, expand Lab 94, fail-reason, this-run.
---

# watcher-tests.ts — watcher behavior contract

Companion of `.pi/extensions/reactive-xaf-build/watcher-tests.ts`. Drives
`/devexpress publish lab|release` with a mock pi; the flow starts the REAL
watcher through an injected 20 ms interval seam. Fake run seam serves CRLF
STATUS= fixtures per chain step. `mkSeams` copies the poll queue so a
shared GREEN fixture is not emptied across tests.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts`

## Pinned behaviors

- W1 — full chain green (Lab): toast per poll, nugets asserted, GitHub
  DRAFT published (`prerelease:true`), chain complete, no steer.
- W2 — failed Reactive.XAF build: warning + steer, then stop.
- W3 — give-up deadline: warning + **steer** + stop.
- W4 — a new publish replaces the previous watcher.
- W5 — command registers through the real index boot.
- W6 — missing nuget version: warning + steer, chain continues.
- W7 — failed release consumers build: steer + stop.
- W8 — missing GitHub draft: warning + steer after retries.
- W9 — draft appears on a retry: published toast, no steer.
- W10 — Release chain polls def 23, nugets on nuget.org, FULL release.
- W11 — missing GH_TOKEN: warning + steer naming the token.
- W12 — empty first polls retry, chain completes, no give-up.
- W13 — Release nugets missing on nuget.org: warning + steer, chain continues.
- W14 — expand lab polls def 94; `26.1.400.0` matches feed `26.1.400`.
- W15 — failed poll with a LOGSTART block: steer carries
  `Release 26.1.301.1 exists`, not "no error lines".
- W16 — finished build of a different version is not this run: wait
  toast, then give-up steers.
- W17 — nugets missing on the first assert, present on retry (index
  lag): confirmed toast, no warning, no steer.
