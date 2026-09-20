---
name: reactive-xaf-build/run
description: Use when working on the build run — the per-run temp dir and supervisor script, the exit marker, the pane-death and stall/overrun backstops, the cadence seam, abort, and why the watch never kills a build.
---

# run.ts — the build run: lifecycle and watch

Companion of `.pi/extensions/reactive-xaf-build/run.ts`.

`/devexpress` hands the build to a pane and returns; this module owns the run
from there. It is both the run's lifecycle and its watch. Nothing awaits a
build inside the command handler, so the agent stays reachable and the result
arrives as a message instead of a hang.

## Lifecycle

- `newRunId()` — `${Date.now()}-${process.pid}`. Two sessions building in the
  same millisecond must not share a run dir: one would overwrite the other's
  supervisor script and marker.
- `runPaths(runId)` — `%TEMP%/rxaf-build-<runId>/` holding `run.ps1` (the
  supervisor) and `exit.code` (the marker). A fresh dir per run, so a stale
  marker can never report a later build.
- `supervisorScript(cmd, marker)` — runs the build in a NESTED `pwsh`, so an
  `exit` (or a crash) inside the build cannot skip the marker write, then
  writes `$LASTEXITCODE` (falling back to `$?`) into the marker. It also
  exports `MSB3026` as a message for the run, see below.
- `writeRunScript(paths, cmd)` — writes the script, creating the run dir,
  through `trackedWrite(file, data)`: every file write rides pi-dev's tracked
  seam from `globalThis`, and a missing seam is a loud error.
- `supervisorCommand(paths)` — the short line typed into the pane:
  `pwsh -NoLogo -File "<script>"`. The logic lives in the file, so a partially
  delivered keystroke cannot lose the exit code.
- `pruneRunDirs(ttlMs = 24 h)` — best-effort TTL cleanup. Age-based on purpose:
  a "newest N" rule deletes a live build's dir, including another session's
  concurrent run whose marker this process never reads.

## The build env, owned by the profile

`supervisorScript(cmd, marker, env)` / `writeRunScript(paths, cmd, env)` write
one `$env:NAME = 'value'` assignment per entry ahead of the build line, so the
nested pwsh inherits it. The env is the CALLER's: `run.ts` owns no build policy,
because the same template serves every profile that shares the pane. A
malformed name throws at write time instead of producing a broken script. Both
branches of a run get it: a pane run through the script, the no-pane fallback
through `RunOpts.env` on the command runner (`pane.ts`), so the two are
interchangeable.

RX declares its own in `profile.ts` (`buildEnv`): `MSBuildWarningsAsMessages =
'MSB3026'`. The shared `bin` folder is written by many projects at once, so the
build's own parallel workers collide on the same DLL now and then; MSBuild's
copy task retries, logs `MSB3026` (a notice), and the copy succeeds. Under
`-WarnAsError` that notice alone failed a Release run on 2026-09-20, and each of
its three attempts reproduced it. A copy that genuinely fails is untouched:
exhausted retries log `Copy.Error`, an error, and still stop the build.

Scope, on purpose: local runs only. The Azure pipeline reaches the same psake
`Compile` task (`Build/BuildPipeline.yml` → `Build/BuildPipeline.ps1`, with
`-WarnAsError`) and keeps failing on that notice by decision, not by oversight:
moving the setting into the shared build entry would change the pipeline too. If
the notice ever fails a pipeline run, that entry is where it belongs. Where the
setting does apply it is an MSBuild property from the environment, so a
project-level definition that does not compose it simply drops the downgrade and
that project fails as before.

## Signals, in the order a tick reads them

1. **Exit marker** — the supervisor's code, consume-on-read. The primary signal.
2. **Pane death** — the probe answers dead or gone and no marker arrived: the
   build host vanished, reported as a failure with the captured tail.
3. **Stall** — no new pane output AND no CPU progress for `stallMs` while the
   process is alive: reported once.
4. **Overrun** — running longer than `overrunMs`: reported once.

A stall or an overrun never kills the build. The only closer is the user's
abort (`/devexpress` → "Abort build"), which closes the pane.

## Cadence (`RUN_CADENCE`)

| Key | Default | Job |
|---|---|---|
| `signalMs` | 2000 | the tick: marker check plus the cadence gate |
| `probeMs` | 10 s | pane liveness plus output capture |
| `cpuMs` | 30 s | CPU sample of the pane's process tree |
| `stallMs` | 10 min | silence AND CPU idleness |
| `overrunMs` | 20 min | runtime report |

10 and 20 minutes are the local-build numbers: a local Lab build skips the test
suite the AzDO pipeline runs, so it is far shorter than the ~39-minute pipeline
figure. Tests inject `runCadence` through `BuildSeams` (10 ms ticks and tuned
windows), so the thresholds stay a seam, not a constant.

## State and API

- `startBuildRun(handle, seams, report)` — one run at a time; a second start
  returns false and the flow refuses with "already running".
- `activeBuildRun()` — the run in flight, feeding the menu's abort label.
- `isBuildRunActive()`, `stopBuildRun()`, `abortBuildRun(close)`.
- `watchInProcessRun(promise, report)` — the no-pane fallback: the promise is
  the signal, and the report still never blocks the command.
- `readMarker(marker)` — consume-on-read: `undefined` when the file is absent
  or still empty (a mid-write is never read as success), a number, or null
  when the content is not a number (terminal, reported as a failure).

The state lives on `globalThis` (`Symbol.for("reactive-xaf-build.run")`), like
the AzDO watcher. A reload therefore drops the run: it stops being reported
and `/devexpress` no longer offers the abort entry for it.
