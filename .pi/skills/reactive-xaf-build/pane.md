---
name: reactive-xaf-build/pane
description: Use when working on the /devexpress build pane — pane identity, the pane seams (open, send, capture, close, probe, CPU sample), their psmux defaults, the process helpers, and why the blocking waiter is gone.
drop-names: psmux split-window -h [-t self] -P -F "#{pane_id}" -c <repo>
---

# pane.ts — build pane machinery

Companion of `.pi/extensions/reactive-xaf-build/pane.ts`.

The brx build runs in a NEW psmux pane split to the right of the invoking
window, and its output streams there live — the invoking pane keeps the
keyboard, because reading the build must never cost the user their cursor.
This module owns the pane and the machine the pane exposes: open it, send a
command, capture the tail, close it, and probe what its process is doing. A
green exit leaves the pane open for the
user to close (`/devexpress` → "Close build pane"); a failed build keeps it,
and `build.ts` falls back to an in-process run only when the pane cannot be
opened at all — that fallback is handed the same build env as a pane run.

Everything about the RUN itself — its id, temp dir, supervisor script, exit
marker, TTL cleanup and the background watch — lives in `run.ts`, not here.

## Pane identity

- `BUILD_PANE_KEY` — `Symbol.for("reactive-xaf-build.build-pane")` held on
  `globalThis` (duplicate-instance safe).
- `getBuildPane()` / `setBuildPane(pane)` — read and write the current build
  pane id; `setBuildPane(null)` deletes the key. `build.ts` sets it when a run
  starts, and the menu offers "Close build pane" while it is set.

## Seams (injectable)

All seams are injectable — the flow swaps them via `registerBuildCommand`
(menu.ts) and the tests pass fakes, so the real psmux CLI is never touched by
the test suite.

| Type | Default | Purpose |
|---|---|---|
| `PaneOpener` | `defaultOpenBuildPane` | open a pane for the repo, return its id or null |
| `PaneRunner` | `defaultRunInPane` | send a command to a pane |
| `PaneCapturer` | `defaultCapturePane` | capture the pane tail |
| `PaneCloser` | `defaultClosePane` | kill the pane (the abort path) |
| `PaneProber` | `defaultProbePane` | pane liveness plus its shell pid |
| `CpuSampler` | `defaultSampleCpu` | CPU seconds of the pane's process tree |

## Defaults

- `defaultOpenBuildPane(repo)` — `psmux split-window -d -h [-t self] -P -F
  "#{pane_id}" -c <repo>`; `-t self` is added when `TMUX_PANE` is set. `-d` is
  the whole point: a plain split makes the NEW pane active, so the user's next
  keystroke would land in the build's output pane instead of their prompt
  (`pane-tests.ts` pins it on a real isolated server, with the plain form as the
  control that proves the assertion can fail).
- `defaultRunInPane(pane, cmd)` — `psmux send-keys -t <pane> <cmd> Enter`.
- `defaultProbePane(pane)` — `psmux display-message -t <pane> -p
  "#{pane_dead} #{pane_pid}"`; a non-zero exit means the pane is gone, and a
  `pane_dead` pane answers but counts as dead.
- `defaultSampleCpu(pid)` — sums `.CPU` over the pane's shell and its children
  (`Get-CimInstance Win32_Process` plus `Get-Process`). A hung wait burns no
  CPU while a quiet compile keeps burning, so output silence alone never reads
  as a stall. `run.ts` consumes it for the idleness backstop.
- `defaultCapturePane(pane)` — `psmux capture-pane -t <pane> -p -S -40`.
- `defaultClosePane(pane)` — `psmux kill-pane -t <pane>`.

## Process helpers

- `runArgv(argv, timeoutMs, cwd?, env?, kill?)` — spawns the argv array with
  `windowsHide` and `env` merged over the inherited environment (so the
  parent's variables survive), captures stdout (bounded 100 KB, keeps the tail)
  and stderr (bounded 50 KB), and waits on ONE `events.once(child, "close")`
  bounded by `AbortSignal.timeout(timeoutMs)`. Inside the budget it resolves
  `{ code, stdout, stderr }` on close. At the deadline the wait is cut,
  `kill` (the `KillFn` seam) takes the child down, the death is VERIFIED by
  waiting up to `REAP_MS` (1 s) for the child's own `exit` event, then both pipes
  are destroyed, and it resolves `{ code: -1, stdout, stderr, timedOut: true, reaped }` —
  `timedOut` is a flag of its own because `-1` alone would read as an exit code
  that never happened. `reaped` is the verification: `true` means the child
  reported its own death, `false` means the kill did not land and the process may
  still be running. The leak is named, never swallowed — the catch rethrows
  anything that is not the reap's own deadline.
  The reap is the half that was missing: the old shape killed
  and assumed, and 300+ leftover `pwsh` on this box is what assuming leaves behind.
  A kill that still does not land cannot delay the answer, because the deadline
  owns it. A spawn failure still rejects, as it always did.
- `runProcess(cmd, opts)` — `pwsh -Command <cmd>` through `runArgv`,
  default 60 s timeout. `RunOpts.noProfile` swaps in
  `pwsh -NoProfile -NonInteractive -Command`: the VM probe sets it, because
  what the user profile loads must never decide whether a probe answers (a
  profile stall is what killed the probe in the incident). `RunOpts.shell` is gone:
  the Hyper-V layer was briefly moved to `powershell.exe` on the theory that pwsh 7
  could not answer `Get-VM` at all, and the re-measurement put pwsh at 1.3-2.3 s
  against 2.5 s for 5.1 — no margin worth a second environment, and the deadline is
  what saves a probe.
  `RunOpts.kill` replaces the
  tree kill (`taskkill /T /F`) — a test seam: `pane-tests.ts` P3 passes a no-op
  and still expects an answer at the deadline, which is the one thing a real kill
  cannot prove. `RunOpts.env`
  carries extra variables into the child: the build's no-pane fallback hands
  the profile's `buildEnv` here, so it starts what a pane run starts.
- `psmuxArgs(argv)` — inserts `-L $PSMUX_SOCKET` directly AFTER the binary when
  the env var is set (socket isolation for tests and parallel servers), and
  returns the argv untouched otherwise. The socket never leads: the caller
  spawns `argv[0]`, so a leading `-L` is not an executable (`spawn -L ENOENT`) —
  the shape that made the seam work only while the socket was unset.
- `sleep(ms)` — promise-based delay, used by the dormant delegation helper.

## Why the wait is one awaited close

The gate refuses an executor (`new Promise` with manual resolve/reject), a
`Promise.race` fork-join, and every `node:timers/promises` import: none carries
provenance, and that module's timers bypass the tracked-timer lifecycle. So
`runArgv` waits with ONE `events.once(child, "close", { signal })` — the shape
`vanguard/spawner.ts` already uses for its scout spawns — and the timeout signal
(`AbortSignal.timeout`, unref'd) is the backstop. A `setTimeout` for the kill is
legal; none is needed, because the abort itself is the deadline. The 5 s grace
window an earlier draft carried is gone with it: a kill that misses cannot hang
the call, and it cannot cost an extra five seconds either. P3 pins exactly that
by handing `runArgv` a no-op kill and expecting the same answer.

## Gone

`defaultWaitForPaneExit` and the `PaneWaiter` seam are deleted. It polled the
marker from inside the command handler with `fs.existsSync` every 2 s (and
`fs.rmSync` on consume), which held the flow for up to an hour while a silent
build produced nothing — and on the old timeout it closed the pane, killing a
build that was still working. The run's exit marker and watch belong to
`run.ts` now.
