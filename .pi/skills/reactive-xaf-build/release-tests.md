---
name: reactive-xaf-build/release-tests
description: Use when changing the build.ps1 version bump — expand Release feed consultation (next release after the last published on the Xpand server + nuget.org), Lab DX base, loud abort on consultation failure.
---

# release-tests.ts — build.ps1 version bump contract

Companion of `.pi/extensions/reactive-xaf-build/release-tests.ts`. Drives the
Build menu item through pi's OWN runtime — the resolver-first route this file
carries itself, in the shape `menu-tests.ts` established — with the extension's
OWN ports injected: the command runner, the feed fetcher and the pane machinery,
all wired through `registerBuildCommand` (the entry `menu.ts` exposes), which is
what `./index.ts` hands pi at boot. The hand-written pi this suite used to build
(`mkPi` plus a `mkCtx` answering from a pick list) is GONE; the picks are keyed
by a substring of each prompt's title (`buildPicks`), and a prompt the case did
not map fails the case.

`release.ts` writes the version, so a case reads the real `build.ps1` in a temp
checkout, and the flow's own report is asserted where the user meets it
(`noticesOf`, over `handle.host.notices`) — pi discards a command handler's
return value. Nothing spawns: the real nuget.org, the Xpand server, pwsh and
psmux are never touched.

The pane seams install **every** seam the run watch reads, `sampleCpu`
included. That one is easy to forget and expensive: a suite that leaves it out
gets `defaultSeams()`'s real host sampler, whose call never settles in the test
island, so the watch's first tick never returns, `polling` stays true forever,
the marker is never consumed, the build is never reported and the next flow is
refused as "already running". This suite was red for exactly that reason before
its conversion (the committed mock-pi version fails the same way).

- R0 — `/devexpress` registers through the real index boot.
- R1 — expand Release consults the feeds and bumps `build.ps1 -version` past
  the last published version (26.1.400 → 26.1.401.0), notes the bump, publishes
  via `px -Release`.
- R2 — nothing published on the DX minor → the DX-derived base stays
  (26.1.400.0).
- R3 — expand Lab bumps to the DX base and never consults the feeds (the feed
  seam THROWS on any URL the case did not set up — a consult would abort the
  flow).
- R4 — the Xpand server consultation fails → the flow aborts with the reason,
  no commands run, nothing written.

Run: `npx tsx .pi/extensions/reactive-xaf-build/release-tests.ts`
