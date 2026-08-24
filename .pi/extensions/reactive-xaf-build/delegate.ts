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
 */

import { runArgv, sleep, psmuxArgs } from "./pane.js";

export type WindowDelegator = (repo: string, task: string) => Promise<string | null>;

const VERIFY_MS = 15000;
const STEP_MS = 500;

/** Spawn a new psmux window running pi with the task; returns the window
 *  index, or null when not inside psmux / the window never came up. */
export async function defaultDelegateWindow(repo: string, task: string): Promise<string | null> {
  if (!process.env.TMUX_PANE) return null;
  const name = `rxaf-${Date.now().toString(36).slice(-6)}`;
  const spawnArgs = [
    "psmux", "new-window", "-c", repo.replace(/\\/g, "/"), "-n", name, "-P", "-F", "#{window_index}",
    "--", "pwsh", "-NoLogo", "-c", `pi '${task}'`,
  ];
  const res = await runArgv(psmuxArgs(spawnArgs), 20000);
  const idx = res.stdout.trim().split("\n").pop() ?? "";
  if (res.code !== 0 || !idx) return null;
  const deadline = Date.now() + VERIFY_MS;
  while (Date.now() < deadline) {
    const list = await runArgv(psmuxArgs(["psmux", "list-windows", "-F", "#{window_index}"]), 10000);
    if (list.stdout.split("\n").map((s) => s.trim()).includes(idx)) return idx;
    await sleep(STEP_MS);
  }
  return null;
}
