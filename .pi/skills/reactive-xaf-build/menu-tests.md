---
name: reactive-xaf-build/menu-tests
description: Behavior contract for the /devexpress skip-build publish surface — menu picks "Lab (skip build)"/"Release (skip build)", the /devexpress publish lab|release arg, delegation task text, and the "Publish (N files)" commit label. Read before changing the skip-build flow or these tests.
---

# menu-tests.ts — skip-build behavior contract

Companion of `.pi/extensions/reactive-xaf-build/menu-tests.ts`. Pins the
OBSERVABLE contract of the skip-build variant: publish-only, no local build.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu-tests.ts`

## Harness (mock pi, injected seams)

`registerBuildCommand(pi, { run, fetchFeed, repoRoot, pollMs, ...paneSeams,
delegateWindow })` with fakes: a scripted command runner, a feed fetcher that
would throw on use (`"[]"` — proves the DX check never runs), pane seams that
record opens/sends, a monitor seam returning a fixed succeeded outcome. The
repo fixture is a temp dir with `src/Extensions` + a `Directory.Packages.props`
so the repo guard passes. The real nuget.org, pwsh, psmux, VMs and git are
never touched.

## Contracts

- **S0** — the command registers through the real index boot (`activate(pi)`).
- **S1** — menu "Lab (skip build)" + Publish: no `brx`, no pane opened/sent,
  `prx` runs, monitor awaited ("AzDO build 1 succeeded"), result "published".
- **S2** — direct arg `["publish", "lab"]` + Publish confirm: same — no `brx`,
  `prx` runs, published.
- **S3** — skip-build with a dirty tree: commit message is
  `Publish (1 files)`, not `Build fixes (1 files)`; then published.
- **S4** — the skip-build menu pick delegates with the `publishTask` text
  (contains `/devexpress publish lab`).

## Notes

- The file is deliberately self-contained: `build-tests.ts` sits at the
  400-line gate and runs its suite on import, so it cannot be imported.
- Placeholder checks fail loudly — e.g. a regression that reintroduces the DX
  feed call throws inside the fake fetcher.
- The full-flow suite (build/publish/failure/monitor/delegation, 52 checks)
  lives in `build-tests.ts`; keep its Section T1–T20 numbering untouched.
