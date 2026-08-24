/**
 * reactive-xaf-build/delegate — run a task in a NEW psmux window.
 *
 * defaultDelegateWindow spawns a fresh pi session in a new window with the
 * task as its first message ("pi '<task>'") — the new window boots already
 * working, no send-keys race. The invoking command returns immediately; the
 * flow (prompts, build pane, notifies, steers) continues in the new window
 * and survives the invoking session's close.
 *
 * The task text is interpolated into pwsh single quotes — it must not contain
 * single quotes (the callers use fixed task strings).
 *
 * The spawned window is only trusted after it survives a boot grace period:
 * a window that vanishes means its pwsh/pi exited (pi crashed at startup —
 * the popup-dies-with-red-chars failure mode). The window is then killed
 * (best effort) and null is returned so the flow falls back to the invoking
 * session instead of handing off to a dead window.
 */

import { runArgv, sleep, psmuxArgs } from "./pane.js";
import type { RunResult } from "./pane.js";

export type WindowDelegator = (repo: string, task: string) => Promise<string | null>;

export interface DelegateDeps {
  run?: (argv: string[]) => Promise<RunResult>;
  windowExists?: (idx: string) => Promise<boolean>;
  killWindow?: (idx: string) => Promise<void>;
  graceMs?: number;
}

const GRACE_MS = 8000;
const STEP_MS = 500;

/** Spawn a new psmux window running pi with the task; returns the window
 *  index, or null when not inside psmux / the window never came up / the
 *  window died within the boot grace period (its pi crashed). */
export async function defaultDelegateWindow(repo: string, task: string, deps: DelegateDeps = {}): Promise<string | null> {
  if (!process.env.TMUX_PANE) return null;
  const run = deps.run ?? ((argv: string[]) => runArgv(psmuxArgs(argv), 20000));
  const windowExists = deps.windowExists ?? (async (idx: string) => {
    const list = await runArgv(psmuxArgs(["psmux", "list-windows", "-F", "#{window_index}"]), 10000);
    return list.stdout.split("\n").map((s) => s.trim()).includes(idx);
  });
  const killWindow = deps.killWindow ?? (async (idx: string) => {
    await runArgv(psmuxArgs(["psmux", "kill-window", "-t", idx]), 10000);
  });
  const graceMs = deps.graceMs ?? GRACE_MS;
  const name = `rxaf-${Date.now().toString(36).slice(-6)}`;
  const spawnArgs = [
    "psmux", "new-window", "-c", repo.replace(/\\/g, "/"), "-n", name, "-P", "-F", "#{window_index}",
    "--", "pwsh", "-NoLogo", "-c", `pi '${task}'`,
  ];
  const res = await run(spawnArgs);
  const idx = res.stdout.trim().split("\n").pop() ?? "";
  if (res.code !== 0 || !idx) return null;
  const deadline = Date.now() + graceMs;
  let alive = true;
  while (Date.now() < deadline) {
    if (!(await windowExists(idx))) {
      alive = false;
      break;
    }
    await sleep(STEP_MS);
  }
  if (!alive) {
    try {
      await killWindow(idx);
    } catch {
      // window already gone
    }
    return null;
  }
  return idx;
}
