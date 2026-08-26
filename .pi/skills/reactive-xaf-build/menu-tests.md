---
name: reactive-xaf-build/menu-tests
description: Use when changing the /devexpress skip-build publish surface — Publish → RX-XAF | eXpand → Lab | Release, the /devexpress publish lab|release arg, and the "Publish (N files)" commit label.
---

# menu-tests.ts — skip-build behavior contract

Companion of `.pi/extensions/reactive-xaf-build/menu-tests.ts`. Pins the
OBSERVABLE contract of the skip-build variant: publish-only, no local build.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu-tests.ts`

## Harness (mock pi, injected seams)

`registerBuildCommand(pi, { run, fetchFeed, repoRoot, pollMs, ...paneSeams })`
with fakes. The repo fixture is a temp dir with `src/Extensions` + a
`Directory.Packages.props` so the repo guard passes.

## Contracts

- **S0** — the command registers through the real index boot (`activate(pi)`).
- **S1** — menu Publish → RX-XAF → Lab + Publish confirm: no `brx`, no pane
  opened/sent, `prx` runs, watcher started ("monitoring in background"), result
  "published".
- **S2** — direct arg `["publish", "lab"]` + Publish confirm: same — no `brx`,
  `prx` runs, published (no project pick; current profile).
- **S3** — skip-build with a dirty tree: commit message is
  `Publish (1 files)`, not `Build fixes (1 files)`; then published.
- **S4** — the Publish menu pick runs the publish flow in the invoking
  window (`prx` runs, published).
