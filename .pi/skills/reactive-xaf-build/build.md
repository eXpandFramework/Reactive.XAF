---
name: reactive-xaf-build/build
description: Use when changing the /devexpress flow engine — DX check, optional depPins, profile.buildCmd in a pane, publish (VM check → commit → optional git push → profile.queueCmd → AzDO background watcher), failure steer, and the skip-build variant.
---

# build.ts — the /devexpress flow engine

Companion of `.pi/extensions/reactive-xaf-build/build.ts`. Registers the
`devexpress` command via `registerBuildCommand(pi, seams?)`; all side effects
go through injectable `BuildSeams` (tests pass fakes). Repo-specific values
come from `seams.profile ?? rxProfile` (`profile.ts`).

## Phases (`runBuildFlow`)

1. **DX check** — `getLatestDx`: max stable `DevExpress.ExpressApp`.
2. **Props compare** (`dxPhase`) — DevExpress.* pins: mixed → untouched;
   single shared version ≠ latest → Update | Skip | Abort.
3. **depPins** (`depPinsPhase`) — skipped when `profile.depPins` is unset
   (RX). Expand: latest `Xpand.Extensions` from the matching feed, rewrite
   `Xpand.Extensions*` / `Xpand.XAF.*` ask-first.
4. **Build** (`buildPhase`) — `profile.buildCmd` in a right-side pane;
   in-process fallback. Red → `failureResult`, steer, pane kept.
5. **Publish** (`publishPhase`) — VMs C11–C14 → commit → optional
   `git push ${profile.pushRemote} HEAD:master` → `profile.queueCmd` →
   `monitorPhase` (watcher starts, turn returns immediately).

## Seams

`BuildSeams.profile?: RepoProfile` (default `rxProfile`). `ghFetch` for
the watcher's GitHub step. `delegateWindow` was removed 2026-08-25.

## Skip-build variant

`skipBuild = true`: phases 1–4 omitted. Publish + watcher run unchanged.

## Outcome strings

`summaryResult` / `failureResult` use `profile.name` (RX: "Reactive.XAF
build — <choice>"). User aborts never deliver. Real failures steer
(`sendUserMessage`, deliverAs "steer").
