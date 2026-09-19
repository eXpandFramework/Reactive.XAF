---
name: reactive-xaf-build/delegate-tests
description: Use when the /devexpress delegation fallback misbehaves or its tests change — delegate-tests.ts pins that a spawned window dying during the boot grace period makes the menu flow fall back to the invoking session, and a surviving window is delegated to. Read before editing delegation behavior or these tests.
---

# delegate-tests.ts — delegation fallback behavior contract

Companion of `.pi/extensions/reactive-xaf-build/delegate-tests.ts`.
`delegateWindow` is dormant (`delegate.ts`; the flow stopped calling it on
2026-08-25), so the /devexpress flow runs in the invoking session and never
consults a window. The helper's own liveness contract stays pinned.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/delegate-tests.ts`

## Harness (mock pi, injected delegate deps)

The REAL `defaultDelegateWindow` runs with injected `DelegateDeps`
(`run`, `windowExists`, `killWindow`, `graceMs`) — the real psmux CLI is
never touched. The menu path is driven through `registerBuildCommand` (from
`menu.ts`, the surface module that owns the command). `TMUX_PANE` is
set/restored around the suite.

## Contracts

- **S0** — the command registers through the real index boot (`activate(pi)`).
- **S1** — spawned window dies during the grace (`windowExists` false): the
  window is killed and `defaultDelegateWindow` returns null.
- **S2** — the flow ignores the dormant seam: menu Publish → RX-XAF → Lab
  publishes in the invoking session (prx runs, no brx).
- **S3** — outside psmux (`TMUX_PANE` unset): null, nothing spawned.

## Notes

- The file is self-contained (build-tests.ts sits at the 400-line gate and
  runs its suite on import, so it cannot be imported).
- The task text passed to the real spawn contains no single quotes (the
  pwsh interpolation constraint from delegate.md).
