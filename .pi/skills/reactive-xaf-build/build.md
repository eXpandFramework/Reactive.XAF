---
name: reactive-xaf-build/build
description: Use when changing the /devexpress flow engine — DX check, optional depPins, the background build run (supervisor script in a pane, watched out of band), the publish continuation, the failure and stall reports, and the runners the command surface drives.
drop-names: profile.name build_stamp head_sha clean_src built_at openDb readDb
---

# build.ts — the /devexpress flow engine

Companion of `.pi/extensions/reactive-xaf-build/build.ts`. All side effects go
through injectable `BuildSeams` (tests pass fakes). Repo-specific values come
from `seams.profile ?? rxProfile` (`profile.ts`).

This module is pure ENGINE: it imports no surface module. The command and its
registration live in `menu.ts`, which drives this engine through the runners
built by `createFlowRunners`. The run's lifecycle (`newRunId`, `runPaths`,
`writeRunScript`, `supervisorCommand`, `pruneRunDirs`, `trackedWrite`) belongs
to `run.ts`. The surfaces are two — `menu.ts` for the user, `tool.ts` for an
agent — and both drive this same engine; neither owns a run.

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
4. **VM pre-warm** (`prewarmPhase` → `prewarmVms`, publish.ts) — after the DX/pin phase and
   before the pane takes the build. A readable probe starts exactly the agents
   that can start, an unreadable one blind-starts C11-C14 (Hyper-V's "already in
   its current state" answer for the agents that are up is tolerated). Never
   waits for Running, never throws. Returns `{ notes, attention }`: the notes
   ride the started message, `attention` adds a `steerWatch` warning that spends
   no model turn. A flow that aborts earlier (DX prompt, feed consultation)
   leaves the VMs alone.
5. **Build** (`startBuildPhase`) — `profile.buildCmd` plus `profile.buildEnv`
   (RX: `MSBuildWarningsAsMessages=MSB3026`, see `profile.md`) in a right-side
   pane, via the run's supervisor script (`run.ts`): the pane is opened, the
   supervisor line is typed, and the phase returns on a STARTED build, where
   `startBuildRun` takes over. No pane → the same command runs in-process
   through `watchInProcessRun` with the SAME env, handed to the command runner
   (`RunOpts.env`, see `pane.md`), so a fallback run cannot lose what the pane
   run would have had. That fallback is a RUN like any other:
   `startBuildPhase` hands `watchInProcessRun` the same handle the pane path
   registers, so `activeBuildRun()` and `isBuildRunActive()` answer for it and
   the running check refuses a second start. Before that they answered null for
   it, the check passed, and two brx processes collided on `obj/`, `bin/`,
   `Build/nuspec/*` and `src/Common/AssemblyInfoVersion.cs`. Either way the
   command never awaits the build, so the agent stays reachable.
6. **Publish** (`publishPhase`) — runs from the watch when the marker reports
   exit 0: the VM gate (C11–C14 must answer Running, see `publish.md`) → commit
   → optional `git push ${profile.pushRemote} HEAD:master` → `profile.queueCmd`
   → `monitorPhase` (the AzDO watcher starts and the report returns).

## The watch's half (`buildRunReporter`)

- `done` 0 → `notes.push("build succeeded")`, then `finishPublish`. A throw
  inside the publish is caught and reported as a warning steer (with the notes
  so far), never as a swallowed rejection.
- `done` ≠ 0 → `finishMessage` → `failureResult` with the exit code and the
  bounded pane tail, plus the marker note when the code was unreadable.
- `died` → "the build pane is gone and no exit code was written" plus the tail.
- `stall` / `overrun` → one report each, nothing killed.

The strings and the delivery live in `report.ts`; `warn(pi, ctx, msg)` pairs a
toast with a steer.

## Seams

`CommandRunner` is `(cmd, opts?: RunOpts)`; `RunOpts` (pane.ts) carries `cwd`,
`timeoutMs` and `noProfile` — the VM probe is the one caller that sets
`noProfile`, because what the user profile loads must never decide whether a
probe answers.

`BuildSeams.profile?: RepoProfile` (default `rxProfile`). `ghFetch` for the
watcher's GitHub step. `stampPath` overrides the green-build stamp store
(default: this extension's own `reactive-xaf-build.db`, see below). The run
watch adds `probePane`, `sampleCpu`, `runPaths`
and `runCadence`. `delegateWindow` was removed 2026-08-25 and the flow never
consults a window.

## The surface boundary

`createFlowRunners(pi, ctx, merged, cwd)` returns the two entries a surface
calls — **menu.ts** and **tool.ts** both do: `FlowRunner`
(`runFlow(choice, mode?, projectPick?)`, the mode from `menu.ts`'s `FLOW_ROWS`;
`tool.ts` passes `"build"`) and `WatchStarter`
(`startWatch(choice, projectPick?)`). Both resolve the profile
and repo from the project pick (`seamsForPick`); a missing tree answers "not
inside the repo" through `refuse`, which toasts it as a warning and returns it —
the return value alone used to leave the user staring at nothing. A second
start answers with the RUNNING run: its id and whether it lives in a pane or
in-process, so a caller can find it instead of guessing why nothing happened.

## Skip-build variant

The `"publish"` flow mode: phases 1–5 omitted, publish + watcher run unchanged
in the command call itself (nothing to watch). The old `skipBuild = true`
boolean became a mode (`FlowMode`), so a pick can never be mistaken for a flag.

## Flow modes (`FlowMode`)

- `"publish"` — the skip-build variant above.
- `"build"` — the full flow. A green build continues into the publish on its
  own (`buildRunReporter`).
- `"buildIfNeeded"` — `buildNotNeeded` asks `buildNeeded` (below) and puts the
  verdict in the notes: FRESH answers "build not needed — <reason>" and
  publishes like `"publish"`; STALE answers "building — <reason>" and runs the
  full flow, which publishes on green. The order inside `runBuildFlow` matters:
  the running-build refusal comes BEFORE this decision, so a build-capable row
  never starts a second run and never publishes behind a live one.

## Build & Publish — the stamp (`stamp.ts`)

The row exists so a publish does not rebuild a tree a green build already
proved. The stamp — the store, its `(profile, mode)` key, its schema version and
the four freshness conditions — is owned by `stamp.md`; this module only decides
WHEN to ask. `buildIfNeeded` calls `buildNeeded(seams, repo, mode)` through
`buildNotNeeded` and puts the verdict in the notes (`build not needed — …` or
`building — …`), and a green run calls `recordGreen(seams, repo, mode)` in
`buildRunReporter`'s green arm AFTER the publish returns, so the recorded HEAD is
the one the next flow will actually see. `seams.stampPath` points the store at a
temp dir in tests.

A red build records nothing, and every store failure degrades instead of
throwing: a read answers STALE with its reason in the started notice, a write
returns its reason and the flow steers it once (`steerWatch`). The single
contract and the whole store rule live in `stamp.md`.

## Outcome strings

`summaryResult` / `failureResult` use `profile.label` (RX: "Reactive.XAF
build — <choice>"); the older note in this file said `profile.name`, which
`RepoProfile` never had. User aborts never deliver. Real failures steer.
`startedMessage` names the pane and the run id.
