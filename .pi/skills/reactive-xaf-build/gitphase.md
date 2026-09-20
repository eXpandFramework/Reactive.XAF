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
question. Both callers fold its outcome into notes: `git add failed: <tail>`,
`git commit failed: <tail>`, or `committed: <msg>`.

`commitMessage(label, dxChanged, latest, changed)` — the message rules both
prompts share: `Update DX to <version>` when the DX/pin phase rewrote pins,
else `<label> (<N> files)`. The labels are `Build fixes` (build flow) and
`Publish` (skip-build), so a publish-only run commits as `Publish (3 files)`.

## The dirty read

`dirtyStatus` runs one `git status --short` and returns either the changed
lines or a named failure: `git status failed (exit N): <stderr tail>`, or
`git status did not run: <message>` when the seam itself threw. A read that
FAILED is never a clean tree — counting the stdout of a broken read reported
"nothing to commit" and let the flow walk over a dirty tree.

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
drops the VM outcome).
