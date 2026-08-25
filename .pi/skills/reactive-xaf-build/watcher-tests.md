---
name: reactive-xaf-build/watcher-tests
description: Behavior contract for the AzDO chain watcher — toast per poll, chain advance with nuget + GitHub pre-release assertions (race retry), terminal steer, give-up, replace — via the /devexpress command with short injected intervals.
---

# watcher-tests.ts — watcher behavior contract

Companion of `.pi/extensions/reactive-xaf-build/watcher-tests.ts`. Drives
`/devexpress publish lab` with a mock pi; the flow starts the REAL watcher
through an injected 20 ms interval seam (the 60 s default would make each
test a minute long). Fake run seam serves CRLF STATUS= fixtures per chain
step and scripts VM/git/prx; the fake feed routes per URL: v2 OData for
the eXpand-server assertion, GitHub releases fixture (attempt-numbered for
race tests). No real pwsh/AzDO/nuget/GitHub.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts`

## Pinned behaviors

- W1 — full chain green: immediate return, toast on EVERY poll, nugets
  asserted, advance to the release consumers pipeline, GitHub pre-release
  found + chain-complete toast, stopped, no steer.
- W2 — failed Reactive.XAF build: warning toast (FAILED label) + steer,
  then stop.
- W3 — give-up deadline: warning + stop.
- W4 — a new publish replaces the previous watcher; `stopAzDoWatcher`
  stops it.
- W5 — command registers through the real index boot.
- W6 — missing nuget version: warning + steer ("NOT found"), chain
  continues to the release step.
- W7 — failed release consumers build: steer + stop.
- W8 — missing GitHub pre-release: warning + steer after the retry budget
  ("NOT found after N tries"), watcher stops.
- W9 — the release appears on a retry (creation race absorbed): success
  toast, no steer.

W1 counts running toasts — that's the toast-per-poll contract. Steers
assert delivery (recorded `_userMessages`), not the steer type.
