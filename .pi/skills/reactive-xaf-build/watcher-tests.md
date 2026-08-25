---
name: reactive-xaf-build/watcher-tests
description: Behavior contract for the AzDO chain watcher — toast per poll, chain advance (23 → 72 → 89 for both choices) with nuget assertion and GitHub DRAFT publish (PATCH, GH_TOKEN; race retry), empty-poll retry, missing-token steer, terminal steer, give-up, replace — via the /devexpress command with short injected intervals.
---

# watcher-tests.ts — watcher behavior contract

Companion of `.pi/extensions/reactive-xaf-build/watcher-tests.ts`. Drives
`/devexpress publish lab|release` with a mock pi; the flow starts the REAL
watcher through an injected 20 ms interval seam (the 60 s default would make
each test a minute long). Fake run seam serves CRLF STATUS= fixtures per
chain step (matched on the status script's $top=5 so the release queue
script is not mistaken for a poll) and scripts VM/git/prx/queue; the fake
feed serves the v2 OData fixture; the fake ghFetch serves the GitHub
releases fixture (attempt-numbered for race tests) and records the PATCH
bodies. No real pwsh/AzDO/nuget/GitHub.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts`

## Pinned behaviors

- W1 — full chain green (Lab): immediate return, toast on EVERY poll, nugets
  asserted, advance to the release consumers pipeline, the GitHub DRAFT is
  published via PATCH with `prerelease:true` + chain-complete toast,
  stopped, no steer.
- W2 — failed Reactive.XAF build: warning toast (FAILED label) + steer,
  then stop.
- W3 — give-up deadline: warning + stop.
- W4 — a new publish replaces the previous watcher; `stopAzDoWatcher`
  stops it.
- W5 — command registers through the real index boot.
- W6 — missing nuget version: warning + steer ("NOT found"), chain
  continues to the release step.
- W7 — failed release consumers build: steer + stop.
- W8 — missing GitHub draft: warning + steer after the retry budget
  ("NOT found after N tries"), watcher stops.
- W9 — the draft appears on a retry (creation race absorbed): published
  toast, no steer.
- W10 — Release chain: polls def 23 (same pipeline as lab), publishes the
  draft as a FULL release (`prerelease:false`), no steer.
- W11 — missing GH_TOKEN: warning + steer naming the token (never a
  silent skip), watcher stops.
- W12 — empty first polls (queue API lag): retried with a "no build found
  yet" toast, chain completes, no give-up, no steer.

W1 counts running toasts — that's the toast-per-poll contract. Steers
assert delivery (recorded `_userMessages`), not the steer type.
