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

Outcome is typed — `{ kind: "published" | "already-published" | "no-draft" | "no-token" | "failed"; detail }`
— and `statusOf(outcome)` maps it to the `ReleaseStatus` stored in the row,
while `retryable(outcome)` is `no-draft | failed`: the two states the release
watch keeps working on. On the watcher's path the wrapper turns `published`
and `already-published` into `ok`, keeping the `{ ok, detail }` shape the rest
of the chain reads.

`releaseTarget(seams, choice, version, token?, retry?)` resolves `target` from
the `RepoProfile` and the caller's options:

| field | from |
|---|---|
| `gh` | `seams.ghFetch` (a test's injectable) or `azdo.ts` `defaultGhFetch` |
| `repo` | `profile.githubRepo(choice)` |
| `prerelease` | Lab → `true`, Release → `false` |
| `version` | `profile.versionFile`, read by `profile.ts` `publishedVersion` |
| `token` | the caller's `opts.ghToken` → `seams.ghToken` → `GH_TOKEN` → `GITHUB_TOKEN` |
| `attempts` / `delayMs` | `opts.ghRetries ?? 6` / `opts.ghRetryMs ?? 30_000` |

Behavior:

- **No token** → `kind: "no-token"`, naming the release that must be published by
  hand. A draft is invisible to an unauthenticated caller, so this is a report,
  never a silent skip.
- **`publishDraft`** → PATCHes `{ draft: false, prerelease }` on the draft with
  that tag; success reads `GitHub <pre-release|release> <version> published from
  draft`.
- **Already published** → `already-published`, reading `GitHub release <version>
  already published`; no PATCH, so a second run says the same thing instead of
  doing something.
- **No release for the tag yet** → `no-draft`: the pipeline may not have created
  it, and the watch keeps asking.
- **Retries** swallow a query error until the last attempt; an HTTP refusal on
  the last attempt is returned verbatim as `failed`. Exhausting every attempt
  says `NOT
  found after N tries` — `no-draft`, never assumed fine.

The token is never logged, and every path returns a `detail` line so the
watcher's toast and steer carry the same words.

## `releasePhase(ctx, seams, repo, choice, pi?)` — the flow's end

One attempt, then:

- recorded → `announce` (info for `published` / `already-published`);
- retryable → the row says `waiting`, the release watch starts, and one info
  toast says the outcome is recorded and reported;
- nothing to attempt (no version on disk) → `failNoVersion` records and warns
  with the manual URL.

## The release watch — `startReleaseWatch(..., { repo, choice })`

The pipeline uploads a release's assets minutes after its build job, so one
attempt at queue time is too early. The watch is the retry that survives the
flow returning, and `publish.ts` starts it beside the AzDO chain watcher.

- **One watch at a time**, in a global slot (`__rxafReleaseWatch`): a start for
  the same version returns the running handle, a different version stops the old
  one first. `releaseIntervalMs` / `releaseDeadlineMs` on the seams shorten it
  for tests; production is 60 s and 3 h.
- **The first look is AFTER one interval, never immediate.** An immediate tick
  published a fixture's static draft while the chain's own last step was about
  to — two PATCHes where the contract allows one (`watcher-tests` W1).
- Every retry rewrites the row as `waiting`; the final answer records the real
  status and speaks once. A watch stopped by its deadline says
  `not published within the watch budget` with the manual URL.

## The voices — `announce(...)`

`published` / `already-published` → info. `no-draft` → warning +
`steerWarning`. `failed` / `no-token` → warning + `steerError` naming the URL. A
store failure is appended to the same text instead of replacing it, so the
outcome is never lost to a DB problem.

A release that cannot even start is the one failure the user blamed the flow for:
the step they asked for never ran and nothing said so. So BOTH doors — the menu
and the watch the flow starts at queue time — go through `failNoVersion`, which
records the row as `failed` with an empty version and says it once (a warning
toast plus a warning steer naming the version file and the tree). Exactly one
voice per attempt: a tree whose version cannot be read hears one line, not one
per door. The `published` / `already-published` arms stay silent-on-success — an
info toast, no steer.

The fixture repos in `build-tests.ts` therefore carry a version file, exactly
like a real checkout (`mkRepo`); `build-tests` T66 removes it on purpose and
pins the one-warning-one-steer contract, and `release-tests` R10 pins the row it
leaves behind (`GitHub release: not attempted`, never a blank version).

The row both callers write is `release_state`, documented in
`release-store.md`. `release-tests.ts` R5–R11 pin the flow's half: RX Release
publishes the draft (one PATCH, `{draft:false,prerelease:false}`), eXpand
Release does the same against its own repo, an already-published release
answers with no PATCH, a draft that appears late is published by the watch,
no token means no GitHub call and the URL in the toast, `Last build status`
prints the recorded line, and a repo with no version file says so without a
GET.
