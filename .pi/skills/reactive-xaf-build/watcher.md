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
- AzDO build numbers are NOT plain versions: the chain HEAD reports
  `<version>-<dxVersion>` (`4.261.3.1-26.1.3`), the downstream pipelines
  date numbers (`20260919.1`). The this-run guard therefore compares only
  the HEAD build number's leading version token, and applies to the head
  step alone — steps 1-2 are fenced by the id baseline (`id > minId`).
   A head build of a different version is not this run — wait (toast), then
   give-up **steers**. Empty polls have their own grace budget — next bullet.
  (2026-09-19: comparing the raw number hit `Number("1-26")` = NaN, so a
  fully GREEN chain was rejected as "not this run", and the watcher gave up
  29 min after def 89 had succeeded.)
- Failed pipeline steers with `extractFailReason` (the log block, same as
  status) and stops.
- **Empty polls** (`STATUS=0`: nothing is queued for this chain) have their OWN
  grace budget, `emptyGraceMs` (default 3 minutes). Inside it the poll retries
  with the same informational "no build found yet (N min) — waiting…" toast, so
  a queue that is a moment late still lands. Past it `giveUp` runs: a warning
  toast PLUS a steer saying the true thing — no AzDO build was queued for this
  chain, so there is nothing to watch; queue it with Publish or Build &
  Publish. The clock is cleared the moment a build appears, so a later queue is
  never judged by an old one. The 120-minute `maxMs` deadline is untouched and
  belongs to a chain that has a build in flight. Before this the watcher said
  "waiting…" once a minute for two hours and never told the agent (2026-10-25).
- Final step: `profile.githubOnSuccess(choice)`. Missing GH_TOKEN steers.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]`.
`stopAzDoWatcher()` / `isAzDoWatcherActive()`.

## What it says

`giveUp` (warning toast plus steer, then stop), the empty-poll retry line and
the grace between them live in `watcher-notice.ts` — see `watcher-notice.md`.
Every give-up path in this module calls that one function.
