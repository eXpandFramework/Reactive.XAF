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
  expand Lab 32 / Release 39 → 38 → 37).
- Version is read from `profile.versionFile` (RX:
  `src/Common/AssemblyInfoVersion.cs`; expand: `XpandAssemblyInfo.cs` —
  the local build writes it, we never edit it).
- Nugets step ASSERTS `profile.nugetId` on `profile.nugetFeed(choice)`
  (lab server vs nuget.org, normalized version on nuget.org).
- Final step: `profile.githubOnSuccess(choice)` — `publishDraft` PATCHes
  the draft (`prerelease` true for Lab); `assertPublished` does not PATCH
  (expand Lab GitHub is already live). Missing GH_TOKEN steers loudly.
- Empty polls retry, never fatal. Failed pipeline steers and stops.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]`.
`stopAzDoWatcher()` / `isAzDoWatcherActive()`.
