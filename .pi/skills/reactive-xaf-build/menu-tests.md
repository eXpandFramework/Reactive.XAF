---
name: reactive-xaf-build/menu-tests
description: Use when changing the /devexpress skip-build publish surface — Publish → RX-XAF | eXpand → Lab | Release, the inert leftover argument, and the "Publish (N files)" commit label — or when migrating another suite in this extension onto pi's real runtime.
---

# menu-tests.ts — skip-build behavior contract

Companion of `.pi/extensions/reactive-xaf-build/menu-tests.ts`. Pins the
OBSERVABLE contract of the skip-build variant: publish-only, no local build.

Run: `npx tsx .pi/extensions/reactive-xaf-build/menu-tests.ts`

## Harness (mock pi, injected seams)

RETIRED in this migration, and replaced by the route below. The suite used to
hand-build a pi object and fire the command through a couple of injected seams,
which proved the fake and not the extension: nothing tied that object to pi's
`ExtensionAPI`, and renaming a ctx field in pi could not have failed it. What it
still injects is the extension's OWN ports, listed under "The route it drives".

## The route it drives

pi's OWN loader and ExtensionRunner, through pi-dev's `buildRealRunner`:
`activate` is the extension's real factory (`./index.ts` →
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
  `boot-proof`, `llm-utils` (publishes `__steer`), `tracked-write` (publishes
  `__writeFileSync`). The extension reads those two globals at run time
  (`report.ts` steers, `run.ts`/`pins.ts` writes), so the seam is the real
  module's publication and never a stub.
- `./menu.ts` — the surface under test (the composition root and the picks).
- `./index.ts` — S0's subject: the real boot entry.

The repo fixture is a temp dir with `src/Extensions` + a
`Directory.Packages.props`, so the repo guard (`resolveRepo`) passes and no
command ever touches the real checkout. Assertions read the harness's host
capture: `handle.host.notices` (the messages the flow showed the user) and
`handle.host.errors`, which EVERY case asserts is empty — the runner reports a
throwing handler instead of throwing, so an error is otherwise a no-op.

## Contracts

- **B0** — the extension's current sources boot, proven once per source key by
  the shared boot ledger. The verdict JSON is the failure detail.
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

- the BOOT — B0's ledger call, a real spawned pi;
- pi's loader TS/alias path for a factory build: this suite imports the
  extension's modules into the test process, so it does not prove jiti loads
  them (the `entry:` route is what covers that, and it cannot inject ports);
- UI rendering, and the real psmux/pwsh/git/Hyper-V/nuget.org: the case's own
  ports stand in for those, which is the point — nothing real is touched.

## The boot proof

`ensureBootProof("reactive-xaf-build", entry)` sits in `routeEvidence`'s
straight-line body. It is NOT at module scope, and in a project tree it cannot
be: the binding comes from a dynamic import that must follow the resolver
install (a static `@pi/...` import is linked before any hook exists), and a
module-level dynamic import is top-level await, which the same write gate
refuses ("Top-level await: not supported by tsx/esbuild CJS output" — verified
with a preflight against a probe file). What the placement rule actually
forbids is a proof something can SKIP, and nothing above this call can: no
branch, no loop, no `try`, no short-circuit.

- The agent dir comes from pi-runner's own `resolveAgentDir()` — the install,
  or the test island's `PI_RUNNER_AGENT_DIR` when one is set — so no absolute
  path is committed. It is deliberately NOT handed to `ensureBootProof`: an
  explicit one would root the ledger inside whatever runtime asked, and the
  proof belongs in the install's `<agentDir>/extensions/pi-dev/pi-dev.db`, the
  row every other converted suite shares.
- `entry` is relative to `<agentDir>/extensions` — the one base pi's `-e`
  resolution and the proof's key both join against — taken from this file's own
  URL, which is what makes an island run prove the ISLAND's copy. Project paths
  sit outside the agent dir, so they digest absolute: an island run and a
  real-tree run file different keys and each pays one boot (one row per
  extension is replaced, never duplicated).
- `// test-timeout: 120000` covers that boot plus the five runner builds (the
  runner's default budget is 30s; the island's per-file cap is 120s).

### The island limit (measured)

B0 cannot pass inside the test gate's island, and the reason is in the
platform's spawn rather than in this suite: `runPi` appends
`dependency-manager/index.ts` to every spawn's `-e` list and resolves it
against the same agent dir the entry uses. The island's root is `b:\Temp`; the
install is on `C:`. Both available shapes fail, one requirement each:

- passing the install as the agent dir puts the entry on another drive, where
  `relative()` returns an absolute path that `join()` mangles, so the KEY fails
  before any spawn: `ok:false, status:null`, which the gate read as a
  load-shaped verdict (`LOAD-INCONCLUSIVE ... menu-tests.ts`);
- letting the platform resolve builds the key and boots the island's copy, but
  `<island>/agent/extensions/` carries only the gitignored home extensions, so
  `dependency-manager` is a phantom path: pi exits 1 and B0 fails with
  `status:1` — a counted failure, with the rest of the suite still executing
  (`1 of 8 cases inside`).

So this file is red in the island until the platform either accepts an absolute
entry for a project tree or projects the always-loaded spawn deps into the
island's agent dir. The real tree resolves the agent dir to the install and
boots: measured 8,967 ms, recorded in the ledger with `status:0`.
