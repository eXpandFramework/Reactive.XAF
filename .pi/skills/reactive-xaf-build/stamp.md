---
name: reactive-xaf-build/stamp
description: Use when working on the green-build proof behind the "Build & Publish" row — the SQLite build_stamp table keyed by (profile.label, mode), its schema version and rebuild rule, the four freshness conditions, when a green run records the row, and the one contract every store failure follows.
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
  profile   TEXT    NOT NULL,
  mode      TEXT    NOT NULL,
  head_sha  TEXT    NOT NULL,
  clean_src INTEGER NOT NULL,
  built_at  INTEGER NOT NULL,
  version   TEXT    NOT NULL,
  PRIMARY KEY (profile, mode)
);
```

- **`profile`** is `profile.label` (`Reactive.XAF`, `eXpand`) — half of the key.
  An RX-XAF green build proves nothing about the eXpand tree, so it must never
  make an eXpand publish look fresh.
- **`mode`** is the configuration — `Lab` or `Release`. Lab and Release compile
  different code, so one proves nothing about the other: a Lab-green stamp
  never makes a Release publish look fresh, and each configuration keeps its
  own row.
- **`version`** is what `profile.versionFile` named when the build passed
  (`src/Common/AssemblyInfoVersion.cs`, `Xpand/Xpand.Utils/Properties/
  XpandAssemblyInfo.cs`). A label for the reader, never part of the decision.
- `openDb`/`readDb` come from the platform's shared sqlite extension as
  `@pi/sqlite`, imported on **first use**, not at load: every suite links this
  extension's graph before the `@pi/` name floor exists, and a static import
  fails at link time there. A module that cannot be loaded is handled like every
  other store failure — see "One failure contract" below, never a throw into the
  flow.
- The handle is **released after each operation** (`withStore`): the stamp is
  touched twice per flow, and a connection held for the process lifetime keeps
  the DB and its WAL open inside whatever tree the store points at — a temp
  repo cannot be removed under an open handle (Windows answers EPERM).
- `seams.stampPath` overrides the path; the suites point it at their own temp
  dir so no test ever writes the real store. `seams.stampLoad` overrides how the
  module is reached, so a suite can prove the module-absent path.
- **The shape carries a version.** `PRAGMA user_version` holds
  `SCHEMA_VERSION` (currently 1). A store whose number is older is DROPPED and
  rebuilt by the next write (`openStore`): the stamp is a cache, and one rebuild
  is the correct price. A read of such a store answers "the build stamp store
  predates this rule — the next green build rebuilds it" instead of querying
  columns it may not have. Bump the constant whenever the columns or the key
  change, or the old table survives and every write fails on a column it never
  had.

## `buildNeeded(seams, repo)` — fresh or stale

ONE job, and **stale for anything it cannot read**: a build is the safe side of
every doubt. The signature is `buildNeeded(seams, repo, mode)`, where `mode` is
the configuration being asked about (`Lab` | `Release`). FRESH requires all
four:

1. a row exists for this profile AND this configuration;
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

Called as `recordGreen(seams, repo, mode)` from `buildRunReporter`'s green arm
**after** the publish returns. The
publish's commit moves HEAD without changing content, so the row is recorded
against the HEAD the next flow will actually see; recording it before the
publish would leave every stamp one commit behind and the row would rebuild
every time.

It returns `null` when the row was written and the reason as a string when it
was not — a store that cannot be written never fails the build that just
succeeded, and the flow steers the reason as a warning (`steerWatch`, no model
turn, no toast). A red build records nothing.

## One failure contract

Every store failure — the shared sqlite module cannot be loaded, the file is not
a database, the shape is older than `SCHEMA_VERSION`, the write is refused —
degrades instead of throwing into the flow:

- a READ failure answers STALE, carrying its reason into the started notice;
- a WRITE failure returns its reason, which the flow steers once
  (`steerWatch`: a warning in the conversation, no toast, no model turn).

Nothing here can decide that a publish does not happen. There is no "throws
loudly" path to reason about: a missing module is the same handled result as an
unreadable file (BP9).

## What the tests pin

`build-tests.ts`: BP1 (no row → builds, then publishes on green), BP2 (a green
run records the row, the next pick publishes with no second build), BP3 (a live
run is refused before any read), BP4 (a red build leaves no row), BP5 (**an
RX-XAF row does not make an eXpand pick fresh — it builds**), BP6 (an unreadable
store builds and says why; a failed write still lets the publish land), BP7 (**a
Lab-green row does not make the Release pick fresh, and the Lab row survives the
Release run**), BP8 (**a store from the old shape is rebuilt by the next green
build, so the row is not stranded**), BP9 (**a store module that cannot load
still builds and still publishes**).
