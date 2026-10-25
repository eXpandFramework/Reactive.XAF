/**
 * reactive-xaf-build/github-release — the chain's LAST step: get the GitHub
 * release for the built version actually published.
 *
 * Split out of watcher.ts at the 400-line cap. The watcher keeps the decision
 * (which repo, which policy, which version); this module keeps the attempt: the
 * token check, the draft PATCH, and the retry loop that tolerates the index lag
 * between a nuget push and GitHub's view of the release.
 *
 * `onSuccess` is the profile's policy, never a guess: `publishDraft` turns the
 * draft into a release (`prerelease` for Lab), `assertPublished` waits for a
 * human to do it and only asserts that it happened. The token is never logged,
 * and a missing one is reported as an unconfirmed release, not as success.
 */

import { githubReleasesUrl, githubReleaseUrl } from "./profile.js";
import type { GithubOnSuccess } from "./profile.js";
import { sleep } from "./pane.js";

export type GhFetch = (url: string, opts?: { method?: string; body?: string }) => Promise<{ ok: boolean; status: number; text: string }>;

/** Everything one release attempt needs — resolved by the watcher from the
 *  profile and the options, so nothing here reads the environment for policy. */
export interface ReleaseTarget {
  gh: GhFetch;
  repo: string;
  onSuccess: GithubOnSuccess;
  prerelease: boolean;
  version: string;
  token: string | undefined;
  attempts: number;
  delayMs: number;
}

function findRelease(releases: Array<{ id?: number; tag_name?: string; draft?: boolean }>, version: string): { draftId?: number; published?: boolean } {
  for (const r of releases) {
    if (r.tag_name !== version) continue;
    if (r.draft === true && typeof r.id === "number") return { draftId: r.id };
    if (r.draft === false) return { published: true };
  }
  return {};
}

async function publishDraft(gh: GhFetch, repo: string, id: number, prerelease: boolean): Promise<{ ok: boolean; detail?: string }> {
  const patch = await gh(githubReleaseUrl(repo, id), {
    method: "PATCH",
    body: JSON.stringify({ draft: false, prerelease }),
  });
  if (patch.ok) return { ok: true };
  return { ok: false, detail: `GitHub publish failed: HTTP ${patch.status}` };
}

type AttemptOutcome =
  | { kind: "publishedDraft"; detail: string }
  | { kind: "published"; detail: string }
  | { kind: "http"; detail: string }
  | { kind: "retry" };

async function githubAttempt(t: ReleaseTarget): Promise<AttemptOutcome> {
  const res = await t.gh(githubReleasesUrl(t.repo));
  if (!res.ok) return { kind: "http", detail: `GitHub query failed: HTTP ${res.status}` };
  const found = findRelease(JSON.parse(res.text), t.version);
  if (found.draftId !== undefined) {
    if (t.onSuccess === "assertPublished") return { kind: "retry" };
    const pub = await publishDraft(t.gh, t.repo, found.draftId, t.prerelease);
    if (pub.ok) {
      const kind = t.prerelease ? "pre-release" : "release";
      return { kind: "publishedDraft", detail: `GitHub ${kind} ${t.version} published from draft` };
    }
    return { kind: "http", detail: pub.detail ?? "GitHub publish failed" };
  }
  if (found.published) return { kind: "published", detail: `GitHub release ${t.version} already published` };
  return { kind: "retry" };
}

/** How the release attempt ended. The module says WHAT happened; the watcher
 *  decides what to say about it — no toast, no steer, no `ctx` in here. */
export type ReleaseOutcome =
  | { kind: "published"; detail: string }
  | { kind: "no-token"; detail: string }
  | { kind: "not-confirmed"; detail: string };

/** Publish (or assert) the release for `version`. Every path returns one of the
 *  three outcomes; `not-confirmed` means NOT confirmed, never "probably fine". */
export async function publishRelease(t: ReleaseTarget): Promise<ReleaseOutcome> {
  if (!t.token) {
    return { kind: "no-token", detail: `GH_TOKEN is not set — the draft release ${t.version} must be published manually on GitHub` };
  }
  for (let i = 0; i < t.attempts; i++) {
    try {
      const out = await githubAttempt(t);
      if (out.kind === "publishedDraft" || out.kind === "published") return { kind: "published", detail: out.detail };
      if (out.kind === "http" && i === t.attempts - 1) return { kind: "not-confirmed", detail: out.detail };
    } catch (err) {
      if (i === t.attempts - 1) {
        return { kind: "not-confirmed", detail: `GitHub publish failed: ${err instanceof Error ? err.message : String(err)}` };
      }
    }
    if (i < t.attempts - 1) await sleep(t.delayMs);
  }
  return { kind: "not-confirmed", detail: `GitHub release ${t.version} NOT found after ${t.attempts} tries` };
}
