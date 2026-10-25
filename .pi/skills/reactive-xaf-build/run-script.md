---
name: reactive-xaf-build/run-script
description: Use when working on a build run's own temp artifacts — the per-run directory, the supervisor script that runs the build in a nested pwsh, the exit marker it writes, the RXAF-RUN-DONE sentinel it echoes into the pane, the tracked write seam, and the TTL prune of old run dirs.
---

# run-script.ts — a run's temp artifacts

Companion of `.pi/extensions/reactive-xaf-build/run-script.ts`, split out of
`run.ts` at the 400-line cap. The run's state and its watch stayed in `run.ts`
(`run.md`); what lives here is the FILE half. `run.ts` re-exports every name
below, so consumers keep importing the run from one module.

## Artifacts

- `newRunId()` — `${Date.now()}-${process.pid}`. Two sessions building in the
  same millisecond must not share a run dir: one would overwrite the other's
  supervisor script and marker.
- `runPaths(runId)` — `%TEMP%/rxaf-build-<runId>/` holding `run.ps1` (the
  supervisor) and `exit.code` (the marker). A fresh dir per run, so a stale
  marker can never report a later build.
- `writeRunScript(paths, cmd, env)` — writes the script, creating the run dir,
  through `trackedWrite(file, data)`: every file write rides pi-dev's tracked
  seam from `globalThis`, and a missing seam is a loud error.
- `supervisorCommand(paths)` — the short line typed into the pane:
  `pwsh -NoLogo -File "<script>"`. The logic lives in the file, so a partially
  delivered keystroke cannot lose the exit code.
- `pruneRunDirs(ttlMs = 24 h)` — best-effort TTL cleanup. Age-based on purpose:
  a "newest N" rule deletes a live build's dir, including another session's
  concurrent run whose marker this process never reads.

## The supervisor

`supervisorScript(cmd, marker, env)` emits the script the pane runs:

1. one `$env:NAME = 'value'` per `profile.buildEnv` entry (`run.ts` owns no
   build policy; the env is the caller's, and a malformed name throws at write
   time rather than producing a broken script);
2. the build in a **nested** `pwsh`, so an `exit` or a crash inside it cannot
   skip the code that follows — `$LASTEXITCODE`, falling back to `$?`;
3. `[IO.File]::WriteAllText($marker, [string]$code)` inside a `try/catch`, so
   the file holds the bare number and a denied write cannot abort the run;
4. two lines ECHOED into the pane:

```
marker=<path> code=<n> exists=<True|False> err=<first error|empty>
RXAF-RUN-DONE code=<n>
```

Both exist because of one incident (2026-10-25): a build printed
`psake succeeded`, returned to a prompt, and left **no** `exit.code` — with no
error anywhere. The watch learned completion from that file alone, so the chain
went silent: no publish, no queue, no notification. The echo makes the next
failure visible in the window it happened in, and the sentinel gives the watch a
second completion signal that does not depend on the filesystem. How the watch
consumes it (marker first, sentinel after two still polls): `run.md`.
