---
name: reactive-xaf-build/gitphase
description: Use when changing what the /devexpress flow does with the work tree — the dirty read and its refusal, the prompt summary (counts and areas, never paths), the commit message rules, the build's commit prompt, and the pre-push dirty check on the profiles that push.
---

# gitphase.ts — the flow's git decisions

Companion of `.pi/extensions/reactive-xaf-build/gitphase.ts`. Split out of
`publish.ts` (the 400-line cap): `publish.ts` owns the VM layer and the phase
order, this module owns everything that reads or writes the work tree.

## One core, two prompts

`commitWith(seams, repoRoot, msg)` is the only code that stages and commits:
`git add -A`, then `git commit -m "<msg>"` with `"` folded to `'`. It never
prompts, so a "commit" answer in either prompt can never turn into a second
question. Both callers fold its outcome into notes: `git add failed (exit N):
<failure text>`, `git commit failed (exit N): <failure text>`, `committed:
<msg>`, or `nothing left to commit: the tree is already committed (git said: …)`.

Every command goes through one boundary — `runGitStep` or `runStep`, two doors
over one `step` (below). It owns the timeout, the failure text (`text.ts`'s
`failureText(stderr, stdout)`: stderr when it spoke, else stdout — git explains
most of its failures there — else `no output on either stream`), and ONE log line
through the injected `log` seam, shaped `<label>: exit <n>` plus the step's own
note and, on a failure, git's words. Reading stderr alone printed a bare `git
commit failed:` with an empty reason (2026-10-25); the exit code is named in the
caller's own sentence, so a timeout-killed process (exit -1) reads as itself.

A non-zero commit over a tree that is clean NOW is not a failure: the commit is
asked to leave nothing uncommitted, and that holds. In a checkout two windows
share, the other window can commit the same files between the prompt and the
commit. The note reports what the two reads prove — git's own words and its exit
code — and claims nothing about WHICH commit landed. The commit's own budget is
180s (not the 60s of the other calls): a repo-local `post-commit` hook may
rebuild a code graph after the ref moved, and a kill there reported a landed
commit as a failure. `log` is a seam on `BuildSeams`, wired once in
`build.ts` `defaultSeams` through a FIRST-USE dynamic import of the shared
`Log` — a static `@pi/` import cannot link under the suite's resolver hook.

`commitMessage(label, dxChanged, latest, changed)` — the message rules both
prompts share: `Update DX to <version>` when the DX/pin phase rewrote pins,
else `<label> (<N> files)`. The labels are `Build fixes` (build flow) and
`Publish` (skip-build), so a publish-only run commits as `Publish (3 files)`.

## The dirty read

`dirtyStatus` runs one `git status --short` and returns either the changed
lines or a named failure: `git status failed (exit N): <failure text>`, or
`git status did not run: <message>` when the seam itself threw. A read that
FAILED is never a clean tree — counting the stdout of a broken read reported
"nothing to commit" and let the flow walk over a dirty tree.

### What counts as a changed file

A porcelain `--short` entry is EXACTLY `XY <path>`: two status columns, a space,
then the path (`STATUS_ENTRY_RE`). Anything else on stdout is NOT a file. That is
not pedantry: the command runner is `pwsh -Command`, which loads the user
profile, and a profile that prints one word puts it in front of every command's
output. This machine's profile printed `high`, `dirtyStatus` counted it, and
every publish believed the tree was dirty — it staged nothing, `git commit`
exited 1 with `nothing to commit, working tree clean`, and the publish stopped
over work that did not exist (2026-10-25).

The lines the read does not understand are NOT dropped quietly:
`dirtyStatus` returns them in `DirtyRead.dropped`, the step's one log line names
their count, and `commitPhase`/`pushDirtyPhase` put `git status printed <N>
line(s) that are not status entries: <lines> — not counted as changes` in the
report the user reads. A read that discarded part of git's output must never be
indistinguishable from a fully read one.

### Two doors, because the shell policy is not one decision

- `runGitStep` NEVER loads the pwsh profile. `git` is an application on PATH, so
  the profile has nothing to contribute and its chatter cannot reach a parse.
  The status read, the add and the commit take this door, and so must every git
  call added later.
- `runStep` loads it, because `prx`/`brx` ARE profile-defined aliases and the
  push may need the environment the profile sets up. The queue command and the
  push take this door; their output is never parsed for entries.

`dirtySummary(lines)` is what a prompt carries instead of paths:

```
2 files: 1 modified, 1 new
areas: src (1), Xpand/Xpand.Utils (1)
```

The first line counts by state in a fixed order (modified, new, deleted,
renamed, untracked, conflicted, then any unknown pair by its own letters), so
the same tree always reads alike. The second names at most three areas by
count, then `+N more areas`. `areaOf` makes an area a DIRECTORY and never a
file: one segment when the rest of the path is the file, two when there is a
deeper directory to name, `(root)` for a top-level file. A rename reads as its
new path.

## commitPhase — the build's commit step

Read the tree, then: clean → `nothing to commit` and the flow continues (the
queue still runs); a failed read → refuse here; dirty → the prompt
`Commit with message: "<msg>"?` followed by the summary, answered with
`Commit` or `Abort`. Abort notes `commit aborted` and stops the publish before
the queue.

## pushDirtyPhase — the pre-push check

Only the profiles with a push remote reach it (eXpand Lab/Release; RX returns
`null` from `pushRemote`). The commit step normally leaves the tree clean, so
this fires when something wrote in the window between the two: the IDE,
another agent, a background build. Dirty → `Dirty working tree before pushing
to <remote>` plus the summary, with three answers:

- `Commit before push` — `commitWith` under the same message rules, then the
  push. A failed commit stops the push and the queue.
- `Push as is` — the tree stays dirty and the push runs.
- `Abort` — no push and NO queue (`push aborted, the tree is left as it is`):
  a pipeline queued on a commit the user just declined is not a publish.

`publishPhase` reads the abort as `{ ok: false, failed: false }`, so a user
abort reports `publish stopped` without steering a warning.

## Tests

`build-tests.ts` pins all of it through the command: T3/T4/T4b (commit
messages, and `Update DX` winning over the label), T10 (clean tree, no commit
prompt), T31 (a commit that runs while the VM layer is unreadable), T48 (the
summary inside the prompt), T49/T50/T51 (the three pre-push answers), T52 (a
failed status read refuses instead of reading as clean), T53 (a commit abort
drops the VM outcome), T58/T59/T61 (a failed commit reports git's stdout, the
exit code when both streams are empty, and stderr when both spoke), T60 (a
commit another process already landed continues the publish), T62 (every step
leaves one line on the `log` seam), T63 (a profile banner alone is not a dirty
file, the status read is profile-free while the queue keeps the profile, and the
publish still reaches the queue), T64 (a banner beside one real change still
counts one file, the add and the commit are profile-free, and the ignored line
reaches the report).
