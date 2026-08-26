---
name: reactive-xaf-build/release-tests
description: Use when changing the build.ps1 version bump — expand Release feed consultation (next release after the last published on the Xpand server + nuget.org), Lab DX base, loud abort on consultation failure.
---

# release-tests.ts — build.ps1 version bump contract

Companion of `.pi/extensions/reactive-xaf-build/release-tests.ts`. Mock-pi
harness with injected seams; drives `/devexpress` via `registerBuildCommand`.

- R0 — `/devexpress` registers through the real index boot.
- R1 — expand Release consults the feeds and bumps `build.ps1 -version` past
  the last published version (26.1.400 → 26.1.401.0), notes the bump, publishes
  via `px -Release`.
- R2 — nothing published on the DX minor → the DX-derived base stays
  (26.1.400.0).
- R3 — expand Lab bumps to the DX base and never consults the feeds (the feed
  fake throws on any consult URL — a consult would abort the flow).
- R4 — the Xpand server consultation fails → the flow aborts with the reason,
  no commands run, nothing written.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/release-tests.ts`
