---
name: reactive-xaf-build/watcher-notice
description: Use when working on what the background AzDO watcher tells the user — giving up (warning toast plus steer), the empty-poll retry line, and the grace budget that decides between them.
---

# watcher-notice.ts — what the watcher says

Companion of `.pi/extensions/reactive-xaf-build/watcher-notice.ts`, split out
of `watcher.ts` at the 400-line cap. The chain's polling, parsing and decisions
stay in `watcher.md`; this module is the voice.

## `giveUp(ctx, state, msg, pi?)`

One warning the user sees (`ctx.ui.notify(..., "warning")`) and one the agent
reads (`pi.sendUserMessage(msg, { deliverAs: "steer" })`), then `state.stop()`.
Every give-up path goes through it — header failure, deadline, an unreadable
probe, a failed pipeline, an empty chain past its grace — so a watcher never
dies silently. The steer is the whole point: a toast alone leaves an agent that
started the watcher waiting for a completion it will never get.

## `emptyPoll(ctx, state, opts, pi)`

An empty poll is `STATUS=0`: nothing is queued for this chain.

- **Inside the grace** — `opts.emptyGraceMs` (default `DEFAULT_EMPTY_GRACE_MS`,
  3 minutes), counted from the first empty poll of this stretch — it retries
  with the informational line `AzDO: no build found yet (N min) — waiting…`.
  A queue that is a moment late still lands; the retry never steers.
- **Past the grace** it calls `giveUp` with the true thing: no AzDO build was
  queued for this chain, so there is nothing to watch — queue it with Publish
  or Build & Publish, then start the watcher again.

The clock lives on the watcher's state as `emptySince` and is cleared by
`watcher.ts` the moment a poll returns a build, so a later queue is never judged
by an old one. The 120-minute `maxMs` deadline is a different budget and is
untouched: it bounds a chain that HAS a build in flight.

Why the grace exists (2026-10-25): a watcher started with nothing queued said
"waiting…" once a minute for two hours and never told the agent, because an
empty poll was treated as a retry until the deadline. Retrying forever is not
neutral — it is a silent failure with a friendly voice.
