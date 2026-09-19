---
name: reactive-xaf-build/pane
description: Use when working on the /devexpress build pane — pane identity, the pane seams (open, send, capture, close, probe, CPU sample), their psmux defaults, the process helpers, and why the blocking waiter is gone.
---

# pane.ts — build pane machinery

Companion of `.pi/extensions/reactive-xaf-build/pane.ts`.

The brx build runs in a NEW psmux pane split to the right of the invoking
window, and its output streams there live. This module owns the pane and the
machine the pane exposes: open it, send a command, capture the tail, close it,
and probe what its process is doing. A green exit leaves the pane open for the
user to close (`/devexpress` → "Close build pane"); a failed build keeps it,
and `build.ts` falls back to an in-process run only when the pane cannot be
opened at all.

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

- `defaultOpenBuildPane(repo)` — `psmux split-window -h [-t self] -P -F
  "#{pane_id}" -c <repo>`; `-t self` is added when `TMUX_PANE` is set.
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

- `runArgv(argv, timeoutMs, cwd?)` — spawns the argv array with
  `windowsHide`, captures stdout (bounded 100 KB, keeps the tail) and
  stderr (bounded 50 KB); on timeout it tree-kills via
  `taskkill /PID <pid> /T /F`; resolves `{ code, stdout, stderr }` on close.
- `runProcess(cmd, opts)` — `pwsh -Command <cmd>` through `runArgv`,
  default 60 s timeout.
- `psmuxArgs(args)` — prepends `-L $PSMUX_SOCKET` when the env var is set
  (socket isolation for tests and parallel servers).
- `sleep(ms)` — promise-based delay, used by the dormant delegation helper.

## Gone

`defaultWaitForPaneExit` and the `PaneWaiter` seam are deleted. It polled the
marker from inside the command handler with `fs.existsSync` every 2 s (and
`fs.rmSync` on consume), which held the flow for up to an hour while a silent
build produced nothing — and on the old timeout it closed the pane, killing a
build that was still working. The run's exit marker and watch belong to
`run.ts` now.
