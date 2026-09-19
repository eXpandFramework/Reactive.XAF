---
name: reactive-xaf-build/publish
description: Use when changing the publish path — the Hyper-V VM probe/classifier (C11-C14), the build-start pre-warm, git commit, optional git push (expand), profile.queueCmd, AzDO watcher start.
---

# publish.ts — VMs, commit, queue, watcher

Companion of `.pi/extensions/reactive-xaf-build/publish.ts`.

`publishPhase` is called after a local build (or skip-build). Steps:

1. `ensureVmsRunning` — the VM gate (below).
2. `commitPhase` — git status; nothing to commit → skip; else confirm,
   add -A, commit (`Update DX to X` / `Build fixes (N files)` /
   skip-build `Publish (N files)`).
3. `queuePhase` — confirm `profile.queueLabel`; optional
   `git push ${profile.pushRemote} HEAD:master`; then `profile.queueCmd`.
4. `monitorPhase` — start the background AzDO watcher, return immediately.

## The VM gate (`planVms` + `ensureVmsRunning`)

One probe (`Get-VM -Name C11,C12,C13,C14 | ForEach-Object { "$($_.Name)=$($_.State)" }`),
run through the seam with `noProfile: true` (pwsh without the user profile and
without a prompt: a profile stall is what killed the probe in the incident).
The read is split on `/\r?\n/`: pwsh writes CRLF and terminates the last line,
and a bare `"\n"` split leaves the `\r` on every line, where the anchored
`(.*)$` never matches (JS `$` without `/m` is end-of-input). That defect read a
healthy lab as "did not report C11, C12, C13, C14" (2026-09-19) — the same
convention `azdo.ts`'s STATUS/CANCEL parse already carries. Two
shapes are retried once: a TRANSPORT failure (the seam threw, or pwsh exited
nonzero — the first pwsh of a session is the slow one) and an exit-0 read that
named NO agent at all, which is an unreadable probe rather than an answer about
the agents (Hyper-V exits nonzero for a name it cannot find). A readable answer
that cannot be acted on is refused on the spot: retrying a state or a truncated
list would only report the same thing twice.

`planVms` is the single place that decides what a probe means, and it THROWS
`VmProbeError` instead of returning a refusal:

- probe exit ≠ 0 → `Get-VM failed (exit N): <stderr tail>`
- an agent missing from the output → `Get-VM did not report C11, ... — no state to act on`,
  with the probe's own words appended: its stderr tail, or (when no agent was read
  at all) its stdout tail, or `no output on either stream`
- `Off` / `Saved` → Start-VM
- `Starting` / `Pausing` / `Resuming` / `Saving` / `Stopping` → wait for Running
- `Running` → nothing to do
- anything else (`Paused`, `Suspended`, unknown) → `<name> is <state> — neither
  running nor startable, fix it before publishing`

Throwing is the point: there is no "unreadable plan" value for a caller to
ignore, so a consumer that omits the handling stops the flow loudly instead of
acting on an empty agent list. `ensureVmsRunning` is the ONE place that turns the
throw into a refusal, and it refuses BEFORE the commit and the queue, steering as
a warning. After a Start-VM the gate polls (18 × `seams.pollMs`, 10s in
production) until every agent answers Running, and a poll that cannot be read
refuses immediately.

## Pre-warm (`prewarmVms`)

`build.ts` calls it once the DX/pin phase passed and before the pane takes the
build. It returns `{ notes, attention }` and never waits or throws:

- a readable probe starts exactly the agents that can start;
- an unreadable probe blind-starts `Start-VM -Name C11,C12,C13,C14`, tolerating
  Hyper-V's "already in its current state" answer for the agents that are up;
- `attention` makes `build.ts` emit a `steerWatch` warning that spends no model
  turn: the build is running and the publish gate still decides at queue time.
