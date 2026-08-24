---
name: reactive-xaf-build/build
description: The /devexpress flow engine — DX check, props update, brx build in a pane, publish (VM check → commit → prx → AzDO monitor), failure steer, and the skip-build publish-only variant. Read when changing build/publish behavior, seams, or the failure path.
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
   pins via `readDxPins`: no pins → noted; mixed versions → surfaced, file
   untouched; single shared version ≠ latest → `ctx.ui.select` Update | Skip |
   Abort (Update rewrites all DevExpress.* pins via `rewriteDxVersion` +
   `trackedWrite`, i.e. `__writeFileSync`).
3. **Build** (`buildPhase`) — `brx` / `brx -Release` in a new right-side pane
   (pane.ts seams); in-process fallback when the pane cannot be opened.
   Red → `failureResult` (captured tail), notify, `steerFailure` (warning
   steer with triggerTurn, type `reactive-xaf-build:build-failed`), pane kept.
4. **Publish** (`publishPhase`) — `ensureVmsRunning` (C11–C14; Off → Start-VM
   + poll, Starting → wait) → `commitPhase` (git status → confirm → add -A →
   commit; message `Update DX to X` when props changed, else
   `Build fixes (N files)` — skip-build mode labels it `Publish (N files)`;
   nothing to commit → skip) → confirm `prx` / `prx -Release` (600 s timeout)
   → `monitorPhase`.
5. **Monitor** (`monitorPhase`) — `waitForAzDoBuild` (azdo.ts, 1 h deadline):
   polls the newest Reactive.XAF build; succeeded/canceled → ok, failed →
   reason + `AZDO_BUILD_URL`, timeout/other → failed with note. Failure steers
   via `steerFailure` (no auto-fix, no auto re-run — the agent plans and asks).

## Skip-build variant

`skipBuild = true` (menu "Lab (skip build)" / "Release (skip build)", arg
`publish lab|release`): phases 1–3 are omitted (no DX feed call, no pane, no
`brx`), the summary notes "build skipped — publish only", and the DX line
reads "no DX check (build skipped)". Publish + monitor run unchanged.

## Outcome strings

- `summaryResult` — "Reactive.XAF build — <choice>" + DX line + notes +
  "published" / "publish stopped" (+ close-pane ask when a pane is open).
- `failureResult` — build FAILED with exit code + output tail (4k) +
  "Fix the warnings, then re-run /devexpress."

## Failure policy

User aborts (DX Skip/Abort, commit Abort, publish Abort) never steer. Real
failures (build or AzDO) steer with triggerTurn so the agent plans a fix and
presents it — user permission is ALWAYS required before any action.
