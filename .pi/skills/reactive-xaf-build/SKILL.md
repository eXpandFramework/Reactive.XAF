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
   supervisor script (`pane.ts`) whose exit code lands in a transient marker.
   The command returns on a STARTED build; `run.ts` watches the run out of
   band (marker, pane death, a 10-minute silence-plus-CPU-idle stall, a
   20-minute overrun) and reports. Nothing in the watch kills a build —
   `/devexpress` → "Abort build" is the only deliberate stop.
5. **Publish** — `publishPhase` gates on the VM probe first: C11–C14 must
   answer Running, `Off`/`Saved` are Start-VM'd, and anything unreadable,
   missing or unstartable THROWS `VmProbeError` out of `planVms`, stopping the
   publish before the commit with a warning steer. The probe runs profile-free
   and is retried once. The agents are pre-warmed at build start
   (`prewarmVms`): a readable probe starts what can start, an unreadable one
   blind-starts C11-C14, and the build never fails over it (a `steerWatch`
   warning, no model turn). Then commit, optional `git push`,
   `profile.queueCmd`, AzDO watcher (`publish.ts`).

## Module map

| Module | Doc | Purpose |
|---|---|---|
| `index.ts` | — | Boot: registers the command (thin). |
| `profile.ts` | `profile.md` | RepoProfile: RX default + expand. `profileByPick`, `resolveRepo`. |
| `pins.ts` | `pins.md` | Expand-only RX package pin rewrite. |
| `publish.ts` | `publish.md` | VMs, commit, queue, watcher start. |
| `menu.ts` | `menu.md` | Command surface and composition root: owns the command, drives the engine. |
| `build.ts` | `build.md` | Flow engine (DX, local build start, menu wiring). |
| `run.ts` | `run.md` | Background build run: marker, pane death, stall/overrun, abort. |
| `report.ts` | `report.md` | The flow's messages and the warn/steer pair. |
| `release.ts` | `release.md` | Release version bump (feed consultation). |
| `watcher.ts` | `watcher.md` | Background AzDO chain watcher. |
| `menu-tests.ts` | `menu-tests.md` | Skip-build contract. |
| `delegate-tests.ts` | `delegate-tests.md` | Delegation fallback. |
| `build-tests.ts` | `build-tests.md` | Full flow. |
| `release-tests.ts` | `release-tests.md` | build.ps1 version bump (Release feed consultation). |
| `watcher-tests.ts` | `watcher-tests.md` | Watcher W1–W16. |
| `profile-tests.ts` | `profile-tests.md` | RepoProfile (RX vs expand). |
| `azdo.ts` / `status.ts` | `azdo.md` | AzDO status/cancel. |
| `delegate.ts` | `delegate.md` | Dormant. |
| `pane.ts` | `pane.md` | Pane seams: open, send, capture, close, probe, CPU sample. |

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/{menu,build,watcher,azdo,profile}-tests.ts`
