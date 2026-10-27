---
name: reactive-xaf-build/nuget-assert
description: Use when the publish chain has to answer whether a pushed package is readable yet — the feed query, the version normalization, and the half-hour window that separates an index delay from a missing publish.
---

# nuget-assert.ts — did the publish become readable?

Companion of `.pi/extensions/reactive-xaf-build/nuget-assert.ts`, split out of
`watcher.ts` at the 400-line cap. `watcher.ts` speaks the verdict (a toast and a
steer); this module only asks the feed.

## `assertNugets(seams, p, repoRoot, choice, retries?, retryMs?)`

Asks `tryAssertNugets` until the published version answers, or the window is
spent. `retries` / `retryMs` default to `NUGET_ASSERT_ATTEMPTS` (30) and
`NUGET_ASSERT_EVERY_MS` (60s) — a half-hour window — and a caller can only
NARROW it (`opts.nugetRetries` / `opts.nugetRetryMs`). Returns `{ ok, detail }`;
the detail is the sentence the toast carries, and it names the feed that was
asked.

**Why half an hour.** nuget.org answers `201 Created` when a push finishes and
commits the batch to its catalog afterwards, so the package stays unreadable for
minutes: 2026-08-26 pushed 17:59Z and read back at 18:03Z, while the
56-package 2026-10-26 Release pushed at 18:49 and carried the last package
(`Xpand.Extensions`) at 19:07 — 18 minutes. The old 10 × 30s budget expired
mid-wait and steered "Nugets NOT confirmed" over a publish that had already
succeeded. Past the window the package really is missing, and the caller's
warning plus steer stand.

## The feed

`p.nugetFeed(choice)` decides which one. `nuget.org` gets a nuspec request
(`nugetOrgNuspecUrl`) for `normalizeNugetVersion(version)` — trailing `.0` parts
dropped, so `4.261.6.0` asks for `4.261.6`. The eXpand server gets the flat list
(`nugetAssertUrl`) and matches with `compareVersions`, so `26.1.400.0` matches a
feed `26.1.400`. The URLs and `compareVersions` live in `profile.ts`; nothing
here rebuilds them.

A failed request is `{ ok: false }`, never a throw: as far as the retry loop is
concerned an unreachable feed and a package that is not indexed yet look the
same, and retrying is what tells them apart.

## `publishedVersion(repoRoot, versionFile)`

Reads the version this checkout published out of the profile's version file.
Returns `null` instead of throwing when the repo root is unknown or the file
cannot be read, and `assertNugets` reports that as `no version read from …`.
`watcher.ts` uses the same reader for the GitHub release version and for the
this-run guard on the chain HEAD.
