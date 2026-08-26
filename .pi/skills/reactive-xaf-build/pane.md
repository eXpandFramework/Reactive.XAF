---
name: reactive-xaf-build/pane
description: Use when working on the /devexpress build pane — how the build's psmux pane is opened, driven, waited on via the transient exit-code marker, captured, and closed, plus the injectable seams and process helpers.
---

# pane.ts — build pane machinery

Companion of `.pi/extensions/reactive-xaf-build/pane.ts`.

The brx build runs in a NEW psmux pane split to the right of the invoking
window; the output streams there live. Completion is signaled by a
transient exit-code marker (consume-on-read, in `%TEMP%`). A green exit
leaves the pane open for the user to close (`/devexpress` → "Close build
pane"); a failed build keeps the pane for reuse. When the pane cannot be
opened, `build.ts` falls back to an in-process build.

## Pane identity

- `BUILD_PANE_KEY` — `Symbol.for("reactive-xaf-build.build-pane")` held on
  `globalThis` (duplicate-instance safe).
- `getBuildPane()` / `setBuildPane(pane)` — read/write the current build
  pane id; `setBuildPane(null)` deletes the key.

## Seams (injectable)

All seams are injectable — `build.ts` swaps them via `registerBuildCommand`
and the tests pass fakes, so the real psmux CLI is never touched by the
test suite.

| Type | Default | Purpose |
|---|---|---|
| `PaneOpener` | `defaultOpenBuildPane` | open a pane for the repo, return its id or null |
| `PaneRunner` | `defaultRunInPane` | send a command to a pane |
| `PaneWaiter` | `defaultWaitForPaneExit` | wait for the exit marker |
| `PaneCapturer` | `defaultCapturePane` | capture the pane tail |
| `PaneCloser` | `defaultClosePane` | kill the pane |

## Defaults

- `defaultOpenBuildPane(repo)` — `psmux split-window -h [-t self] -P -F
  "#{pane_id}" -c <repo>`; `-t self` is added when `TMUX_PANE` is set (the
  current pane). Returns the last output line as the pane id when the exit
  code is 0, else null.
- `defaultRunInPane(pane, cmd)` — `psmux send-keys -t <pane> <cmd> Enter`.
- `defaultWaitForPaneExit(pane, marker, timeoutMs)` — polls `fs.existsSync`
  every 2 seconds until the deadline; on the marker it reads the exit code,
  removes the marker (`rmSync`, consume-on-read) and returns
  `{ code, timedOut: false }`. Timeout returns `{ code: null, timedOut: true }`.
- `defaultCapturePane(pane)` — `psmux capture-pane -t <pane> -p -S -40`
  (last 40 lines).
- `defaultClosePane(pane)` — `psmux kill-pane -t <pane>`.
- `exitMarkerPath()` — `%TEMP%/rxaf-build-<timestamp>.exit`.

## Process helpers

- `runArgv(argv, timeoutMs, cwd?)` — spawns the argv array with
  `windowsHide`, captures stdout (bounded 100 KB, keeps the tail) and
  stderr (bounded 50 KB); on timeout it tree-kills via
  `taskkill /PID <pid> /T /F`; resolves `{ code, stdout, stderr }` on close.
- `runProcess(cmd, opts)` — `pwsh -Command <cmd>` through `runArgv`,
  default 60 s timeout.
- `psmuxArgs(args)` — prepends `-L $PSMUX_SOCKET` when the env var is set
  (socket isolation for tests and parallel servers).
- `sleep(ms)` — promise-based delay.
