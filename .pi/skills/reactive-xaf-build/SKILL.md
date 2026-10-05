---
name: reactive-xaf-build
description: Use when working on or invoking the /devexpress extension in Reactive.XAF — the Lab/Release build-publish flow (DX check, brx in a pane, commit, queue, AzDO monitor, GitHub draft publish) and its skip-build publish-only variant, including the command menu, the flow engine, RepoProfile, and their tests.
---

# /devexpress (reactive-xaf-build)

Repo-local extension at `.pi/extensions/reactive-xaf-build/` (auto-loads in
Reactive.XAF sessions). One command: `/devexpress` — the Lab/Release build +
publish workflow. Repo-specific values live on `RepoProfile` (`profile.ts`);
RX is the default. Expand is a second profile, picked from the same menu.

Both RX choices run the `Reactive.XAF` pipeline (**def 23**) — Lab queues
branch lab, Release (`prx -Release`) queues branch master — followed by
PublishNugets (**def 72**) and release consumers (**def 89**).

Expand Lab queues **def 94** (`Xpand-Lab`; `px`). Def 32 is `_Xpand-Lab`,
a 2023 leftover. Release is def 39 → 38 → 37.

## Command surface

- `/devexpress` — interactive menu: Build | Publish → **RX-XAF | eXpand**
  → Lab | Release, plus "Last build status", "Cancel AzDO build" and one of
  "Abort build" (a run is being watched) or "Close build pane" (a pane is
  open, nothing running).
- No arguments, no flags and no subcommand words: every capability is a menu
  pick. The old words `/devexpress status`, `/devexpress cancel`,
  `/devexpress watch`, `/devexpress build lab|release` and
  `/devexpress publish lab|release` are gone (CLI Flag Gate) — the same
  actions are the menu items "Last build status", "Cancel AzDO build",
  "Start AzDO watcher", Build and Publish, each followed by the Lab | Release
  pick it needs.

Menu picks run in the INVOKING window. The eXpand pick uses
`resolveRepo` (cwd, then known roots only when cwd is the other repo).

## Flow (Lab | Release)

1. **DX check** — nuget.org flat-container, max stable `DevExpress.ExpressApp`.
2. **Props compare** — `Directory.Packages.props` DevExpress.* pins. `build.ps1`
   version: Lab → DX base; Release → next after the last published version on
   the feeds (Xpand server + nuget.org, e.g. 26.1.401.0 after 26.1.400).
3. **depPins** (expand only) — latest `Xpand.Extensions` from the matching feed.
4. **Build** — `profile.buildCmd` in a right-side pane, driven by a per-run
   supervisor script (`run.ts`) whose exit code lands in a transient marker.
   The command returns on a STARTED build; `run.ts` watches the run out of
   band (marker, pane death, a 10-minute silence-plus-CPU-idle stall, a
   20-minute overrun) and reports. Nothing in the watch kills a build —
   `/devexpress` → "Abort build" is the only deliberate stop.
5. **Publish** — `publishPhase` starts the VM probe and reads its outcome
   AFTER the commit prompt: C11–C14 must answer Running, `Off`/`Saved` are
   Start-VM'd, and anything unreadable, missing or unstartable THROWS
   `VmProbeError` out of `planVms`, stopping the QUEUE with a warning steer —
   a green build's commit stands either way. The probe runs profile-free and is
   retried once (a nonzero exit, a thrown seam, or an exit-0 read that named no
   agent); a refusal carries the probe's own output. The agents are pre-warmed
   at build start (`prewarmVms`): a readable probe starts what can start, an
   unreadable one blind-starts C11-C14, and the build never fails over it (a
   `steerWatch` warning, no model turn). Then the commit step, the pre-push
   dirty check on the profiles that push, `profile.queueCmd`, AzDO watcher
   (`publish.ts`, `gitphase.ts`).

## Module map

| Module | Doc | Purpose |
|---|---|---|
| `index.ts` | — | Boot: registers the command (thin). |
| `profile.ts` | `profile.md` | RepoProfile: RX default + expand. `profileByPick`, `resolveRepo`. |
| `pins.ts` | `pins.md` | Expand-only RX package pin rewrite. |
| `publish.ts` | `publish.md` | The VM layer and the phase order: commit, push, queue, watcher start. |
| `gitphase.ts` | `gitphase.md` | The git half: dirty read + summary, both commit prompts, the commit core. |
| `menu.ts` | `menu.md` | Command surface and composition root: owns the command, drives the engine. |
| `build.ts` | `build.md` | Flow engine (DX, local build start, menu wiring). |
| `run.ts` | `run.md` | Background build run: marker, pane death, stall/overrun, abort. |
| `report.ts` | `report.md` | The flow's messages and the warn/steer pair. |
| `release.ts` | `release.md` | Release version bump (feed consultation). |
| `watcher.ts` | `watcher.md` | Background AzDO chain watcher. |
| `menu-tests.ts` | `menu-tests.md` | Skip-build contract, on pi's own runtime. |
| `resolve.mjs` | `resolve.md` | Node hook: `.js`→`.ts` and the whitelisted `@pi/` floor, installed before any harness import. |
| `delegate-tests.ts` | `delegate-tests.md` | Dormant delegation helper plus the flow publishing locally. |
| `build-tests.ts` | `build-tests.md` | Full flow. |
| `release-tests.ts` | `release-tests.md` | build.ps1 version bump (Release feed consultation). |
| `watcher-tests.ts` | `watcher-tests.md` | Watcher W1–W18. |
| `profile-tests.ts` | `profile-tests.md` | RepoProfile (RX vs expand). |
| `azdo.ts` / `status.ts` | `azdo.md` | AzDO status/cancel. |
| `delegate.ts` | `delegate.md` | Dormant. |
| `pane.ts` | `pane.md` | Pane seams: open, send, capture, close, probe, CPU sample. |

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/{menu,build,watcher,azdo,profile,release,delegate}-tests.ts`

## Test harness route (project tree)

The suites drive the extension through pi's OWN loader and `ExtensionRunner`
(pi-dev's `buildRealRunner`), never a hand-written fake pi. The harness lives
in the home tree, unreachable by a relative import from this repo: copy
`menu-tests.ts`; `menu-tests.md` has the shapes and the traps.

1. **Resolver first.** `./resolve.mjs` maps `.js`→`.ts` and resolves
   `@pi/<name>/...` into the home extension tree, gated by the platform's
   `shared-utilities.json`. Install it:
   `await import(new URL("./resolve.mjs", import.meta.url).href)`.
2. **Harness by dynamic import only.** A static import is linked before the
   hook exists: hold `buildRealRunner` in a module-scope `let` and import every
   `@pi/...` module from inside a function.
3. **Import the seam-owning modules, never stub them.** The extension reads
   `__steer` (llm-utils) and `__writeFileSync` (tracked-write) off globalThis;
   a process-boundary stub is hard-blocked by the write gate.
4. **`buildRealRunner({ activate, cwd, ui })`, one route only**: `activate`
   wires the real modules, and the extension's own ports (runner, panes,
   watcher, `repoRoot`, `fetchFeed`, `pollMs`) go INSIDE it — why not `entry:`,
   which cannot inject ports. `cwd` is what the repo guard reads. **`ui.select`
   answers the FIRST option**, so every case answers the prompts itself and
   fails on a title it did not expect.
5. **Assert on `handle.host`** (`notices`, `messages`, `prompts`, `errors`):
   `runCommand` returns only a boolean, and a throwing handler is REPORTED, not
   thrown — so every case asserts `errors` is empty. Dispose every handle;
   clean fixture dirs in a `finally`.
6. **Prove the boot**: `ensureBootProof("<ext>", entry)` in a straight-line
   function body, asserted as `boot.ok` with
   `JSON.stringify(boot).slice(0, 200)`. `entry` must be relative to
   `<agentDir>/extensions` — the one base pi's `-e` and the proof's key both
   join — and comes from the suite's own URL. Take the dir from pi-runner's
   `resolveAgentDir()`; do NOT pass it to `ensureBootProof` (an explicit one
   roots the ledger in the caller). Module scope is unreachable here (the
   binding needs a dynamic import; top-level await is refused), so keep the call
   clear of any branch, loop, `try` or short-circuit. In a `b:\Temp` island B0
   is red (pi's spawn also loads `dependency-manager` from that base, which the
   island's agent dir lacks) and green in the real tree — `menu-tests.md` has
   the measurement. Budget it with `// test-timeout: 120000`.
