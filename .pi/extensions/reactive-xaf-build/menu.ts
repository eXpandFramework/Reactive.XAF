/**
 * reactive-xaf-build/menu — the /devexpress command surface.
 *
 * Direct args run in the current window: /devexpress status (one-shot AzDO
 * status), /devexpress build lab|release (full flow) and /devexpress publish
 * lab|release (skip-build: publish + monitor only, no brx). Menu picks run
 * in the INVOKING window too — the build pane splits it to the right
 * (pane.ts); no delegation (removed 2026-08-25: the user wants the build
 * visible in the window that started it, not in spawned windows).
 */

import { getBuildPane, setBuildPane, defaultClosePane } from "./pane.js";
import { statusPhase, cancelPhase } from "./status.js";
import type { BuildSeams } from "./build.js";

type BuildFlowRunner = (choice: string, skipBuild?: boolean) => Promise<string>;

/** The interactive menu (used when no direct args were given). */
async function menuFlow(ctx: any, seams: BuildSeams, runFlow: BuildFlowRunner): Promise<string> {
  const pane = getBuildPane();
  const base = ["Build", "Publish", "Last build status", "Cancel AzDO build"];
  const top = await ctx.ui.select("DevExpress", pane ? [...base, "Close build pane"] : base);
  if (top === "Close build pane") {
    await (seams.closePane ?? defaultClosePane)(pane!);
    setBuildPane(null);
    await ctx.ui.notify(`Build pane ${pane} closed.`, "info");
    return "Build pane closed.";
  }
  if (top === "Last build status") {
    return statusPhase(ctx, seams);
  }
  if (top === "Cancel AzDO build") {
    return cancelPhase(ctx, seams);
  }
  if (top === "Publish") {
    const rx = await ctx.ui.select("RX-XAF", ["Lab", "Release"]);
    if (rx !== "Lab" && rx !== "Release") return "RX-XAF: aborted (no flow selected).";
    return runFlow(rx, true);
  }
  if (top !== "Build") return "DevExpress menu: aborted.";
  const build = await ctx.ui.select("Build", ["RX-XAF"]);
  if (build !== "RX-XAF") return "Build menu: aborted.";
  const rx = await ctx.ui.select("RX-XAF", ["Lab", "Release"]);
  if (rx !== "Lab" && rx !== "Release") return "RX-XAF: aborted (no flow selected).";
  return runFlow(rx);
}

/** Entry: direct args first, then the menu. The repo guard lives in build.ts. */
export async function runDevexpressMenu(ctx: any, seams: BuildSeams, args: string | string[], runFlow: BuildFlowRunner): Promise<string> {
  const parts = (typeof args === "string" ? args.split(/\s+/) : args ?? []).filter(Boolean);
  if (parts[0] === "status") return statusPhase(ctx, seams);
  if (parts[0] === "cancel") return cancelPhase(ctx, seams);
  if (parts[0] === "build") {
    const choice = parts[1]?.toLowerCase();
    if (choice === "lab" || choice === "release") return runFlow(choice === "lab" ? "Lab" : "Release");
  }
  if (parts[0] === "publish") {
    const choice = parts[1]?.toLowerCase();
    if (choice === "lab" || choice === "release") return runFlow(choice === "lab" ? "Lab" : "Release", true);
  }
  return menuFlow(ctx, seams, runFlow);
}
