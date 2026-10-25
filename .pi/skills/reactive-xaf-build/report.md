---
name: reactive-xaf-build/report
description: Use when changing the /devexpress messages — the failure and summary strings, the bounded pane tail, the start notice, and the one delivery decision behind the warn/error/watch steers.
---

# report.ts — the flow's messages and its steer half

Companion of `.pi/extensions/reactive-xaf-build/report.ts`. Extracted from
`build.ts` so the flow engine stays inside the file-size cap. The behaviour is
unchanged: the same strings, the same delivery.

## Strings

- `failureResult(id, choice, latest, notes, build)` — the FAILED message: id
  and choice, the DX line, the notes, the exit code, the tail header, then
  `tail(stdout, 4000)` and the "Fix the warnings, then re-run /devexpress." line.
- `summaryResult(id, choice, latest, notes, pubOk)` — the publish summary,
  ending in `published` or `publish stopped`.
- `watchMessage(event, id)` — the two report-only backstops (stall, overrun)
  as one line each, or null for a terminal event.
- `finishMessage(event, id, choice, latest, notes, tailText)` — the dead-pane
  message ("the build pane is gone and no exit code was written") or the
  nonzero-exit `failureResult`.
- `tail(s, n = 1500)` — imported from `text.ts`, the leaf this module and the
  command layers both use; the bounded tail, marked `...` when truncated. What a
  failed command said (`failureText`) lives there too, because it belongs to the
  commands rather than to the flow's messages: see `text.md`.
- `lazyLog()` — the sink the publish phases write through: one line per command
  (`gitphase.ts` `runStep`). It resolves the shared `Log` on FIRST USE with a
  dynamic `import("@pi/pi-dev/llm-utils.js")`, because a static `@pi/` specifier
  is linked before the suite's resolver hook exists and the extension would not
  load under the harness. A resolution that never happens is named once on
  stderr and the sink goes quiet — logging can never fail a publish. A suite
  that injects no `log` seam logs nowhere.
- `minutes(ms)` — minutes, never below 1.

## Delivery

ONE decision. `deliver(pi, msg, { severity?, triggerTurn? })` is what every
steer goes through: `globalThis.__steer` with customType
`reactive-xaf-build:build`, the severity, and the message as the blockReason
when it is an error. Without the shared sender the message still goes out
through `pi.sendUserMessage(msg, { deliverAs: "steer" })` and the severity rides
the TEXT (`[error] …`) — pi's own `sendMessage` has no channel for a severity,
so the fallback carries it in band rather than dropping it and letting an error
read as chatter.

- `warn(pi, ctx, msg)` — a warning toast plus a steer. Every failure, stall,
  overrun and dead pane goes out as this pair, never as a bare toast.
- `steerWarning(pi, msg)` — `deliver` with severity warning and
  `triggerTurn: true`.
- `steerError(pi, msg)` — `deliver` with severity ERROR and the message as its
  blockReason, so a failure the user must act on (a dead AzDO token, a queue
  nothing confirms) renders red instead of reading like one more line of
  chatter. The watcher's give-up goes out this way.
- `steerWatch(pi, msg)` — `deliver` with severity warning and NO `triggerTurn`:
  the build is already running, so the user must see it but no agent action
  follows (the pre-warm's "VM layer needs attention" lines).
- `steerStarted(pi, msg)` — the started notice with neither severity nor turn
  (a started build needs no agent action). Absent the shared sender it is
  dropped and only the toast shows it.
