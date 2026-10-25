---
name: reactive-xaf-build/text
description: Use when changing how the flow words what a command said — the bounded tail and the failure text (both streams, then the exit code its caller names) that report.ts, gitphase.ts and publish.ts all share.
---

# text.ts — the shared string helpers

Companion of `.pi/extensions/reactive-xaf-build/text.ts`. A LEAF: it imports
nothing, so the delivery layer (`report.ts`) and the command layers
(`gitphase.ts`, `publish.ts`) all use it without depending on each other.

## `tail(s, n = 1500)`

The last `n` characters, `...`-marked when truncated. One bounded shape for a
pane capture, a probe's answer and a command's two streams.

## `failureText(stderr, stdout, n = 1500)`

What a failed command said, in the order a reader needs it:

1. stderr, when it spoke;
2. else stdout — git explains most of its failures there (`nothing to commit,
   working tree clean` included) and the queue command writes its progress there;
3. else `no output on either stream`.

Reading ONE stream is what printed a bare `git commit failed:` with an empty
reason and cost a whole diagnosis (2026-10-25). The exit code is deliberately not
part of this string: every caller names it in its own sentence — `git commit
failed (exit 1): …` — so a process killed by a timeout (exit -1) reads as itself.

## Who uses it

- `gitphase.ts` `runStep` — the one command boundary: timeout, `failureText`,
  the exit code and one log line through the injected `log` seam.
- `publish.ts` — the push and the queue command, through that same boundary.
- `report.ts` — `tail` for the pane tails inside its messages.

## Tests

`build-tests.ts` pins the behaviour where it matters, through the flow: T52 (a
failed status read refuses instead of reading as clean), T58 (the reason git put
on stdout is reported), T59 (neither stream spoke, so the exit code is named),
T61 (stderr wins when both spoke).
