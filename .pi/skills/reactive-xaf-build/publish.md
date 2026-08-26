---
name: reactive-xaf-build/publish
description: Use when changing the publish path — Hyper-V VMs C11-C14, git commit, optional git push (expand), profile.queueCmd, AzDO watcher start.
---

# publish.ts — VMs, commit, queue, watcher

Companion of `.pi/extensions/reactive-xaf-build/publish.ts`.

`publishPhase` is called after a local build (or skip-build). Steps:

1. `ensureVmsRunning` — C11–C14 Off → Start-VM + poll; Starting → wait.
2. `commitPhase` — git status; nothing to commit → skip; else confirm,
   add -A, commit (`Update DX to X` / `Build fixes (N files)` /
   skip-build `Publish (N files)`).
3. `queuePhase` — confirm `profile.queueLabel`; optional
   `git push ${profile.pushRemote} HEAD:master`; then `profile.queueCmd`.
4. `monitorPhase` — start the background AzDO watcher, return immediately.
