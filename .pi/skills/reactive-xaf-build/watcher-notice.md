---
name: reactive-xaf-build/watcher-notice
description: Use when working on what the background AzDO watcher tells the user — giving up (warning toast plus steer), the empty-poll retry line, the stall and blind-monitor warnings, and the clocks that decide between them.
---

# watcher-notice.ts — what the watcher says

Companion of `.pi/extensions/reactive-xaf-build/watcher-notice.ts`, split out
of `watcher.ts` at the 400-line cap. The chain's polling, parsing and decisions
stay in `watcher.md`; this module is the voice.

## `giveUp(ctx, state, msg, pi?)`

One warning the user sees (`ctx.ui.notify(..., "warning")`) and one the agent
reads as an ERROR (`report.ts` `steerError`: severity `error`, the message as
its blockReason), then `state.stop()`. Every give-up path goes through it —
header failure, a caller-passed deadline, an unreadable probe, a failed pipeline,
an empty chain past its grace, a refused token — so a watcher never dies
silently. The
steer is the whole point: a toast alone leaves an agent that started the watcher
waiting for a completion it will never get, and a plain warning steer put a
condition the agent cannot repair (an expired PAT) in the same key as chatter —
hence the error severity (2026-10-25).

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
by an old one. A chain that HAS a build in flight is watched by PROGRESS
(`stalledPoll`, `blindPoll`), not by elapsed time — see the three clocks above.

Why the grace exists (2026-10-25): a watcher started with nothing queued said
"waiting…" once a minute for two hours and never told the agent, because an
empty poll was treated as a retry until the deadline. Retrying forever is not
neutral — it is a silent failure with a friendly voice.

## The three clocks

| clock | default | what it means | who speaks |
|---|---|---|---|
| `emptyGraceMs` | 3 min | nothing is queued for this chain — retried inside the grace | `emptyPoll`, then `giveUp` |
| `maxMs` | **none** | a caller-supplied backstop only; the watch has NO wall-clock deadline by default | `giveUp` |
| `stallMs` | 45 min | the in-flight task stopped writing — one warning, the watch keeps going | `stalledPoll` |

There is no 120-minute default any more: a chain whose head job may legally run
90 minutes outlives any number this module invents, and the 2026-10-25 watch
died 30 minutes before the build it was watching failed. What says a build is
alive is progress — and when progress cannot be read at all, `blindPoll` says so
instead of staying quiet.

## `stalledPoll(ctx, pi, { id, activeName, silentMs })`

A build that is still `inProgress` while the task inside it has written nothing
for `stallMs` (default 45 minutes, decided in `watcher.ts`). It WARNS — a toast
plus a warning steer, no error severity and NO stop — because the condition is
not the agent's to repair and the build is not over: AzDO's own job timeout ends
it, and that verdict must still reach the agent.

Reported once per episode: the watcher keys it `<buildId>:<task>` and clears the
key when the task writes again or the chain advances, so a long silence is one
message rather than one per poll. 2026-10-25 is why it exists — `Tests Workflow`
wrote nothing for 80 minutes, every status field still said `inProgress`, and the
watch had already ended on the deadline it used to have, 30 minutes before the
build failed.

## `blindPoll(ctx, pi, { id, polls })`

The monitor cannot see: `BLIND_POLLS` (3, `watcher.ts`) in-flight polls carried
no readable `PROGRESS=` line — the timeline or the log could not be read, or the
last line carried no leading timestamp. One warning per build, never a per-poll
repeat, and never the word "hung": that would be a claim the evidence does not
support. The watch stays alive, because the pipeline's own timeout still ends the
build and that verdict has to reach the agent.

Why: dropping the default deadline without this line would trade one silent
two-hour wait for another whenever the monitor goes blind.

## `refusedPoll(ctx, state, pi, message)`

`STATUS=ERR;auth;…` — the token is expired or refused. No retry clears it and no
agent can renew a PAT, so it gives up on the FIRST poll with the remedy in the
message. The classification is `azdo.ts`'s (the script that saw the failure),
never a regex over prose here.

## `transientPoll(ctx, state, opts, pi, message)`

`STATUS=ERR;transient;…` — a 429, a blip, a proxy. Same grace clock as an empty
chain: inside it the poll retries with an informational line, past it the watch
gives up naming the read failure (the pipeline may well be fine).

## `wrongRun(ctx, state, pi, s, expected)`

The newest HEAD build is finished and its version is not `profile.versionFile`'s:
nothing was queued for this run, so there is nothing to wait for. Gives up at
once with expected vs found and the re-queue instruction — the deadline is for a
chain that HAS a build in flight, not for a chain that has none.
