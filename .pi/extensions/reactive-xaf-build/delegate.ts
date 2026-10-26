/**
 * reactive-xaf-build/delegate — run a task in a NEW psmux window.
 *
 * defaultDelegateWindow spawns a fresh pi session in a new window with the
 * task as its first message ("pi '<task>'") — the new window boots already
 * working, no send-keys race. The invoking command returns immediately; the
 * flow (prompts, build pane, notifies, steers) continues in the new window
 * and survives the invoking session's close.
 *
 * The task text is interpolated into pwsh single quotes; a quote inside it is
 * DOUBLED (`quoteTask`), never dropped — the brief carries AzDO test names now,
 * and a task that silently lost a character would be a brief nobody can trust.
 *
 * `launch` boots the window as a model role (`--provider/--model/--thinking`):
 * the same launch line psmux uses for a role window, built here because this
 * spawner is the one that boot-verifies its window.
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

/** A model role's launch triple. The caller resolves it (a store lookup is not
 *  this module's business); this module only knows how it rides the command. */
export interface RoleLaunch {
  provider: string;
  model: string;
  thinkingLevel?: string;
}

/** A spawner that can boot the window as a role. */
export type WindowSpawner = (repo: string, task: string, launch?: RoleLaunch) => Promise<string | null>;

/** The model-role a failing AzDO test is delegated to. The operator store
 *  carries it as `deepseek` / `deepseek-flash` / `thinking max`. */
export const DEFAULT_TEST_ROLE = "Test-Runner";

/** The role's launch triple, or null when the store has no such role. The store
 *  is the model-role extension's own slot, read at CALL time — a role assigned
 *  while a build runs is picked up by the next offer. A role entry with no
 *  provider or no modelId resolves to NOTHING: an invented model would spawn a
 *  window on whatever the caller guessed. */
export function roleLaunch(role: string, store = (globalThis as any).__pi_model_roles): RoleLaunch | null {
  const entry = store?.[role];
  if (!entry || !entry.provider || !entry.modelId) return null;
  const thinkingLevel = entry.thinkingLevel ? String(entry.thinkingLevel) : undefined;
  return { provider: String(entry.provider), model: String(entry.modelId), thinkingLevel };
}

/** The flags that boot a window as a role, empty when there is no role. */
function launchFlags(launch?: RoleLaunch): string {
  if (!launch) return "";
  const level = launch.thinkingLevel ? ` --thinking ${launch.thinkingLevel}` : "";
  return `--provider ${launch.provider} --model ${launch.model}${level} `;
}

/** A task inside pwsh single quotes: a quote is doubled, so the brief survives
 *  the shell instead of ending it. */
function quoteTask(task: string): string {
  return task.replace(/'/g, "''");
}

export interface DelegateDeps {
  run?: (argv: string[]) => Promise<RunResult>;
  windowExists?: (idx: string) => Promise<boolean>;
  killWindow?: (idx: string) => Promise<void>;
  graceMs?: number;
  launch?: RoleLaunch;
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
    "--", "pwsh", "-NoLogo", "-c", `pi ${launchFlags(deps.launch)}'${quoteTask(task)}'`,
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
