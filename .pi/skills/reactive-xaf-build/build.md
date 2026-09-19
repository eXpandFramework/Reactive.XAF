---
name: reactive-xaf-build/build
description: Use when changing the /devexpress flow engine — DX check, optional depPins, the background build run (supervisor script in a pane, watched out of band), the publish continuation, the failure and stall reports, and the runners the command surface drives.
drop-names: profile.name
---

# build.ts — the /devexpress flow engine

Companion of `.pi/extensions/reactive-xaf-build/build.ts`. All side effects go
through injectable `BuildSeams` (tests pass fakes). Repo-specific values come
from `seams.profile ?? rxProfile` (`profile.ts`).

This module is pure ENGINE: it imports no surface module. The command and its
registration live in `menu.ts`, which drives this engine through the runners
built by `createFlowRunners`. The run's lifecycle (`newRunId`, `runPaths`,
`writeRunScript`, `supervisorCommand`, `pruneRunDirs`, `trackedWrite`) belongs
to `run.ts`.

## Phases (`runBuildFlow`)

1. **DX check** — `getLatestDx`: max stable `DevExpress.ExpressApp`.
2. **Props compare** (`dxPhase`) — DevExpress.* pins: mixed → untouched;
   single shared version ≠ latest → Update | Skip | Abort. `build.ps1 -version`
   is aligned when the pins are at latest (or after an update): Lab → the
   DX-derived base (`dxBaseVersion`, 26.1.4 → 26.1.400.0); Release → the next
   release after the last published `profile.nugetId` on the Xpand server and
   nuget.org (same DX minor, build + 1, revision 0 — 26.1.400 → 26.1.401.0).
   A failed consultation aborts the flow; no silent fallback to the DX base.
3. **depPins** (`depPinsPhase`) — skipped when `profile.depPins` is unset
   (RX). Expand: latest `Xpand.Extensions` from the matching feed, rewrite
   `Xpand.Extensions*` / `Xpand.XAF.*` ask-first.
4. **Build** (`startBuildPhase`) — `profile.buildCmd` in a right-side pane,
   now via the run's supervisor script (`run.ts`): the pane is opened, the
   supervisor line is typed, and the phase returns on a STARTED build, where
   `startBuildRun` takes over. No pane → the same command runs in-process
   through `watchInProcessRun`. Either way the command never awaits the build,
   so the agent stays reachable.
5. **Publish** (`publishPhase`) — runs from the watch when the marker reports
   exit 0: VMs C11–C14 → commit → optional
   `git push ${profile.pushRemote} HEAD:master` → `profile.queueCmd` →
   `monitorPhase` (the AzDO watcher starts and the report returns).

## The watch's half (`buildRunReporter`)

- `done` 0 → `notes.push("build succeeded")`, then `finishPublish`.
- `done` ≠ 0 → `finishMessage` → `failureResult` with the exit code and the
  bounded pane tail, plus the marker note when the code was unreadable.
- `died` → "the build pane is gone and no exit code was written" plus the tail.
- `stall` / `overrun` → one report each, nothing killed.

The strings and the delivery live in `report.ts`; `warn(pi, ctx, msg)` pairs a
toast with a steer.

## Seams

`BuildSeams.profile?: RepoProfile` (default `rxProfile`). `ghFetch` for the
watcher's GitHub step. The run watch adds `probePane`, `sampleCpu`, `runPaths`
and `runCadence`. `delegateWindow` was removed 2026-08-25 and the flow never
consults a window.

## The surface boundary

`createFlowRunners(pi, ctx, merged, cwd)` returns the two entries the command
surface calls: `FlowRunner` (`runFlow(choice, skipBuild?, projectPick?)`) and
`WatchStarter` (`startWatch(choice, projectPick?)`). Both resolve the profile
and repo from the project pick (`seamsForPick`) and answer a loud "not inside
the repo" message when the tree is missing.

## Skip-build variant

`skipBuild = true`: phases 1–4 omitted. Publish + watcher run unchanged, in
the command call itself (nothing to watch).

## Outcome strings

`summaryResult` / `failureResult` use `profile.label` (RX: "Reactive.XAF
build — <choice>"); the older note in this file said `profile.name`, which
`RepoProfile` never had. User aborts never deliver. Real failures steer.
`startedMessage` names the pane and the run id.
