---
name: reactive-xaf-build/menu-tests
description: Use when changing the /devexpress skip-build publish surface — Publish → RX-XAF | eXpand → Lab | Release, the inert leftover argument, and the "Publish (N files)" commit label — or when converting another suite in this extension onto pi's real runtime.
---

# menu-tests.ts — skip-build behavior contract

Companion of `.pi/extensions/reactive-xaf-build/menu-tests.ts`. Pins the
OBSERVABLE contract of the skip-build variant: publish-only, no local build.

Run: `npx tsx .pi/extensions/reactive-xaf-build/menu-tests.ts`

## Harness (mock pi, injected seams)

RETIRED, and replaced by the route below. The suite used to hand-build a pi
object and fire the command through a couple of injected seams, which proved the
fake and not the extension: nothing tied that object to pi's `ExtensionAPI`, and
renaming a ctx field in pi could not have failed it. What it still injects is the
extension's OWN ports, listed under "The route it drives".

## The route it drives

pi's OWN loader and ExtensionRunner, through the resolver-first route this file
carries itself (`installRoute()`): `./resolve.mjs` installs the `@pi/` name floor
first, and the harness plus the seam-owning modules are DYNAMIC imports after it,
since a static one is linked before the hook exists. `buildRealRunner({ activate
})` builds the extension from its real factory (`./index.ts` →
`registerBuildCommand` from `./menu.ts`), with the extension's own ports wired
inside it — command runner, pane ports, AzDO watcher starter, `repoRoot`,
`fetchFeed`, `pollMs`. Those are the extension's injectable seams, not a pi
stand-in, which is why this suite uses the factory route rather than `entry:`
(the entry route cannot inject ports).

The harness host answers `ui.select` with the FIRST option, so every case
ANSWERS the flow's prompts itself — by title substring, because the commit
prompt spells the whole message and the dirty summary — and fails when the flow
shows a prompt the case did not map, or when a mapped answer is not one of the
offered options. Without that, a Publish case would silently drive the Build
branch and pass for the wrong reason.

Which module is which:

- `./resolve.mjs` (this dir) is installed as the first statement of `main()`:
  it maps `.js` onto its `.ts` sibling and resolves `@pi/<name>/...` into the
  home pi tree, gated by the platform's `shared-utilities.json`. Every harness
  import is therefore DYNAMIC and comes after the install — `real-runner`,
  `llm-utils` (publishes `__steer`), `tracked-write` (publishes
  `__writeFileSync`). The extension reads those two globals at run time
  (`report.ts` steers, `run.ts`/`pins.ts` writes), so the seam is the real
  module's publication and never a stub.
- `./menu.ts` — the surface under test (the composition root and the picks).
- `./index.ts` — S0's subject: the real boot entry.

The repo fixture is a temp dir with `src/Extensions` +
a `Directory.Packages.props`, so the repo guard (`resolveRepo`) passes and no
command ever touches the real checkout. Assertions read the harness's host
capture: `handle.host.notices` (the messages the flow showed the user) and
`handle.host.errors`, which EVERY case asserts is empty — the runner reports a
throwing handler instead of throwing, so an error is otherwise a no-op.

## Contracts

- **R1** — the process seams the extension reads off globalThis are the real
  modules' publications.
- **R2** — the `@pi` floor refuses a name the platform does not share: a
  whitelist, not a hole into the install.
- **S0** — the command registers through the real index factory, no handler
  error.
- **S1** — Publish → RX-XAF → Lab on a clean tree: no `brx`, no pane opened or
  sent, `prx` runs, the watcher starts ("monitoring in background"),
  "published".
- **S2** — a leftover argument (`/devexpress publish lab`) is inert: the top
  menu still opens and drives the same flow.
- **S3** — a dirty tree: the commit is labeled `Publish (1 files)`, not
  `Build fixes (1 files)`; then published.
- **S4** — the publish pick runs in the invoking window: the menu opened, `prx`
  ran, "published".

## What the harness cannot cover

- the BOOT — this suite no longer proves it (see below): every case builds
  through pi's own loader in-process, with only the host stubbed;
- pi's loader TS/alias path for a factory build: this suite imports the
  extension's modules into the test process, so it does not prove jiti loads
  them (the `entry:` route is what covers that, and it cannot inject ports);
- UI rendering, and the real psmux/pwsh/git/Hyper-V/nuget.org: the case's own
  ports stand in for those, which is the point — nothing real is touched.

## The boot proof

Retired 2026-10-08 with the last suite conversion. What stood here was
`ensureBootProof(ext, entry)` — the shared ledger's recorded proof that the
extension's current sources boot under a real pi — with `bootEntry` and
`resolveAgentDir` building the key and a `B0` case asserting `boot.ok`. It is
GONE, and the `boot-proof.js` import with it: it was the one place in this repo
where a test started a real pi. Every suite in this extension now builds through
pi's own loader in-process, nothing spawns, and B0 has no subject left.

### The island limit (measured)

The limit recorded here retired with the proof. It was the platform's spawn, not
this suite: `runPi` appends `dependency-manager/index.ts` to every spawn's `-e`
list against the same agent dir the entry uses, and the island's root is
`b:\Temp` while the install sits on `C:`. Passing the install as the agent dir
put the entry on another drive, where `relative()` returned an absolute path
that `join()` mangled, so the key failed before any spawn (`ok:false,
status:null`); letting the platform resolve it booted the island's copy, whose
`<island>/agent/extensions/` carries only the gitignored home extensions, so
`dependency-manager` was a phantom path and pi exited 1 — B0 then failed with
`status:1`, while the real tree resolved the agent dir to the install and booted
(`status:0`, measured 8,967 ms). Without a spawn the suite is island-green as it
stands and needs no exemption.
