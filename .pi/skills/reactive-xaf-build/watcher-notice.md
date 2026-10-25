---
name: reactive-xaf-build/watcher-notice
description: Use when working on what the background AzDO watcher tells the user — giving up (warning toast plus steer), the empty-poll retry line, the stall, frozen-stream and blind-monitor warnings, and the clocks that decide between them.
---

# watcher-notice.ts — what the watcher says

Companion of `.pi/extensions/reactive-xaf-build/watcher-notice.ts`, split out
of `watcher.ts` at the 400-line cap. The chain's polling, parsing and the chain
ADVANCE stay in `watcher.md`; this module is the voice — and, because the voices
are exactly what tell the four in-flight readings apart, the progress verdict
itself (`reportProgress`) is judged here too.

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
| `stallMs` | 45 min | the in-flight build stopped writing — one warning, the watch keeps going | `stalledPoll`, `frozenPoll` |
| `queueMs` | 10 min | the build is queued and no agent has taken it — one warning, the watch keeps going | `queuedPoll` |

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

Reported once per episode: `reportProgress` keys the silence
`<buildId>:<lastOutputAt>` — ONE key for both stall voices — and clears it when
the stamp advances or the chain advances, so a long silence is one message rather
than one per poll. 2026-10-25 is why it exists — `Tests Workflow` wrote nothing
for 80 minutes, every status field still said `inProgress`, and the watch had
already ended on the deadline it used to have, 30 minutes before the build
failed.

## `frozenPoll(ctx, pi, { id, activeName, silentMs })`

The same silence read from the other clock: the live task opened no log of its
own, so the reading is the newest change across every AzDO log container
(`source=logs`, `azdo.md`). A frozen watermark is output evidence all the same,
so this is a stall — toast plus warning steer, no error severity, no stop — never
blindness.

`AzDO <id>: no log stream has changed for N min — <record> is in flight and
writing nothing.` Once per episode, keyed `<buildId>:logs:<record>` in
`watcher.ts`; a watermark that moves again clears the key, so a build that
resumes and falls silent later is reported again.

2026-10-25, build 36112: task `Build` carried `log: null`, every container had
sat unchanged since 18:57:48Z for over two hours, and the watch was saying it
could not see the build at all. The evidence was in the same poll the whole time.

## `queuedPoll(ctx, pi, { id, queuedMs })`

A build that was queued and never picked up: no agent has it, so nothing exists
to read and no stall clock can arm. Without its own clock this is the one state
the watch could never speak about — status reads fine, nothing is written,
`emptyPoll` does not apply (a build WAS queued) and `maxMs` defaults to none.
It WARNS once per wait, keyed `<buildId>:queue:<queuedAt>`, so a re-queued build
is a new wait and speaks again.

`AzDO <id> has been queued for N min and no agent has picked it up …` — the
evidence for `queueMs` (10 min) is the healthy queue itself: 36111 went from
queued to a running job in 12 seconds, 36112 in 7.

## `blindPoll(ctx, pi, { id, polls })`

The monitor cannot see: `BLIND_POLLS` (3, `watcher.ts`) in-flight polls carried
a `PROGRESS=0` line — the timeline call itself failed — or no readable
`PROGRESS=` line at all. One warning per build, never a per-poll repeat, and
never the word "hung": that would be a claim the evidence does not support. The
watch stays alive, because the pipeline's own timeout still ends the build and
that verdict has to reach the agent.

It is the LAST resort: a queued build (`PROGRESS=queue`) and a build whose live
task opened no log (`frozenPoll`) are both read rather than blind, and neither
counts toward the three polls.

Why: dropping the default deadline without this line would trade one silent
two-hour wait for another whenever the monitor goes blind.

## `reportProgress(ctx, pi, state, opts, s)`

The single entry point `watcher.ts` hands every in-flight poll to — the voices
are what tell the readings apart, so the decision lives with them. It reads the
`BuildProgress` union (`azdo.md`) and switches on the kind:

- `blind` → `noteBlind`: counted per build, spoken once at `BLIND_POLLS`.
- `queue` → `noteQueue`: quiet under `queueMs`, then `queuedPoll` once per wait.
- `running` → `noteSilence`: under `stallMs` it CLEARS the episode key (so output
  that resumed re-arms the next silence); past it, one warning per episode —
  `frozenPoll` when the stamp came from the container watermark, `stalledPoll`
  when the record's own log supplied it.

The episode key is `<buildId>:<lastOutputAt>`, ONE key shared by both stall
voices: a reading that flips between the task log and the watermark, or that
moves to another record mid-silence, is still the same silence and is announced
once. Output that resumes advances the stamp, which is what starts a new episode.

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
