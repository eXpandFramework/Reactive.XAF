---
name: reactive-xaf-build/build
description: The /devexpress flow engine — DX check, props update, brx build in a pane, publish (VM check → commit → prx → AzDO background watcher), failure steer, and the skip-build publish-only variant. Read when changing build/publish behavior, seams, or the failure path.
---

# build.ts — the /devexpress flow engine

Companion of `.pi/extensions/reactive-xaf-build/build.ts`. Registers the
`devexpress` command via `registerBuildCommand(pi, seams?)`; all side effects
go through injectable `BuildSeams` (tests pass fakes — the real nuget.org,
pwsh, psmux, Hyper-V VMs and git are never touched).

## Phases (`runBuildFlow(pi, ctx, seams, choice, repo, skipBuild = false)`)

1. **DX check** — `getLatestDx`: max stable `DevExpress.ExpressApp` version
   from the nuget.org flat-container.
2. **Props compare** (`dxPhase`) — `Directory.Packages.props` DevExpress.*
   pins via `readDxPins`: no pins → noted; mixed → surfaced, untouched;
   single shared version ≠ latest → `ctx.ui.select` Update | Skip | Abort
   (Update rewrites pins via `rewriteDxVersion` + `trackedWrite` =
   `__writeFileSync`).
3. **Build** (`buildPhase`) — `brx` / `brx -Release` in a new right-side pane
   (pane.ts seams); in-process fallback. Red → `failureResult` (captured
   tail), notify, `steerFailure` (`pi.sendUserMessage(msg, { deliverAs:
   "steer" })` — a triggered turn; the triggerTurn steer started no turn in
   long-lived sessions, 2026-08-25 fix), pane kept.
4. **Publish** (`publishPhase`) — `ensureVmsRunning` (C11–C14; Off →
   Start-VM + poll, Starting → wait) → `commitPhase` (git status → confirm →
   add -A → commit; `Update DX to X` when props changed, else
   `Build fixes (N files)` / skip-build `Publish (N files)`; nothing to
   commit → skip) → confirm `prx` / `prx -Release` (600 s timeout) →
   `monitorPhase`.
5. **Monitor** (`monitorPhase`) — starts the background watcher
   (`seams.startAzDoWatcher`, default watcher.ts) with `followNugets` and
   RETURNS IMMEDIATELY — the chat is never locked. The watcher toasts on
   every check and walks the chain (Reactive.XAF → PublishNugets → release
   consumers), asserting the nugets on the eXpand server at the nugets
   step; any failure steers (`pi.sendUserMessage`, deliverAs "steer").
   No auto-fix, no auto re-run — the agent plans and asks.

## Skip-build variant

`skipBuild = true` (menu Publish → RX-XAF → Lab | Release, arg
`publish lab|release`): phases 1–3 are omitted; the summary notes "build
skipped — publish only". Publish + watcher run unchanged.

## Outcome strings

- `summaryResult` — "Reactive.XAF build — <choice>" + DX line + notes +
  "published" / "publish stopped" (+ close-pane ask when a pane is open).
- `failureResult` — build FAILED with exit code + output tail (4k) +
  "Fix the warnings, then re-run /devexpress."

## Failure policy

User aborts (DX Skip/Abort, commit Abort, publish Abort) never deliver.
Real failures (build or AzDO) deliver a triggered turn (`sendUserMessage`,
deliverAs "steer") so the agent plans a fix and presents it — user
permission is ALWAYS required before any action.
