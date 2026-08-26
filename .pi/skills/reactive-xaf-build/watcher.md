---
name: reactive-xaf-build/watcher
description: Use when changing the background AzDO chain watcher — polls profile.chain, toasts on every check, retries empty polls, asserts nugets on the matching feed, then publishDraft or assertPublished on GitHub.
---

# watcher.ts — background AzDO chain watcher

Companion of `.pi/extensions/reactive-xaf-build/watcher.ts`. Started by
`monitorPhase` after `profile.queueCmd`, or by `/devexpress watch`.

## Behavior

- `startAzDoWatcher(pi, ctx, seams, opts?)` — one per session; a new start
  stops the previous. Chain is `profile.chain(choice)` (RX 23 → 72 → 89;
  expand Lab 94 / Release 39 → 38 → 37). Def 32 is `_Xpand-Lab` (2023).
- Version is read from `profile.versionFile`.
- Nugets step ASSERTS `profile.nugetId` on `profile.nugetFeed(choice)`.
  Lab uses `compareVersions` so `26.1.400.0` matches feed `26.1.400`.
  The assert RETRIES `nugetRetries` (default 10) × `nugetRetryMs`
  (default 30s) before warning — nuget.org's flatcontainer lags a push
  by minutes (observed 2026-08-26: pushed 17:59Z, indexed 18:03Z, false
  warning), so a single 404 is an index delay, not a missing publish.
- A finished build whose `buildNumber` does not match `versionFile` is
  not this run — wait (toast), then give-up **steers**. Empty polls retry,
  never fatal.
- Failed pipeline steers with `extractFailReason` (the log block, same as
  status) and stops.
- Final step: `profile.githubOnSuccess(choice)`. Missing GH_TOKEN steers.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]`.
`stopAzDoWatcher()` / `isAzDoWatcherActive()`.
