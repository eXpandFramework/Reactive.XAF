/**
 * reactive-xaf-build/run-script — a run's own temp artifacts: its dir, its
 * supervisor script and the cleanup of old dirs.
 *
 * Split out of run.ts at the 400-line cap. The watch and the run's lifecycle
 * stayed there; what lives here is the FILE half: a per-run directory holding
 * the supervisor, and the supervisor itself, which runs the build in a NESTED
 * pwsh and reports the exit code BOTH ways — a marker file the watch consumes
 * and a sentinel line printed into the pane.
 *
 * `run.ts` re-exports every name below, so a consumer keeps importing the run
 * from one module.
 */

import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";

/** A run id unique across sessions: two pi windows building in the same
 *  millisecond must never share a run dir (one would overwrite the other's
 *  supervisor script and marker). */
export function newRunId(): string {
  return `${Date.now()}-${process.pid}`;
}

/** The per-run temp artifacts: the supervisor script and its exit marker. */
export interface RunPaths {
  runId: string;
  dir: string;
  script: string;
  marker: string;
}

/** One run's temp artifacts. A fresh dir per run, so a stale marker can
 *  never report a later build. */
export function runPaths(runId: string): RunPaths {
  const dir = path.join(os.tmpdir(), `rxaf-build-${runId}`);
  return { runId, dir, script: path.join(dir, "run.ps1"), marker: path.join(dir, "exit.code") };
}

/** One `$env:NAME = 'value'` assignment per entry, so the nested pwsh inherits
 *  what the caller asked for. A malformed name is a loud error: a silently
 *  broken script would fail the build far from here. */
function envLines(env: Record<string, string>): string[] {
  return Object.entries(env).map(([name, value]) => {
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) throw new Error(`invalid env name for the run: ${name}`);
    return `$env:${name} = '${value.replace(/'/g, "''")}'`;
  });
}

/** The supervisor the pane runs. The build goes into a NESTED pwsh, so an
 *  `exit` (or a crash) inside the build cannot skip the marker write. The env
 *  it starts with is the caller's (the profile's build data), never this
 *  module's: a repo-specific MSBuild property here would leak into every
 *  profile that shares the pane.
 *
 *  The marker is a FILE the watch consumes; the sentinel is a LINE the watch
 *  can read out of the pane when the file never lands. That failure was real:
 *  a build printed "psake succeeded" and returned to a prompt with no
 *  exit.code and no error anywhere, and the chain went silent because the
 *  marker was the only completion signal. The write is guarded (a denied write
 *  cannot swallow the lines), it uses [IO.File]::WriteAllText so the content is
 *  the bare number, and both the marker's state and the code are ECHOED into
 *  the pane, so the next failure is visible in the window it happened in. */
export function supervisorScript(buildCmd: string, marker: string, env: Record<string, string> = {}): string {
  const quotedMarker = marker.replace(/'/g, "''");
  const quotedCmd = buildCmd.replace(/'/g, "''");
  return [
    ...envLines(env),
    `$marker = '${quotedMarker}'`,
    "$code = 1",
    "try {",
    `  & pwsh -NoLogo -Command '${quotedCmd}'`,
    "  if ($null -ne $LASTEXITCODE) { $code = $LASTEXITCODE } elseif ($?) { $code = 0 } else { $code = 1 }",
    "} catch { $code = 1 }",
    "$markerErr = ''",
    "try {",
    "  [IO.File]::WriteAllText($marker, [string]$code)",
    "} catch { $markerErr = $_.Exception.Message }",
    "$markerThere = Test-Path -LiteralPath $marker",
    'Write-Host "marker=$marker code=$code exists=$markerThere err=$markerErr"',
    'Write-Host "RXAF-RUN-DONE code=$code"',
    "",
  ].join("\n");
}

/** Every file write rides pi-dev's tracked seam (a load-time global), the same
 *  one the flow uses for repo files; a missing seam is a loud error. */
export function trackedWrite(file: string, data: string): void {
  const seam = (globalThis as any).__writeFileSync;
  if (typeof seam !== "function") throw new Error("__writeFileSync seam missing — pi-dev not loaded");
  seam(file, data);
}

/** Write a run's supervisor script; returns the same paths for chaining. */
export function writeRunScript(paths: RunPaths, buildCmd: string, env: Record<string, string> = {}): RunPaths {
  fs.mkdirSync(paths.dir, { recursive: true });
  trackedWrite(paths.script, supervisorScript(buildCmd, paths.marker, env));
  return paths;
}

/** The short line typed into the pane. The logic lives in the script file,
 *  so a partially delivered keystroke cannot lose the exit code. */
export function supervisorCommand(paths: RunPaths): string {
  return `pwsh -NoLogo -File "${paths.script}"`;
}

function mtimeOf(dir: string): number {
  try { return fs.statSync(dir).mtimeMs; } catch { return 0; }
}

/** Drop run dirs older than the TTL (best-effort). Age-based on purpose: a
 *  "newest N" rule would delete a live build's dir, including another
 *  session's concurrent run whose marker this process never reads. */
export function pruneRunDirs(ttlMs = 86_400_000): void {
  let names: string[];
  try { names = fs.readdirSync(os.tmpdir()).filter((n) => n.startsWith("rxaf-build-")); } catch { return; }
  const cutoff = Date.now() - ttlMs;
  for (const name of names) {
    const dir = path.join(os.tmpdir(), name);
    if (mtimeOf(dir) >= cutoff) continue;
    fs.rmSync(dir, { recursive: true, force: true });
  }
}
