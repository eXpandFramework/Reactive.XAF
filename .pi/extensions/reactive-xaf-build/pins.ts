/**
 * reactive-xaf-build/pins — RX package pins in Directory.Packages.props.
 *
 * Expand-only (profile.depPins). After DX, rewrite Xpand.Extensions* /
 * Xpand.XAF.* to the latest from the matching feed, ask-first.
 */

import * as fs from "node:fs";
import { getLatestPackage, readPrefixedPins, rewritePrefixedPins, profileOf } from "./profile.js";
import type { Choice } from "./profile.js";
import type { BuildSeams } from "./build.js";

function trackedWrite(file: string, data: string): void {
  const seam = (globalThis as any).__writeFileSync;
  if (typeof seam !== "function") throw new Error("__writeFileSync seam missing — pi-dev not loaded");
  seam(file, data);
}

export async function depPinsPhase(
  ctx: any,
  seams: BuildSeams,
  propsPath: string,
  choice: string,
): Promise<{ changed: boolean; notes: string[] }> {
  const p = profileOf(seams);
  if (!p.depPins) return { changed: false, notes: [] };
  const notes: string[] = [];
  const text = fs.readFileSync(propsPath, "utf-8");
  const { count, unique } = readPrefixedPins(text, p.depPins.prefixes);
  if (count === 0) {
    notes.push("no RX pins found");
    return { changed: false, notes };
  }
  const latest = await getLatestPackage(seams.fetchFeed, p.depPins.discoverId, p.nugetFeed(choice as Choice));
  if (unique === latest) {
    notes.push(`RX already at ${latest}`);
    return { changed: false, notes };
  }
  if (unique === null) {
    notes.push(`RX pins mixed (${count} versions) — untouched`);
    return { changed: false, notes };
  }
  const pick = await ctx.ui.select(
    `RX ${unique} → ${latest}: update Xpand.Extensions* / Xpand.XAF.* pins?`,
    ["Update", "Skip", "Abort"],
  );
  if (pick === "Abort") throw new Error("aborted at the RX pin prompt");
  if (pick === "Skip") {
    notes.push(`kept RX ${unique} (latest on feed: ${latest})`);
    return { changed: false, notes };
  }
  trackedWrite(propsPath, rewritePrefixedPins(text, p.depPins.prefixes, latest));
  notes.push(`updated RX pins ${unique} → ${latest}`);
  return { changed: true, notes };
}
