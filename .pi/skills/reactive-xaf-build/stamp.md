---
name: reactive-xaf-build/stamp
description: Use when working on the green-build proof behind the "Build & Publish" row — the SQLite build_stamp table keyed by profile, the four freshness conditions, when a green run records the row, and how a store that cannot be read or written behaves.
---

# stamp.ts — the last green build

Companion of `.pi/extensions/reactive-xaf-build/stamp.ts`. It answers one
question for the menu's **Build & Publish** row: may this publish skip the
build? Detail of the row itself: `menu.md`; the flow modes that call it:
`build.md`.

## The store

One DB in this extension's own directory — `.pi/extensions/reactive-xaf-build/`
`reactive-xaf-build.db` — never a JSON or flag file. One table:

```sql
CREATE TABLE IF NOT EXISTS build_stamp (
  profile   TEXT    PRIMARY KEY,
  head_sha  TEXT    NOT NULL,
  clean_src INTEGER NOT NULL,
  built_at  INTEGER NOT NULL,
  version   TEXT    NOT NULL
);
```

- **`profile`** is `profile.label` (`Reactive.XAF`, `eXpand`). The row is per
  profile on purpose: an RX-XAF green build proves nothing about the eXpand
  tree, so it must never make an eXpand publish look fresh.
- **`version`** is what `profile.versionFile` named when the build passed
  (`src/Common/AssemblyInfoVersion.cs`, `Xpand/Xpand.Utils/Properties/
  XpandAssemblyInfo.cs`). A label for the reader, never part of the decision.
- `openDb`/`readDb` come from the platform's shared sqlite extension as
  `@pi/sqlite`, imported on **first use**, not at load: every suite links this
  extension's graph before the `@pi/` name floor exists, and a static import
  fails at link time there. Absence still throws — loudly, on the first read or
  write, never a silent fallback to a file.
- The handle is **released after each operation** (`withStore`): the stamp is
  touched twice per flow, and a connection held for the process lifetime keeps
  the DB and its WAL open inside whatever tree the store points at — a temp
  repo cannot be removed under an open handle (Windows answers EPERM).
- `seams.stampPath` overrides the path; the suites point it at their own temp
  dir so no test ever writes the real store.

## `buildNeeded(seams, repo)` — fresh or stale

ONE job, and **stale for anything it cannot read**: a build is the safe side of
every doubt. FRESH requires all four:

1. a row exists for this profile;
2. its `head_sha` equals `git rev-parse HEAD` now (an empty read is unreadable,
   hence stale);
3. its `clean_src` is 1;
4. `git status --porcelain -- src Directory.Packages.props` is empty now, after
   dropping the profile's own `versionFile` line.

The exclusion in 4 is not cosmetic: every build rewrites the version file, so
counting it would make the tree "dirty" the moment a build finished and no tree
would ever read fresh. Its content is a function of the version being built,
not a source edit.

Every failure answers with the sentence the user reads, and the flow puts it in
the started notice: `building — <reason>` or
`build not needed — <reason> (N min old)`.

## `recordGreen(seams, repo)` — the proof

Called from `buildRunReporter`'s green arm **after** the publish returns. The
publish's commit moves HEAD without changing content, so the row is recorded
against the HEAD the next flow will actually see; recording it before the
publish would leave every stamp one commit behind and the row would rebuild
every time.

It returns `null` when the row was written and the reason as a string when it
was not — a store that cannot be written never fails the build that just
succeeded, and the flow steers the reason as a warning (`steerWatch`, no model
turn, no toast). A red build records nothing.

## What the tests pin

`build-tests.ts`: BP1 (no row → builds, then publishes on green), BP2 (a green
run records the row, the next pick publishes with no second build), BP3 (a live
run is refused before any read), BP4 (a red build leaves no row), BP5 (**an
RX-XAF row does not make an eXpand pick fresh — it builds**), BP6 (an unreadable
store builds and says why; a failed write still lets the publish land).
