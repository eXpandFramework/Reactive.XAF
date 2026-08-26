/**
 * reactive-xaf-build/release — the build.ps1 version derivation.
 *
 * dxBaseVersion maps a DX version to the eXpand base (26.1.4 → 26.1.400.0);
 * releaseVersionTarget picks the version the flow writes: Lab keeps the DX
 * base, Release consults the feeds and takes the next release after the last
 * published version on the same DX minor.
 */

import { profileOf, getLatestPackage, nextReleaseVersion } from "./profile.js";
import type { BuildSeams } from "./build.js";

/** The eXpand base version for a DX version: 26.1.4 → 26.1.400.0 (minor × 100). */
export function dxBaseVersion(dxVersion: string): string | null {
  const m = dxVersion.match(/^(\d+)\.(\d+)\.(\d+)$/);
  return m ? `${m[1]}.${m[2]}.${Number(m[3]) * 100}.0` : null;
}

/** Next build.ps1 version. Lab keeps the DX-derived base; Release consults the feeds
 *  (profile.nugetId on the Xpand server and nuget.org) and takes the next release
 *  after the last published version on the same DX minor (build + 1, revision 0).
 *  A failed consultation aborts — silently falling back to the DX base would ship
 *  a colliding release version. */
export async function releaseVersionTarget(seams: BuildSeams, dxVersion: string, choice: string): Promise<string | null> {
  const base = dxBaseVersion(dxVersion);
  if (!base) return null;
  if (choice !== "Release") return base;
  const p = profileOf(seams);
  const published: string[] = [];
  for (const feed of ["xpand-server", "nuget.org"] as const) {
    try {
      published.push(await getLatestPackage(seams.fetchFeed, p.nugetId, feed));
    } catch (err) {
      throw new Error(`could not consult the last published ${p.nugetId} on ${feed} (${err instanceof Error ? err.message : String(err)}) — release version not computed`);
    }
  }
  return nextReleaseVersion(base, published);
}
