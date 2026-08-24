---
name: reactive-xaf-build
description: Use when working on or invoking the /devexpress extension in Reactive.XAF — the Lab/Release build-publish flow (DX check, brx in a pane, commit, prx, AzDO monitor) and its skip-build publish-only variant, including the command menu, the flow engine, and their tests.
---

# /devexpress (reactive-xaf-build)

Repo-local extension at `.pi/extensions/reactive-xaf-build/` (auto-loads in
Reactive.XAF sessions). One command: `/devexpress` — the Lab/Release build +
publish workflow for the AzDO `Reactive.XAF` pipeline (definition 23).

## Command surface

- `/devexpress` — interactive menu: Build → RX-XAF (**Lab** | **Release**,
  full flow) or Publish → RX-XAF (**Lab** | **Release**, skip-build flow),
  plus "Last build status" and "Close build pane" while a build pane is open.
- `/devexpress status` — one-shot AzDO status of the newest Reactive.XAF build.
- `/devexpress build lab|release` — full flow: DX check → props update
  (ask-first) → `brx` local build in a pane → publish.
- `/devexpress publish lab|release` — skip-build variant: publish only, no
  DX check, no `brx`.

Menu picks delegate to a NEW psmux window (`pi '<task>'` boot message) so the
flow survives the invoking session's close; when no window can be spawned the
flow falls back to running in the invoking session.

## Flow (Lab | Release)

1. **DX check** — nuget.org flat-container, max stable `DevExpress.ExpressApp`.
2. **Props compare** — `Directory.Packages.props` DevExpress.* pins: single
   shared version → ask update-all / skip / abort; mixed → untouched, surfaced.
3. **Build** — `brx` (Lab) / `brx -Release` (Release) in a new right-side pane;
   in-process fallback. Red → failure steer with `triggerTurn`, pane kept.
4. **Publish** — Hyper-V agents C11–C14 ensured running, git commit of build
   state (confirmed), `prx` / `prx -Release` (confirmed), then the AzDO build
   monitor: polls the newest Reactive.XAF build to completion; on failure the
   failed record's log supplies the real `##[error]` reason (wrapper noise
   filtered — ScriptHalted/Approve-LastExitCode/retries).
5. **Skip-build variant** — steps 1–3 omitted; commit message label becomes
   "Publish (N files)" instead of "Build fixes (N files)".

## Module map

| Module | Doc | Purpose |
|---|---|---|
| `index.ts` | — | Boot: registers the command (thin). |
| `menu.ts` | `menu.md` | Command surface: menu picks, direct args, delegation. |
| `build.ts` | `build.md` | Flow engine: DX/build/publish phases, seams, steer. |
| `menu-tests.ts` | `menu-tests.md` | Behavior contract for the skip-build surface. |
| `build-tests.ts` | — | Behavior contract for the full flow (52 checks). |
| `azdo.ts` / `status.ts` | — | AzDO monitor/status scripts + fail-reason extraction. |
| `delegate.ts` / `pane.ts` | — | Window delegation, build pane primitives. |

Run the contract tests with:
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu-tests.ts`
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/build-tests.ts`
