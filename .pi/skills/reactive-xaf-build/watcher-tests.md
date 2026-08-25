---
name: reactive-xaf-build/watcher-tests
description: Behavior contract for the background AzDO watcher — toast per poll, terminal steer, give-up, replace-on-restart — via the registered /devexpress command with short injected intervals.
---

# watcher-tests.ts — watcher behavior contract

Companion of `.pi/extensions/reactive-xaf-build/watcher-tests.ts`. Drives
`/devexpress publish lab` with a mock pi; the flow starts the REAL watcher
through an injected 20 ms interval seam (the 60 s default would make each
test a minute long). Fake run seam serves CRLF STATUS= fixtures to the
watcher and scripts VM/git/prx. No real pwsh/AzDO/pi.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts`

## Pinned behaviors

- W1 — publish returns immediately ("monitoring in background"), watcher
  active right after, toast on EVERY poll, succeeded terminal stops it,
  no steer.
- W2 — failed terminal: warning toast with the FAILED label + steer
  (`sendUserMessage`, deliverAs "steer"), then stop.
- W3 — give-up deadline: warning + stop.
- W4 — second publish replaces the first (one active); `stopAzDoWatcher`
  stops it.
- W5 — command registers through the real index boot.

W1 counts running toasts — that's the toast-per-poll contract. Steers
assert delivery (recorded `_userMessages`), not the steer type.
