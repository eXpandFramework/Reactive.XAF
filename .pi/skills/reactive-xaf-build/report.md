---
name: reactive-xaf-build/report
description: Use when changing the /devexpress messages — the failure and summary strings, the bounded pane tail, the start notice, and the warn/steer pair that carries a report to the agent.
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
- `tail(s, n = 1500)` — the bounded tail, marked `...` when truncated.
- `minutes(ms)` — minutes, never below 1.

## Delivery

- `warn(pi, ctx, msg)` — a warning toast plus a steer. Every failure, stall,
  overrun and dead pane goes out as this pair, never as a bare toast.
- `steerWarning(pi, msg)` — `globalThis.__steer` with customType
  `reactive-xaf-build:build`, severity warning and `triggerTurn: true`; without
  the shared sender it falls back to
  `pi.sendUserMessage(msg, { deliverAs: "steer" })`, which always starts a turn.
- `steerStarted(pi, msg)` — the started notice on the same sender with no
  severity and no turn (a started build needs no agent action). Absent the
  shared sender it is dropped and only the toast shows it.
- `steerWatch(pi, msg)` — a warning on the same sender with
  `severity: "warning"` and NO `triggerTurn`: the build is already running, so
  the user must see it but no agent action follows (the pre-warm's "VM layer
  needs attention" lines). Falls back to `pi.sendUserMessage` like
  `steerWarning`.
