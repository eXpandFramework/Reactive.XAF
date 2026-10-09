---
name: reactive-xaf-build/profile-tests
description: Behavior contract for RepoProfile — RX default vs expandProfile driven through registerBuildCommand on pi's own runtime. Detect, cmds, menu pick, status def, git push then px.
---

# profile-tests.ts

Companion of `.pi/extensions/reactive-xaf-build/profile-tests.ts`. Drives the
Build and Last-build-status menu items through pi's OWN runtime — the
resolver-first route this file carries itself, in the shape `menu-tests.ts`
established — with the extension's OWN ports injected: the command runner, the
feed fetcher and the pane seams, all wired through `registerBuildCommand` (the
entry `menu.ts` exposes). The hand-written pi this suite used to build (`mkPi`
plus a `mkCtx` answering from a pick list) is GONE; the picks are keyed by a
substring of each prompt's title, and a prompt the case did not map fails the
case. The flow's report is asserted where the user meets it (`noticesOf`, over
`handle.host.notices`), because pi discards a command handler's return.

The pane seams install every seam the run watch reads, `sampleCpu` included: a
suite that leaves it out gets the real host sampler, whose call never settles in
the test island, and the watch's first tick wedges with `polling` still true —
the run then reports nothing and the next flow is refused as already running.
The real nuget.org, pwsh, psmux and AzDO are never touched.

Run: `npx tsx .pi/extensions/reactive-xaf-build/profile-tests.ts`

- P0 — index still registers `/devexpress`.
- P1 — RX detect rejects a foreign tree (zero commands, and the refusal itself
  is a warning toast now — pinned by `build-tests.ts` T2).
- P2 — expandProfile: menu offers the Project pick, `bx lab` in the pane,
  `git push lab` then `px`, published.
- P3 — expand status queries def 94; RX status still queries def 23.
- P4 — default profile still sends `brx` and `prx`.
