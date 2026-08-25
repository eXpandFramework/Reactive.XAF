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
  plus "Last build status", "Cancel AzDO build" and "Close build pane" while a build pane is open.
- `/devexpress status` — one-shot AzDO status of the newest Reactive.XAF build.
- `/devexpress cancel` — PATCH-cancel the newest running Reactive.XAF build.
- `/devexpress watch` — start the chain watcher for the current build (no
  publish flow): toasts per check, follows Reactive.XAF → PublishNugets →
  release consumers, asserts the nugets on the eXpand server, steers on
  failure.
- `/devexpress build lab|release` — full flow: DX check → props update
  (ask-first) → `brx` local build in a pane → publish.
- `/devexpress publish lab|release` — skip-build variant: publish only, no
  DX check, no `brx`.

Menu picks delegate to a NEW psmux window (`pi '<task>'` boot message) so the
flow survives the invoking session's close; when no window can be spawned,
or the spawned window dies during the boot grace period, the flow falls back
to running in the invoking session.

## Flow (Lab | Release)

1. **DX check** — nuget.org flat-container, max stable `DevExpress.ExpressApp`.
2. **Props compare** — `Directory.Packages.props` DevExpress.* pins: single
   shared version → ask update-all / skip / abort; mixed → untouched, surfaced.
3. **Build** — `brx` (Lab) / `brx -Release` (Release) in a new right-side pane;
   in-process fallback. Red → failure steer with `triggerTurn`, pane kept.
4. **Publish** — Hyper-V agents C11–C14 ensured running, git commit of build
   state (confirmed), `prx` / `prx -Release` (confirmed), then the **AzDO
    background watcher** (watcher.md): the turn returns immediately — a
    timer polls the chain (Reactive.XAF → PublishNugets → release
    consumers) and TOASTS ON EVERY CHECK; a terminal failure steers via
    `sendUserMessage` (turn-independent) with the real `##[error]` reason
    (wrapper noise filtered); the nugets step asserts the packages on the
    eXpand nuget server. The chat is never locked.
5. **Skip-build variant** — steps 1–3 omitted; commit message label becomes
   "Publish (N files)" instead of "Build fixes (N files)".

## Module map

| Module | Doc | Purpose |
|---|---|---|
| `index.ts` | — | Boot: registers the command (thin). |
| `menu.ts` | `menu.md` | Command surface: menu picks, direct args, delegation. |
| `build.ts` | `build.md` | Flow engine: DX/build/publish phases, seams, steer. |
| `watcher.ts` | `watcher.md` | Background AzDO chain watcher: toast per poll, chain 23→72→89, nuget assertion. |
| `menu-tests.ts` | `menu-tests.md` | Behavior contract for the skip-build surface. |
| `delegate-tests.ts` | `delegate-tests.md` | Behavior contract for the delegation fallback. |
| `build-tests.ts` | `build-tests.md` | Behavior contract for the full flow. |
| `watcher-tests.ts` | `watcher-tests.md` | Behavior contract for the watcher (watcher.md). |
| `azdo.ts` / `status.ts` | `azdo.md` | AzDO status/cancel scripts + fail-reason extraction. |
| `delegate.ts` | `delegate.md` | Window delegation (liveness-verified, fallback). |
| `pane.ts` | — | Build pane primitives. |

Run the contract tests with:
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu-tests.ts`
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/build-tests.ts`
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/watcher-tests.ts`
`npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/azdo-tests.ts`
