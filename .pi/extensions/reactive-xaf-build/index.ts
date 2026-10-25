/**
 * reactive-xaf-build — the /devexpress command and the agent's build door.
 *
 * Boot only: the surfaces (menu.ts, tool.ts) drive the engine (build.ts →
 * run.ts/publish.ts/watcher.ts), and fence.ts refuses the same pipeline through
 * a shell — a door nobody is forced to use is not a fence. `run.ts` owns the
 * run's lifecycle; the surfaces never do. The pi parameter is typed as any
 * because the installed pi typings declare no ExtensionAPI type — the per-file
 * tsc gate cannot resolve it either way. The optional seams are the extension's
 * own injectable ports, so a suite activates the real factory.
 */

import { registerBuildCommand } from "./menu.js";
import { registerBuildTool } from "./tool.js";
import { registerBuildFence } from "./fence.js";
import type { BuildSeams } from "./build.js";

export default function (pi: any, seams?: Partial<BuildSeams>): void {
  registerBuildCommand(pi, seams);
  registerBuildTool(pi, seams);
  registerBuildFence(pi);
}
