---
name: reactive-xaf-build/publish
description: Use when changing the publish path — the Hyper-V VM probe/classifier (C11-C14), the build-start pre-warm, git commit, optional git push (expand), profile.queueCmd, AzDO watcher start.
drop-names: dirtyStatus, dirtySummary, areaOf, commitWith, pushDirtyPhase, Dirty working tree before pushing to <remote>
---

# publish.ts — the VM layer, commit, queue, watcher

Companion of `.pi/extensions/reactive-xaf-build/publish.ts`. The git half lives
in `gitphase.ts` (below).

`publishPhase` is called after a local build (or skip-build). Steps:

1. `ensureVmsRunning` — the VM gate (below), STARTED here and read later.
2. `commitPhase` (`gitphase.ts`) — git status; nothing to commit → skip; else
   confirm, add -A, commit (`Update DX to X` / `Build fixes (N files)` /
   skip-build `Publish (N files)`).
3. the VM outcome, read now: not ok → the publish stops here, with the commit
   already standing and its line already above the VM error in the report.
4. `queuePhase` — confirm `profile.queueLabel`; the pre-push dirty check;
   `git push ${profile.pushRemote} HEAD:master`; then `profile.queueCmd`.
5. `monitorPhase` — start the background AzDO watcher, return immediately.

The gate is deliberately OFF the commit's critical path: it is started before
the commit prompt is raised and read after it, so the agents boot while the
user reads the prompt, and a VM layer that cannot be read costs the queue,
never the commit (2026-09-20). Its failure is folded at creation, so the
commit-abort path (which returns without awaiting it) leaves no unhandled
rejection. Both milestones are ONE notify: same-type notifies back to back lose
the first, and from here on they belong to the same moment.

## What a failed push or queue says

Both go through `gitphase.ts`'s `runStep` — the one command boundary (timeout,
failure text over both streams, one log line) — and carry their own exit code:

- `git push <remote> failed (exit N): <failure text>`
- `<profile.queueCmd> failed (exit N): <failure text>`

`failureText` reads stderr first, then stdout — git explains most of its
failures on stdout, and the queue command writes its own progress there — and
reads `no output on either stream` when neither spoke. Reading one stream is
what printed a bare `failed:` with an empty reason (2026-10-25).

## The git half (`gitphase.ts`)

The dirty read, the summary a prompt carries, the message rules and both commit
prompts live in `gitphase.ts`, with the module detail in
[gitphase.md](gitphase.md). What stays here is the ORDER: the commit step runs
before the VM outcome is read, and the pre-push check runs inside `queuePhase`.

## The VM gate (`planVms` + `ensureVmsRunning`)

One probe (`Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`),
run through the seam with `noProfile: true` on the default shell. The profile stays
skipped because a profile stall is what killed the probe in the incident, and
because loading it is the `Tmux.ps1` BOM trap. There is no shell option, and the
reason is recorded rather than assumed: pwsh 7 was blamed for the stall, then
re-measured at 1.3-2.3 s against 2.5 s for `powershell.exe`, so the shell was not
the fault and the deadline is what saves the probe. Every attempt writes one log
row (`Get-VM attempt 1 of 2: timed out in 60001ms | stdout: … | stderr: …`),
because the probe used to log nothing and a stall had to be reconstructed from
process counts. A timed-out probe whose kill was not verified says so twice: the
log row reads `timed out, kill not verified`, and the refusal reads `and its process
could not be killed … the leftover pwsh may still be running`. The old flow killed
and assumed, and a refusal that claimed a kill it never got was part of that.
The read is split on `/\r?\n/`: pwsh writes CRLF and terminates the last line,
and a bare `"\n"` split leaves the `\r` on every line, where the anchored
`(.*)$` never matches (JS `$` without `/m` is end-of-input). That defect read a
healthy lab as "did not report C11, C12, C13, C14" (2026-09-19) — the same
convention `azdo.ts`'s STATUS/CANCEL parse already carries. Two
shapes are retried once, and both are defined by the ABSENCE of the `timedOut`
mark: a TRANSPORT failure (the seam threw, or the shell exited nonzero — the first
pwsh of a session is the slow one) and an exit-0 read that named NO agent at all,
which is an unreadable probe rather than an answer about the agents (Hyper-V exits
nonzero for a name it cannot find). A probe KILLED at its deadline resolves with
`code: -1` and `timedOut: true`, so the nonzero-exit rule above does not catch it:
`probeVms` hands it straight to `planVms` and that refusal ends the loop, spending
no second 60 s wait on the same silence. A readable answer
that cannot be acted on is refused on the spot too: retrying a state or a truncated
list would only report the same thing twice.

`planVms` is the single place that decides what a probe means, and it THROWS
`VmProbeError` instead of returning a refusal:

- probe killed at its deadline → `Get-VM did not answer within 60s: the probe was
  killed at its deadline, so the agent list is unreadable` (decided here, so the
  gate and the pre-warm read one wording)
- probe exit ≠ 0 → `Get-VM failed (exit N): <stderr tail>`
- an agent missing from the output → `Get-VM did not report C11, ... — no state to act on`,
  with the probe's own words appended: its stderr tail, or (when no agent was read
  at all) its stdout tail, or `no output on either stream`
- `Off` / `Saved` → Start-VM (profile-free like the probe: the `Tmux.ps1` BOM
  trap again)
- `Starting` / `Pausing` / `Resuming` / `Saving` / `Stopping` → wait for Running
- `Running` → nothing to do
- anything else (`Paused`, `Suspended`, unknown) → `<name> is <state> — neither
  running nor startable, fix it before publishing`

Throwing is the point: there is no "unreadable plan" value for a caller to
ignore, so a consumer that omits the handling stops the flow loudly instead of
acting on an empty agent list. `ensureVmsRunning` is the ONE place that turns the
throw into a refusal, and `publishPhase` reads that refusal AFTER the commit
prompt: the commit stands, the queue does not run, and the refusal steers as a
warning with the commit line above it. After a Start-VM the gate polls
(18 × `seams.pollMs`, 10s in production) until every agent answers Running, and
a poll that cannot be read refuses immediately.

## Pre-warm (`prewarmVms`)

`build.ts` calls it once the DX/pin phase passed and before the pane takes the
build. It returns `{ notes, attention }` and never waits or throws:

- a readable probe starts exactly the agents that can start;
- an unreadable probe blind-starts `Start-VM -Name C11,C12,C13,C14`, tolerating
  Hyper-V's "already in its current state" answer for the agents that are up;
- `attention` makes `build.ts` emit a `steerWatch` warning that spends no model
  turn: the build is running and the publish gate still decides at queue time.
