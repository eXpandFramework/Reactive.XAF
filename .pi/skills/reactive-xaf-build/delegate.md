---
name: reactive-xaf-build/delegate
description: Use when the /devexpress menu pick delegation behaves wrong or is being changed — delegate.ts spawns a new psmux window running pi with a task, verifies the window survives the pi boot grace period, and falls back to the invoking session when the window dies. Read before editing delegation or its tests.
---

# delegate.ts — window delegation with liveness verification

Companion of `.pi/extensions/reactive-xaf-build/delegate.ts`. Runs a task in
a NEW psmux window so the /devexpress flow survives the invoking session's
close.

## Spawn

`defaultDelegateWindow(repo, task, deps?)`:

- Refuses to run when `process.env.TMUX_PANE` is unset (not inside psmux).
- Spawns `psmux new-window -c <repo> -n rxaf-<ts> -P -F "#{window_index} --
  pwsh -NoLogo -c "pi '<task>'"` — the new pi boots already working, no
  send-keys race.
- Reads the new window index from the spawn's stdout.
- **Task text constraint:** interpolated into pwsh single quotes — the task
  strings must contain NO single quotes (menu.ts keeps them fixed).

## Liveness verification (grace period)

The spawned window is only trusted after it survives a grace period
(default `VERIFY_MS`, injectable `graceMs`). The window vanishing means the
pwsh/pi process exited (pi crashed at startup — the popup-dies-with-red-chars
failure mode). On death the window is killed (best effort) and the function
returns `null`, so `delegateOrRun` (menu.ts) falls back to running the flow
in the invoking session with a warning notify.

The persistence check polls `psmux list-windows -F "#{window_index}"` every
`STEP_MS` until the grace expires.

## Injectable deps (tests)

`deps.run` (spawn argv), `deps.listWindows` (window-exists check),
`deps.killWindow`, `deps.graceMs` — `delegate-tests.ts` drives the REAL
function with fakes so psmux is never touched by the test suite.

## Contracts (delegate-tests.ts)

- Dead window (vanishes during grace) → `null` → the menu flow falls back
  to the invoking session and completes there ("published").
- Surviving window → the index is returned → "delegated to window N".
