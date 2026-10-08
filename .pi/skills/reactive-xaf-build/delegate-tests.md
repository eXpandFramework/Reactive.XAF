---
name: reactive-xaf-build/delegate-tests
description: Use when the /devexpress delegation fallback misbehaves or its tests change — delegate-tests.ts pins that a spawned window dying during the boot grace period makes the helper answer null, that the flow publishes in the invoking session, and that outside psmux nothing is spawned. Read before editing delegation behavior or these tests.
---

# delegate-tests.ts — delegation fallback behavior contract

Companion of `.pi/extensions/reactive-xaf-build/delegate-tests.ts`.
`delegateWindow` is dormant (`delegate.ts`; the flow stopped calling it on
2026-08-25), so the /devexpress flow runs in the invoking session and never
consults a window. The helper's own liveness contract stays pinned.

The menu path runs through pi's OWN runtime — the resolver-first route this file
carries itself, in the shape `menu-tests.ts` established — with the command
registered on the harness's own api by `registerBuildCommand` (the entry
`menu.ts` exposes) and the extension's OWN ports injected. The picks are keyed by
a substring of each prompt's title. The hand-written pi this suite used to build
(`mkPi` plus a `mkCtx` answering from a pick list) is GONE. The flow's report is
asserted where the user meets it (`noticesOf`, over `handle.host.notices`),
because pi discards a command handler's return.

Run: `npx tsx .pi/extensions/reactive-xaf-build/delegate-tests.ts`

## Harness (mock pi, injected delegate deps)

The mock pi this heading is named for is GONE: what is injected now is the
delegate's own deps, over pi's real runtime (see above). The REAL
`defaultDelegateWindow` runs with injected `DelegateDeps`
(`run`, `windowExists`, `killWindow`, `graceMs`) — the real psmux CLI is
never touched, and no window is ever spawned. `TMUX_PANE` is set for the
suite and restored around it (S3 removes it for its own case). Nothing
spawns: this suite imports no boot proof.

## Contracts

- **S0** — the command registers through the real index boot (`activate`).
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
