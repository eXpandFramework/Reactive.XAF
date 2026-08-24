---
name: reactive-xaf-build/delegate-tests
description: Use when the /devexpress delegation fallback misbehaves or its tests change — delegate-tests.ts pins that a spawned window dying during the boot grace period makes the menu flow fall back to the invoking session, and a surviving window is delegated to. Read before editing delegation behavior or these tests.
---

# delegate-tests.ts — delegation fallback behavior contract

Companion of `.pi/extensions/reactive-xaf-build/delegate-tests.ts`. Pins the
OBSERVABLE contract of the liveness-verified delegation: the flow must never
hand off to a dead window.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/delegate-tests.ts`

## Harness (mock pi, injected delegate deps)

The REAL `defaultDelegateWindow` runs with injected `DelegateDeps`
(`run`, `windowExists`, `killWindow`, `graceMs`) — the real psmux CLI is
never touched. The menu path is driven through `registerBuildCommand` with
`delegateWindow` wrapping the real function. `TMUX_PANE` is set/restored
around the suite.

## Contracts

- **S0** — the command registers through the real index boot (`activate(pi)`).
- **S1** — spawned window dies during the grace (`windowExists` false):
  the window is killed, `defaultDelegateWindow` returns null, and the menu
  flow (Publish → Lab) falls back to the invoking session — prx runs there,
  no brx, result "published".
- **S2** — surviving window: "delegated to window N", nothing runs in the
  invoking session (no prx).
- **S3** — outside psmux (`TMUX_PANE` unset): null, nothing spawned.

## Notes

- The file is self-contained (build-tests.ts sits at the 400-line gate and
  runs its suite on import, so it cannot be imported).
- The task text passed to the real spawn contains no single quotes (the
  pwsh interpolation constraint from delegate.md).
