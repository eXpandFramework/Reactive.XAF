---
name: reactive-xaf-build/watcher
description: The background AzDO chain watcher — polls the publish chain (23 → 72 → 89 for both Lab and Release), toasts on every check, retries empty polls, asserts the nugets on the eXpand nuget server and PUBLISHES the GitHub draft (PATCH, GH_TOKEN), steers on failure.
---

# watcher.ts — background AzDO chain watcher

Companion of `.pi/extensions/reactive-xaf-build/watcher.ts`. Started by
`monitorPhase` (build.ts) after prx (Lab) / prx -Release (Release), or by
`/devexpress watch` for a running
build. Replaces the blocking `defaultWaitForAzDoBuild` (deleted 2026-08-25)
that locked the chat for up to 2 h.

## Behavior

- `startAzDoWatcher(pi, ctx, seams, opts?)` — one per session; a new start
  stops the previous. Defaults: 60 s interval, 2 h give-up per chain step.
  `opts.choice` ("Lab" | "Release", default Lab) picks the label and the
  GitHub prerelease flag. `pollTick` delegates the deadline check and the
  empty-poll toast to the `checkDeadline` / `notifyEmptyPoll` helpers.
- Every poll runs `azdoStatusScript(definition, minId)` via `seams.run` and
  NOTIFIES (toast per check; same-type notifies self-replace → live status
  line). Running → `AzDO <id>: <status> (<elapsed> min) — link`.
- With `followNugets` the watcher walks the chain
  `23 (Reactive.XAF) → 72 (PublishNugets) → 89 (release consumers)` for
  BOTH choices (Release differs only in the label and the prerelease flag),
  advancing on each success (next build = newest with id > finished id,
  via `minId`). Advance toast: `... succeeded — watching the <next>…`.
- Nugets step ASSERTS — choice-aware feed: the version from
  `src/Common/AssemblyInfoVersion.cs` is checked on the eXpand nuget server
  (`xpandnugetserver.azurewebsites.net` v2 FindPackagesById) for LAB, and
  on **nuget.org** (flatcontainer nuspec blob) under the NORMALIZED version
  (4.261.3.0 → 4.261.3) for RELEASE — Release nugets never touch the Xpand
  server (2026-08-25 first Release run warned falsely). Found →
  `Nugets published: ...`; missing → warning + steer; the
  chain CONTINUES either way.
- Final step FINDS the GitHub release for the build version
  (`api.github.com/repos/eXpandFramework/Reactive.XAF/releases` — tag =
  version) and PUBLISHES THE DRAFT via PATCH
  `{"draft":false,"prerelease":LAB-TRUE-OR-RELEASE-FALSE}` — the chain
  creates a DRAFT, not a published release; drafts are only visible to
  authenticated callers, so this step needs **GH_TOKEN** /
  **GITHUB_TOKEN** (or `opts.ghToken`). Retries (`ghRetries` default 6 ×
  `ghRetryMs` default 30 s) absorb the release-creation race. Published
  draft → `... succeeded — GitHub release V published from draft
  (pre-release for Lab) — chain complete.`; an already-published release
  with the tag counts as success; missing after retries OR a missing token
  (loud steer naming GH_TOKEN — never a silent skip) → warning + steer.
- Failed pipeline → warning toast (FAILED label) + steer via
  `pi.sendUserMessage(msg, { deliverAs: "steer" })` (turn-independent),
  then stop. The agent plans a fix; user permission ALWAYS required.
- Empty polls (no build yet — the queue API can lag the POST by seconds)
  RETRY with a "no build found yet" toast until the deadline, never fatal
  (2026-08-25: the watcher used to give up on poll #1 and the just-queued
  build ran unwatched).
- Give-up (deadline, script failure) → warning + stop; check
  `/devexpress status`.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]` (Windows double-
load safety). `stopAzDoWatcher()` / `isAzDoWatcherActive()`; reload kills
the timer with the process.

## Seam

`BuildSeams.startAzDoWatcher` (default = real starter) and
`BuildSeams.ghFetch` (default = `defaultGhFetch`, azdo.ts — Authorization
from GH_TOKEN / GITHUB_TOKEN; never logs the token); tests inject short
intervals + retries. Contract: `watcher-tests.ts` (toast per poll, chain
advance, nuget assertion, draft publish + race retry, Release chain on
def 23, empty-poll retry, missing-token steer, terminal steer, give-up,
replace).
