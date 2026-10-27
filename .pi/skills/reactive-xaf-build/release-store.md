---
name: reactive-xaf-build/release-store
description: Use when changing what the extension remembers about a GitHub release — the release_state rows, the write that survives a flow, and the read "Last build status" prints.
---

# release-store.ts — did the release happen?

Companion of `.pi/extensions/reactive-xaf-build/release-store.ts`, split out of
`stamp.ts` at the 400-line cap. The two share one DB but not one question: "was
this tree built?" (`build_stamp`, `stamp.md`) versus "did the release happen?"
(`release_state`, here). The sqlite plumbing stays in `stamp.ts`; these rows go
through its `withDb` door, so nothing is copied.

## The table

```sql
CREATE TABLE IF NOT EXISTS release_state (
  profile    TEXT    NOT NULL,
  version    TEXT    NOT NULL,
  mode       TEXT    NOT NULL,
  repo       TEXT    NOT NULL,
  status     TEXT    NOT NULL,
  detail     TEXT    NOT NULL,
  updated_at INTEGER NOT NULL,
  PRIMARY KEY (profile, version)
);
```

The DDL lives in `stamp.ts` beside `build_stamp` — one schema, one place that
applies it — and the key is `(profile, version)`, so re-running a publish
REPLACES the row instead of growing a history. That is deliberate: the question
is "how does this version stand now", and the answer must not depend on how many
times it was asked.

## `recordRelease(seams, { version, mode, repo, status, detail })`

`INSERT … ON CONFLICT(profile, version) DO UPDATE` — one row per profile and
version. Answers `null` when it was written and a reason string when it was not,
because the release itself already happened: a store failure must never change
what the flow reports. `status` is the `ReleaseStatus` union
(`waiting` / `published` / `already-published` / `no-draft` / `no-token` /
`failed`); `waiting` is what a release watch leaves behind while it polls, so a
release that never happens still says something instead of nothing.

Three callers write it: the chain's last step (`watcher.ts`
`publishGitHubRelease`), the flow's own end (`github-release.ts`
`releasePhase`), and the release watch on every retry.

## `lastRelease(seams)` → `{ record, reason }`

The newest row for the profile, plus the reason when there is none. The read
opens the store WRITABLE on purpose: opening materializes the schema, so a store
written before this table existed answers "no GitHub release attempt is recorded
yet" instead of "no such table".

`status.ts` `releaseLine` turns it into the line `Last build status` prints beside
the AzDO one, and that is the whole point of the table — the question "did the
release go out?" is answered from the extension's own record, not by opening
GitHub, and a run that ended two hours ago still answers.

## The error contract

Same as `stamp.md`: a write answers a reason, a read answers `{ record: null,
reason }`, and nothing throws into a flow that has already published. Both
messages name the store path, because the path is the first thing to check when
the row is not there.
