/**
 * reactive-xaf-build — /devexpress command for the Reactive.XAF lab/release workflow.
 *
 * Boot only: the surface (menu.ts) owns the command and drives the engine
 * (build.ts → run.ts/publish.ts/watcher.ts). The pi parameter is typed as any
 * because the installed pi typings declare no ExtensionAPI type — the per-file
 * tsc gate cannot resolve it either way.
 */

import { registerBuildCommand } from "./menu.js";

export default function (pi: any): void {
  registerBuildCommand(pi);
}
