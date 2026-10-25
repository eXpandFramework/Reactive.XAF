---
name: reactive-xaf-build/github-release
description: Use when changing the chain's last step — publishing the GitHub draft release (or asserting a human published it) after the Reactive.XAF publish chain turns green.
---

# github-release.ts — the chain's last step

Companion of `.pi/extensions/reactive-xaf-build/github-release.ts`, split out of
`watcher.ts` at the 400-line cap. The watcher decides (which repo, which policy,
which version); this module makes the attempt.

## `publishRelease(target)`

No `ctx`, no toast, no steer: this module says WHAT happened and the watcher
decides what to say, so `watcher-notice.ts` keeps the only voice.

Outcome is typed — `{ kind: "published" | "no-token" | "not-confirmed"; detail }`
— and the watcher's wrapper turns `kind === "published"` into `ok`, keeping the
`{ ok, detail }` shape the rest of the chain reads.

`target` is resolved by the watcher from `RepoProfile` and the watcher options:

| field | from |
|---|---|
| `gh` | `seams.ghFetch` (a test's injectable) or `azdo.ts` `defaultGhFetch` |
| `repo` | `profile.githubRepo(choice)` |
| `onSuccess` | `profile.githubOnSuccess(choice)` — `publishDraft` or `assertPublished` |
| `prerelease` | Lab → `true`, Release → `false` |
| `version` | `profile.versionFile` |
| `token` | `opts.ghToken` → `GH_TOKEN` → `GITHUB_TOKEN` |
| `attempts` / `delayMs` | `opts.ghRetries ?? 6` / `opts.ghRetryMs ?? 30_000` |

Behavior:

- **No token** → `kind: "no-token"`, naming the release that must be published by
  hand. A draft is invisible to an unauthenticated caller, so this is a report,
  never a silent skip.
- **`publishDraft`** → PATCHes `{ draft: false, prerelease }` on the draft with
  that tag; success reads `GitHub <pre-release|release> <version> published from
  draft`.
- **`assertPublished`** → never PATCHes; it retries until a non-draft release
  with that tag appears, and reports `already published` when it does.
- **Retries** swallow a query error until the last attempt; an HTTP refusal on
  the last attempt is returned verbatim. Exhausting every attempt says `NOT
  found after N tries` — `kind: "not-confirmed"`, never assumed fine.

The token is never logged, and every path returns a `detail` line so the
watcher's toast and steer carry the same words.
