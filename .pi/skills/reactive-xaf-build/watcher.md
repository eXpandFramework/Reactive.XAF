---
name: reactive-xaf-build/watcher
description: The background AzDO chain watcher — polls the publish chain (def 23 → 72 → 89), toasts on every check, asserts the nugets on the eXpand nuget server, steers on failure.
---

# watcher.ts — background AzDO chain watcher

Companion of `.pi/extensions/reactive-xaf-build/watcher.ts`. Started by
`monitorPhase` (build.ts) after prx queues a build, or by `/devexpress watch`
for a running build. Replaces the blocking `defaultWaitForAzDoBuild`
(deleted 2026-08-25) that locked the chat for up to 2 h.

## Behavior

- `startAzDoWatcher(pi, ctx, seams, opts?)` — one per session; a new start
  stops the previous. Defaults: 60 s interval, 2 h give-up per chain step.
- Every poll runs `azdoStatusScript(definition, minId)` via `seams.run` and
  NOTIFIES (toast per check; same-type notifies self-replace → live status
  line). Running → `AzDO <id>: <status> (<elapsed> min) — link`.
- With `followNugets` the watcher walks the chain
  `23 (Reactive.XAF) → 72 (PublishNugets) → 89 (release consumers)`,
  advancing on each success (next build = newest with id > finished id,
  via `minId`). Advance toast: `... succeeded — watching the <next>…`;
  final: `... succeeded — chain complete.`
- Nugets step ASSERTS: version from `src/Common/AssemblyInfoVersion.cs`
  checked on the eXpand nuget server (`xpandnugetserver.azurewebsites.net`
  v2 FindPackagesById — NOT nuget.org; the lab chain publishes to the Xpand
  server). Found → `Nugets published: ...`; missing → warning + steer; the
  chain CONTINUES either way.
- Failed pipeline → warning toast (FAILED label) + steer via
  `pi.sendUserMessage(msg, { deliverAs: "steer" })` (turn-independent),
  then stop. The agent plans a fix; user permission ALWAYS required.
- Give-up (deadline, script failure, no build) → warning + stop; check
  `/devexpress status`.

## State

`globalThis[Symbol.for("reactive-xaf-build.azdo-watcher")]` (Windows double-
load safety). `stopAzDoWatcher()` / `isAzDoWatcherActive()`; reload kills
the timer with the process.

## Seam

`BuildSeams.startAzDoWatcher` (default = real starter); tests inject short
intervals. Contract: `watcher-tests.ts` (toast per poll, chain advance,
nuget assertion, terminal steer, give-up, replace).
