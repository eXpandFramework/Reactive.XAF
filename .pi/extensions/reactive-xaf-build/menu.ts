/**
 * reactive-xaf-build/menu — the /devexpress command surface.
 *
 * Build / Publish always pick RX-XAF | eXpand, then Lab | Release.
 * Direct args (build lab, publish release) stay on the current profile.
 */

import { getBuildPane, setBuildPane, defaultClosePane } from "./pane.js";
import { statusPhase, cancelPhase } from "./status.js";
import type { BuildSeams } from "./build.js";
import { rxProfile, expandProfile } from "./profile.js";

type BuildFlowRunner = (choice: string, skipBuild?: boolean, projectPick?: string) => Promise<string>;

const PROJECT_PICKS = [rxProfile.menuProjectPick, expandProfile.menuProjectPick];

async function pickProject(ctx: any): Promise<string | null> {
  const pick = await ctx.ui.select("Project", PROJECT_PICKS);
  if (pick !== rxProfile.menuProjectPick && pick !== expandProfile.menuProjectPick) return null;
  return pick;
}

async function pickChoice(ctx: any, title: string): Promise<string | null> {
  const rx = await ctx.ui.select(title, ["Lab", "Release"]);
  if (rx !== "Lab" && rx !== "Release") return null;
  return rx;
}

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
  if (top === "Last build status") return statusPhase(ctx, seams);
  if (top === "Cancel AzDO build") return cancelPhase(ctx, seams);
  if (top !== "Build" && top !== "Publish") return "DevExpress menu: aborted.";
  const skipBuild = top === "Publish";
  const project = await pickProject(ctx);
  if (!project) return "Project: aborted (no project selected).";
  const rx = await pickChoice(ctx, project);
  if (!rx) return `${project}: aborted (no flow selected).`;
  return runFlow(rx, skipBuild, project);
}

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
