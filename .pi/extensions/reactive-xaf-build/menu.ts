/**
 * reactive-xaf-build/menu — the /devexpress command surface.
 *
 * Direct args run in the current window: /devexpress status (one-shot AzDO
 * status), /devexpress build lab|release (full flow, no menus — the delegated
 * window runs these). Menu picks delegate to a NEW psmux window (delegate.ts):
 * the flow continues there with its own prompts, build pane, notifies and
 * steers, and survives the invoking session's close. When no window can be
 * spawned, the flow falls back to running here.
 */

import { getBuildPane, setBuildPane, defaultClosePane } from "./pane.js";
import { defaultDelegateWindow } from "./delegate.js";
import { statusPhase, STATUS_TASK } from "./status.js";
import type { BuildSeams } from "./build.js";

type BuildFlowRunner = (choice: string) => Promise<string>;

function buildTask(choice: string): string {
  return `Run the /devexpress build ${choice.toLowerCase()} command now. The DX update, commit and publish confirmations will appear in this window — the user answers them. See the build through to the end and report the outcome.`;
}

/** Delegate to a new window, or run the fallback here when that fails. */
async function delegateOrRun(ctx: any, seams: BuildSeams, repo: string, task: string, fallback: () => Promise<string>, label: string): Promise<string> {
  const window = await (seams.delegateWindow ?? defaultDelegateWindow)(repo, task);
  if (window) {
    const msg = `${label} delegated to window ${window} — it continues there.`;
    await ctx.ui.notify(msg, "info");
    return msg;
  }
  await ctx.ui.notify(`No psmux window available — running ${label.toLowerCase()} here.`, "warning");
  return fallback();
}

/** The interactive menu (used when no direct args were given). */
async function menuFlow(ctx: any, seams: BuildSeams, repo: string, runFlow: BuildFlowRunner): Promise<string> {
  const pane = getBuildPane();
  const top = await ctx.ui.select("DevExpress", pane ? ["Build", "Last build status", "Close build pane"] : ["Build", "Last build status"]);
  if (top === "Close build pane") {
    await (seams.closePane ?? defaultClosePane)(pane!);
    setBuildPane(null);
    await ctx.ui.notify(`Build pane ${pane} closed.`, "info");
    return "Build pane closed.";
  }
  if (top === "Last build status") {
    return delegateOrRun(ctx, seams, repo, STATUS_TASK, () => statusPhase(ctx, seams), "AzDO status check");
  }
  if (top !== "Build") return "DevExpress menu: aborted.";
  const build = await ctx.ui.select("Build", ["RX-XAF"]);
  if (build !== "RX-XAF") return "Build menu: aborted.";
  const rx = await ctx.ui.select("RX-XAF", ["Lab", "Release"]);
  if (rx !== "Lab" && rx !== "Release") return "RX-XAF: aborted (no flow selected).";
  return delegateOrRun(ctx, seams, repo, buildTask(rx), () => runFlow(rx), `Reactive.XAF ${rx} build`);
}

/** Entry: direct args first, then the menu. The repo guard lives in build.ts. */
export async function runDevexpressMenu(ctx: any, seams: BuildSeams, repo: string, args: string | string[], runFlow: BuildFlowRunner): Promise<string> {
  const parts = (typeof args === "string" ? args.split(/\s+/) : args ?? []).filter(Boolean);
  if (parts[0] === "status") return statusPhase(ctx, seams);
  if (parts[0] === "build") {
    const choice = parts[1]?.toLowerCase();
    if (choice === "lab" || choice === "release") return runFlow(choice === "lab" ? "Lab" : "Release");
  }
  return menuFlow(ctx, seams, repo, runFlow);
}
