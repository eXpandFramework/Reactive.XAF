/**
 * reactive-xaf-build/dxpins — the DevExpress pin questions, split out of
 * build.ts when the engine reached the 400-line cap.
 *
 * Pure text and feed readers, nothing else: what nuget.org offers, what the pin
 * file says, how the pin file is rewritten, and how build.ps1's `-version` is
 * moved. No filesystem writes and no prompts — the flow decides when a write
 * happens (`dxPhase`), so this module can be read and tested on its own.
 */

import { compareVersions } from "./profile.js";
import type { FeedFetcher } from "./build.js";

const DX_FEED_URL = "https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json";
const DX_PIN_RE = /Include="(DevExpress\.[^"]*)"\s+Version="([^"]*)"/g;

/** The newest STABLE DevExpress.ExpressApp on nuget.org. */
export async function getLatestDx(fetchFeed: FeedFetcher): Promise<string> {
  const text = await fetchFeed(DX_FEED_URL);
  const versions = (JSON.parse(text).versions as string[] | undefined) ?? [];
  const stable = versions.filter((v) => /^\d+\.\d+\.\d+$/.test(v));
  if (!stable.length) throw new Error("no stable DevExpress.ExpressApp versions on nuget.org");
  stable.sort((a, b) => compareVersions(b, a));
  return stable[0];
}

/** How many DISTINCT DevExpress.* pin versions the props file carries, and the
 *  one version when they all agree. Mixed pins answer null — the flow leaves
 *  such a file untouched. */
export function readDxPins(text: string): { count: number; unique: string | null } {
  const versions = new Set<string>();
  let m: RegExpExecArray | null;
  DX_PIN_RE.lastIndex = 0;
  while ((m = DX_PIN_RE.exec(text)) !== null) versions.add(m[2]);
  return { count: versions.size, unique: versions.size === 1 ? [...versions][0] : null };
}

/** Rewrite every DevExpress.* pin to one version. */
export function rewriteDxVersion(text: string, newVersion: string): string {
  return text.replace(/(Include="DevExpress\.[^"]*"\s+Version=")[^"]*(")/g, `$1${newVersion}$2`);
}

/** Rewrite build.ps1's `-version "X.Y.Z.W"` when it mismatches the target. */
export function rewriteBuildVersion(text: string, version: string): string {
  return text.replace(/-version "(\d+\.\d+\.\d+\.\d+)"/, `-version "${version}"`);
}
