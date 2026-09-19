/**
 * reactive-xaf-build/menu — the /devexpress command surface.
 *
 * Every capability is a menu pick: Build / Publish / Last build status /
 * Cancel AzDO build / Start AzDO watcher, then RX-XAF | eXpand, then Lab |
 * Release. No flags and no subcommand words: a parameter is always a pick or
 * a prompt, never an argument.
 *
 * This module is the composition root. It imports the engine (build.ts) and
 * drives it through the runners it builds; the engine imports no surface.
 */

import { getBuildPane, setBuildPane, defaultClosePane } from "./pane.js";
import { abortBuildRun, activeBuildRun } from "./run.js";
import { statusPhase, cancelPhase } from "./status.js";
import { createFlowRunners, defaultSeams } from "./build.js";
import type { BuildSeams, FlowRunner, WatchStarter } from "./build.js";
import { rxProfile, expandProfile } from "./profile.js";

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

/** The abort entry, labelled with the run it would stop. Empty when no run is
 *  being watched, so the menu falls back to the pane-closing entry. */
function abortLabel(): string {
  const run = activeBuildRun();
  if (!run) return "";
  const mins = Math.max(1, Math.round((Date.now() - run.startedAt) / 60_000));
  return `Abort build (${run.pane ?? "in-process"}, ${mins} min)`;
}

/** Abort: stop the watch and close the pane, the one deliberate kill. A
 *  deliberate stop reports nothing — it is not a build failure. */
async function abortFlow(ctx: any, seams: BuildSeams): Promise<string> {
  const handle = await abortBuildRun(seams.closePane ?? defaultClosePane);
  setBuildPane(null);
  const msg = handle
    ? `Aborted the build run${handle.pane ? ` in pane ${handle.pane}` : ""} — the pane is closed and no failure is reported.`
    : "No build run is being watched.";
  await ctx.ui.notify(msg, "info");
  return msg;
}

/** Pick a project then a choice, for the items that need both. */
async function pickProjectAndChoice(ctx: any): Promise<{ project: string; choice: string } | string> {
  const project = await pickProject(ctx);
  if (!project) return "Project: aborted (no project selected).";
  const choice = await pickChoice(ctx, project);
  if (!choice) return `${project}: aborted (no flow selected).`;
  return { project, choice };
}

export async function runDevexpressMenu(
  ctx: any, seams: BuildSeams, runFlow: FlowRunner, startWatch: WatchStarter,
): Promise<string> {
  const pane = getBuildPane();
  const abort = abortLabel();
  const base = ["Build", "Publish", "Last build status", "Cancel AzDO build", "Start AzDO watcher"];
  const items = abort ? [...base, abort] : pane ? [...base, "Close build pane"] : base;
  const top = await ctx.ui.select("DevExpress", items);
  if (abort && top === abort) return abortFlow(ctx, seams);
  if (top === "Close build pane") {
    await (seams.closePane ?? defaultClosePane)(pane!);
    setBuildPane(null);
    await ctx.ui.notify(`Build pane ${pane} closed.`, "info");
    return "Build pane closed.";
  }
  if (top === "Last build status") return statusPhase(ctx, seams);
  if (top === "Cancel AzDO build") return cancelPhase(ctx, seams);
  if (top === "Start AzDO watcher") {
    const picked = await pickProjectAndChoice(ctx);
    if (typeof picked === "string") return picked;
    return startWatch(picked.choice, picked.project);
  }
  if (top !== "Build" && top !== "Publish") return "DevExpress menu: aborted.";
  const picked = await pickProjectAndChoice(ctx);
  if (typeof picked === "string") return picked;
  return runFlow(picked.choice, top === "Publish", picked.project);
}

/** Register the command: merge the seams, then hand the menu this
 *  invocation's runners from the engine. This is the composition root, so the
 *  dependency runs surface → engine and never the other way. */
export function registerBuildCommand(pi: any, seams?: Partial<BuildSeams>): void {
  pi.registerCommand("devexpress", {
    description: "DevExpress menu: Build | Publish | Last build status | Cancel AzDO build | Start AzDO watcher → RX-XAF | eXpand → Lab | Release",
    handler: async (_args: string | string[], ctx: any) => {
      const merged = { ...defaultSeams(), ...seams };
      const cwd = ctx?.cwd ?? merged.repoRoot ?? process.cwd();
      const runners = createFlowRunners(pi, ctx, merged, cwd);
      return runDevexpressMenu(ctx, merged, runners.runFlow, runners.startWatch);
    },
  });
}
