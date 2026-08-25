---
name: reactive-xaf-build/watcher
description: The background AzDO build watcher — after prx queues a build, a timer polls the newest Reactive.XAF build and toasts on EVERY check; terminal failure steers via sendUserMessage.
---

# watcher.ts — background AzDO watcher

Companion of `.pi/extensions/reactive-xaf-build/watcher.ts`. Started by
`monitorPhase` (build.ts) after prx queues the build — replaces the old
blocking `defaultWaitForAzDoBuild` (deleted 2026-08-25) that held the
agent's turn for up to 2 h and locked the chat.

## Behavior

- `startAzDoWatcher(pi, ctx, seams, opts?)` — one watcher per session:
  starting a new one stops the previous. Defaults: 60 s interval, 2 h
  give-up deadline.
- Every poll runs the one-shot `azdoStatusScript` via `seams.run` and
  NOTIFIES — the user asked for a toast each time the build is looked at;
  same-type notifies self-replace, so the toast is a live status line.
- Running state → info toast `AzDO <id>: <status> (<elapsed> min) — link`.
- Terminal outcome → final toast; a FAILED build (the FAILED label, same
  convention as status.ts) also delivers `pi.sendUserMessage(msg,
  { deliverAs: "steer" })` — the same turn-independent failure path
  `steerFailure` uses — then stops. The agent plans a fix; user permission
  is ALWAYS required before action.
- Give-up (deadline passed, status script failed, no build found) →
  warning toast + stop; the user is told to check `/devexpress status`.

## State

Registry on `globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]` —
module-level state would duplicate on Windows path-casing double loads.
`stopAzDoWatcher()` / `isAzDoWatcherActive()` manage it; a session reload
kills the timer with the process.

## Seam

`BuildSeams.startAzDoWatcher` (default = the real starter) — tests inject
short intervals/deadlines or a recording fake. Contract:
`watcher-tests.ts` (toast per poll, terminal steer, give-up, replace).
