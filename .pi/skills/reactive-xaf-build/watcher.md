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
   A head build of a different version is not this run: the watcher gives up AT
   ONCE on the first one, naming expected vs found and the re-queue. Waiting on
   a deadline for a build nothing queued is exactly the silent two-hour give-up
   of 2026-10-25 (W16). Empty polls have their own grace budget — next bullet.
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
  never judged by an old one. A chain WITH a build in flight has no wall-clock
  deadline any more — see the last two bullets. Before this the watcher said
  "waiting…" once a minute for two hours and never told the agent (2026-10-25).
- **A build that stops writing is reported, not waited out.** While the build is
  in flight the status script also prints `PROGRESS=` — the task the build is
  inside and the timestamp of that task's LAST log line (`azdo.md`). When that
  stamp is older than `stallMs` (default 45 minutes) the watcher WARNS once per
  episode, naming the task and the silence, and KEEPS WATCHING
  (`watcher-notice.md` `stalledPoll`). It never gives up there: AzDO's own job
  timeout ends a hung build, and that terminal verdict is what the agent needs.
  2026-10-25: `Tests Workflow` wrote nothing from 14:43 and was canceled at its
  90-minute timeout at 16:04, so that silence was the only evidence of the hang.
  Progress that cannot be read is not a stall — no `PROGRESS=` line leaves the
  watch quiet.
- **No wall-clock deadline by default.** `maxMs` is a backstop a caller asks
  for; without it the watch runs until the chain ends. The old 120-minute
  default ended the watch at 15:34 on 2026-10-25, 30 minutes before the build
  actually failed — which is why nothing was listening when it did.
- Final step: `profile.githubOnSuccess(choice)`. Missing GH_TOKEN steers. The
  attempt itself — the token, the draft PATCH and the retry loop — lives in
  `github-release.ts` (`github-release.md`); this module only decides which
  repo, which policy and which version.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]`.
`stopAzDoWatcher()` / `isAzDoWatcherActive()`.

## What it says

`giveUp` (warning toast plus an ERROR steer, then stop), the retry lines and the
grace between them live in `watcher-notice.ts` — see `watcher-notice.md`. Every
give-up path in this module calls that one function.

`applyPoll` switches on `parsePoll`'s typed answer, never on prose:

- `build` → the in-flight line, the chain advance, or the terminal message. The
  in-flight line is also where PROGRESS is decided: a task silent past `stallMs`
  is `stalledPoll`, and three polls in a row with no readable progress are
  `blindPoll`. Neither stops the watch (`watcher-notice.md`).
- `empty` → `emptyPoll`, retried inside the grace.
- `error` / `auth` → `refusedPoll`: give up at once, the remedy in the message.
- `error` / `transient` → `transientPoll`: retry inside the same grace.
- `null` (no `STATUS=` line at all — a crashed script) → give up with the
  stderr; nothing can classify it.
