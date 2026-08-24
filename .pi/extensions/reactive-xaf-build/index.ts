/**
 * reactive-xaf-build — /Build command for the Reactive.XAF lab/release workflow.
 *
 * Submenu: Lab | Release. Flow: DX check (nuget.org latest) → Directory.Packages.props
 * update (ask-first, single shared version only) → brx / brx -Release → publish via
 * prx with Hyper-V agent (C11-C14) + git commit guards. Logic lives in build.ts.
 *
 * The pi parameter is typed as any because the installed pi typings declare no
 * ExtensionAPI type — the per-file tsc gate cannot resolve it either way.
 */

import { registerBuildCommand } from "./build.js";

export default function (pi: any): void {
  registerBuildCommand(pi);
}
