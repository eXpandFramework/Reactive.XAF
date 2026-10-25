/**
 * reactive-xaf-build/tool — the agent's door to the build.
 *
 * `/devexpress` is a menu, and an agent cannot type a slash command. Measured
 * 2026-10-25 in this project: with no door, an agent in this repo ran
 * `pwsh -NoLogo -Command "brx" > /tmp/brx.log 2>&1` through a shell, held the
 * call 227 s, and read a `Build FAILED` out of the log afterwards — while the
 * extension already owned a pane, an exit marker and a watch that would have
 * reported the same failure unprompted.
 *
 * This module is a SURFACE, not a second engine: `run.ts` owns the run
 * lifecycle, `menu.ts` and this tool both drive `runFlow` through
 * `createFlowRunners`. Nothing here builds, watches, or writes run state.
 *
 * No UI is required to start. Worker and headless windows are exactly where a
 * blind shell does the most damage, so the flow runs behind a shim
 * (`uiFreeCtx`): a notice is forwarded when a UI exists and dropped when it
 * does not, and a PROMPT throws instead of answering — see that function for
 * why a non-answer is not safe on two of the flows' paths.
 *
 * Scope limit, stated rather than pretended: the pane and the run live in this
 * process. A build started in another window is invisible here, and `status`
 * says so instead of reporting a quiet repo.
 */

import { activeBuildRun } from "./run.js";
import { defaultCapturePane, getBuildPane } from "./pane.js";
import { createFlowRunners, defaultSeams } from "./build.js";
import type { BuildSeams } from "./build.js";
import { expandProfile, rxProfile } from "./profile.js";

const TOOL_NAME = "devexpress_build";
const CHOICES = ["Lab", "Release"];
const PROJECTS = [rxProfile.menuProjectPick, expandProfile.menuProjectPick];

/** A notice is not a decision, a prompt is. The flows read a NON-ANSWER as
 *  consent on two paths — `dxPhase` rewrites Directory.Packages.props on
 *  anything but Skip/Abort (build.ts:151-157) and `pushDirtyPhase` pushes the
 *  tree as-is on anything but Abort/Commit (gitphase.ts:180-184) — so a
 *  missing UI must throw, never answer. The throw reaches the run's own
 *  reporter, which reports it as the build's outcome. */
function uiFreeCtx(ctx: any): any {
  const base = ctx && typeof ctx === "object" ? ctx : {};
  const ui = base.ui;
  const shim = {
    // A notice is best effort: a failing UI must never become an unhandled
    // rejection on the start path this shim exists to keep safe.
    notify: (message: unknown, type?: unknown) => {
      void Promise.resolve(ui?.notify?.(message, type)).catch(() => undefined);
    },
    select: async (title: string, options?: string[], opts?: unknown) => {
      if (typeof ui?.select !== "function") {
        throw new Error(
          `the build needs a decision ("${title}") and this session has no UI — answer it with /devexpress in an interactive window`,
        );
      }
      return ui.select(title, options, opts);
    },
  };
  const wrapper = Object.create(base);
  Object.defineProperty(wrapper, "ui", { value: shim, enumerable: true, configurable: true });
  return wrapper;
}

/** `Lab`/`Release` from the call, else null. A stated pick is preferred over a
 *  prompt: the tool exists so a caller can start without one. */
function pickChoice(raw: unknown): string | null {
  if (raw === undefined) return "Lab";
  return typeof raw === "string" && CHOICES.includes(raw) ? raw : null;
}

function pickProject(raw: unknown): string | null {
  if (raw === undefined) return rxProfile.menuProjectPick;
  return typeof raw === "string" && PROJECTS.includes(raw) ? raw : null;
}

function elapsed(startedAt: number): string {
  const minutes = Math.max(1, Math.round((Date.now() - startedAt) / 60_000));
  return `${minutes} min`;
}

/** The flow's own message says what it did; this header adds the identity a
 *  caller needs to follow the run with `status`. */
function startedAnswer(message: string): string {
  const run = activeBuildRun();
  if (!run) return message; // refused or aborted: the flow's message IS the answer
  const where = run.pane ? `pane ${run.pane}` : "in-process (no pane could be opened)";
  return `started: run ${run.runId}, ${where}.\n${message}`;
}

function alreadyRunning(run: { runId: string; pane: string | null; startedAt: number }): string {
  const where = run.pane ? `pane ${run.pane}` : "in-process";
  return `not started: run ${run.runId} is already being watched (${where}, ${elapsed(run.startedAt)}).\n` +
    'Look at it with action "status"; stop it with /devexpress → "Abort build". Two builds in this repo collide on' +
    " obj/, bin/, Build/nuspec/* and src/Common/AssemblyInfoVersion.cs, so a second one is never started here.";
}

async function startBuild(pi: any, ctx: any, seams: BuildSeams, params: any): Promise<string> {
  const choice = pickChoice(params?.choice);
  if (!choice) return `${TOOL_NAME}: choice must be Lab or Release (got ${JSON.stringify(params?.choice)}).`;
  const project = pickProject(params?.project);
  if (!project) return `${TOOL_NAME}: project must be ${PROJECTS.join(" or ")} (got ${JSON.stringify(params?.project)}).`;
  const running = activeBuildRun();
  if (running) return alreadyRunning(running);
  const cwd = ctx?.cwd ?? seams.repoRoot ?? process.cwd();
  const runners = createFlowRunners(pi, uiFreeCtx(ctx), seams, cwd);
  return startedAnswer(await runners.runFlow(choice, "build", project));
}

async function buildStatus(seams: BuildSeams): Promise<string> {
  const run = activeBuildRun();
  const pane = getBuildPane();
  const head = run
    ? `run ${run.runId} is being watched here — started ${elapsed(run.startedAt)} ago, ` +
      `${run.pane ? `pane ${run.pane}` : "in-process (no pane)"}.`
    : pane
      ? `pane ${pane} is open, but no run is being watched in this window (the watch ended, or this window reloaded mid-run).`
      : "nothing is watched in THIS window: no run, no build pane. A build started in another window is invisible" +
        " here — its pane, run and exit marker live in that process, so check that window instead.";
  if (!pane) return head;
  const capture = await (seams.capturePane ?? defaultCapturePane)(pane);
  return `${head}\n--- ${pane} tail ---\n${capture.trim() || "(no output yet)"}`;
}

const TOOL_DESCRIPTION =
  "Start or inspect the Reactive.XAF / eXpand build in its right-side psmux pane: action \"start\" runs the same" +
  " flow as /devexpress → Build (DX check, pin update, brx, the outcome reported when it ends) and returns as" +
  " soon as the pane has the command; action \"status\" returns the pane tail and the run in flight. Use this" +
  " instead of running brx/bx/prx/px or the build scripts through a shell — that is refused, because a shell call" +
  " is blind and a second build collides on obj/, bin/, Build/nuspec/* and src/Common/AssemblyInfoVersion.cs." +
  " The pane and the run are per process: another window's build cannot be seen from here.";

/** Register the build door on pi's tool surface. */
export function registerBuildTool(pi: any, seams?: Partial<BuildSeams>): void {
  pi.registerTool({
    name: TOOL_NAME,
    label: "DevExpress build",
    description: TOOL_DESCRIPTION,
    promptSnippet: "Start or inspect the Reactive.XAF build in its pane",
    parameters: {
      type: "object",
      properties: {
        action: {
          type: "string",
          enum: ["start", "status"],
          description: "start = begin a build and return at once; status = the run in flight and the pane tail",
        },
        choice: { type: "string", enum: CHOICES, description: "Lab (default) or Release" },
        project: {
          type: "string",
          enum: PROJECTS,
          description: `${rxProfile.menuProjectPick} (default) or ${expandProfile.menuProjectPick}`,
        },
      },
      required: ["action"],
    },
    execute: async (_id: unknown, params: any, _signal?: unknown, _onUpdate?: unknown, ctx?: any) => {
      const merged = { ...defaultSeams(), ...seams };
      const action = params?.action;
      if (action !== "start" && action !== "status") {
        throw new Error(`${TOOL_NAME}: unknown action ${JSON.stringify(action)} — use "start" or "status".`);
      }
      const text = action === "start" ? await startBuild(pi, ctx, merged, params) : await buildStatus(merged);
      return { content: [{ type: "text", text }] };
    },
  });
}
