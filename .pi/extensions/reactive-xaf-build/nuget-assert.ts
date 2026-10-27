/** reactive-xaf-build/nuget-assert — did the publish actually become readable?
 *
 * The publish pipeline reports success when its push steps finish, and nuget.org
 * answers `201 Created` long before the package can be read: it validates the
 * batch and only then commits it to its catalog. Asking the public feed keeps
 * "pushed" and "available" two separate facts; the verdict belongs to the voices
 * in `watcher-notice.ts`, and `watcher.ts` decides what to do with it.
 */

import { sleep } from "./pane.js";
import { nugetAssertUrl, nugetOrgNuspecUrl, compareVersions, publishedVersion } from "./profile.js";
import type { RepoProfile } from "./profile.js";
import type { BuildSeams } from "./build.js";

const NUGET_VERSION_RE = /Version='([^']+)'/g;

/** The nuget.org window, as attempts x the delay between them. nuget.org
 *  validates a pushed batch BEFORE committing it to its catalog, and that wait is
 *  minutes, not seconds: the 2026-10-26 Release pushed 56 packages at 18:49 and
 *  nuget.org carried the last of them — Xpand.Extensions — at 19:07, 18 minutes,
 *  against under one minute for 2026-09-20. The old 10 x 30s budget expired
 *  mid-wait and steered the user over a publish that had already succeeded.
 *  30 minutes covers the measured latency; past it the package really is missing,
 *  and the caller still warns and steers. */
const NUGET_ASSERT_ATTEMPTS = 30;
const NUGET_ASSERT_EVERY_MS = 60_000;

/** The version this checkout published — the ONE reader lives in profile.ts
 *  (`publishedVersion`), re-exported here for the modules that already import
 *  this half of the publish chain. */
export { publishedVersion };

function normalizeNugetVersion(version: string): string {
  const parts = version.split(".");
  while (parts.length > 2 && parts[parts.length - 1] === "0") parts.pop();
  return parts.join(".");
}

async function tryAssertNugets(seams: BuildSeams, p: RepoProfile, choice: "Lab" | "Release", version: string): Promise<{ ok: boolean; detail: string }> {
  if (p.nugetFeed(choice) === "nuget.org") {
    const normalized = normalizeNugetVersion(version);
    try {
      await seams.fetchFeed(nugetOrgNuspecUrl(p.nugetId, normalized));
      return { ok: true, detail: `${p.nugetId} ${normalized} found on nuget.org` };
    } catch {
      return { ok: false, detail: `${p.nugetId} ${normalized} NOT found on nuget.org` };
    }
  }
  try {
    const text = await seams.fetchFeed(nugetAssertUrl(p.nugetId));
    const versions = [...text.matchAll(NUGET_VERSION_RE)].map((m) => m[1]);
    const found = versions.some((v) => compareVersions(v, version) === 0);
    return { ok: found, detail: `${p.nugetId} ${version} ${found ? "found" : "NOT found"} on the eXpand nuget server` };
  } catch (err) {
    return { ok: false, detail: `feed query failed: ${err instanceof Error ? err.message : String(err)}` };
  }
}

/** Ask the feed until the published version answers, or the window runs out. The
 *  caller passes `opts.nugetRetries` / `opts.nugetRetryMs` only to narrow it. */
export async function assertNugets(
  seams: BuildSeams,
  p: RepoProfile,
  repoRoot: string | undefined,
  choice: "Lab" | "Release",
  retries = NUGET_ASSERT_ATTEMPTS,
  retryMs = NUGET_ASSERT_EVERY_MS,
): Promise<{ ok: boolean; detail: string }> {
  const version = publishedVersion(repoRoot, p.versionFile);
  if (!version) {
    return { ok: false, detail: `no version read from ${p.versionFile}` };
  }
  let last: { ok: boolean; detail: string } | null = null;
  for (let i = 0; i < retries; i++) {
    last = await tryAssertNugets(seams, p, choice, version);
    if (last.ok) return last;
    if (i < retries - 1) await sleep(retryMs);
  }
  return last!;
}
