/**
 * reactive-xaf-build/stamp — the last green build, and the question a
 * "Build & Publish" pick asks before it spends another 10-20 minutes building.
 *
 * A stamp is a PROOF, not a log: one row PER PROFILE AND CONFIGURATION,
 * replaced by every green build of that pair, saying "this tree state was
 * compiled and passed". The key is `(profile.label, mode)`, so neither an
 * RX-XAF build nor a Lab build vouches for an eXpand or a Release publish.
 *
 * `buildNeeded` applies the four conditions together and answers STALE for
 * anything it cannot read: a build is the safe side of every doubt.
 *   1. a row exists for THIS profile and configuration,
 *   2. its head_sha is HEAD now,
 *   3. it was recorded on a tree with no uncommitted source edits,
 *   4. no tracked file under src/ (or Directory.Packages.props) is modified now.
 *
 * The build's OWN version stamp (`profile.versionFile`) is excluded from both
 * dirty reads: every build rewrites it, so counting it would make the tree
 * "dirty" the moment a build finished and no tree would ever read fresh.
 *
 * The same store carries ONE more thing: the GitHub RELEASE the flow managed to
 * publish (`release_state`), so "did the release happen?" is answered by "Last
 * build status" instead of by opening GitHub. A draft nobody published and a
 * release attempt nobody recorded look identical from the outside — that is
 * exactly how 4.261.6.0 came to be published by hand.
 *
 * ONE error contract: every store failure — the shared sqlite module missing,
 * the file unreadable, a shape from an older schema, a refused write — degrades
 * to STALE with a reason (a read) or to a reported reason (a write). Nothing
 * here throws into the flow, because a store must never be able to decide that
 * a publish cannot happen.
 */

import { existsSync } from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { profileOf, publishedVersion } from "./profile.js";
import type { BuildSeams } from "./build.js";

/** One recorded green build. `version` is what the profile's version file
 *  named when the build passed — a label for the reader, never part of the
 *  decision. */
export interface GreenStamp {
  headSha: string;
  cleanSrc: boolean;
  builtAt: number;
  version: string;
}

/** The flow's answer: build or not, and the sentence that says why. */
export interface BuildVerdict {
  fresh: boolean;
  reason: string;
}

/** The shared sqlite store, as this module consumes it. */
export type SqliteModule = typeof import("@pi/sqlite/index.js");
/** How the store is reached. A seam so a suite can prove the failure path. */
export type StampLoader = () => Promise<SqliteModule>;

const SCHEMA = `
CREATE TABLE IF NOT EXISTS build_stamp (
  profile   TEXT    NOT NULL,
  mode      TEXT    NOT NULL,
  head_sha  TEXT    NOT NULL,
  clean_src INTEGER NOT NULL,
  built_at  INTEGER NOT NULL,
  version   TEXT    NOT NULL,
  PRIMARY KEY (profile, mode)
);
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
`;
/** Bumped whenever the shape above changes. A store carrying an older number is
 *  DROPPED and rebuilt — see `openStore`. */
const SCHEMA_VERSION = 1;
const BUSY_TIMEOUT_MS = 5000;
const GIT_TIMEOUT_MS = 30_000;
/** The paths a green build is a proof ABOUT: the sources, and the pin file. */
const SOURCE_PATHS = "src Directory.Packages.props";
/** A read answer for a store written by an older shape. */
const LEGACY = Symbol("legacy-stamp-store");

let sqlite: SqliteModule | null = null;

async function store(): Promise<SqliteModule> {
  if (sqlite === null) sqlite = await import("@pi/sqlite/index.js");
  return sqlite;
}

function storeKey(dbPath: string): string {
  return `reactive-xaf-build:${dbPath}`;
}

/** One operation on the store, with the handle RELEASED before returning: the
 *  stamp is touched twice per flow, and a connection held for the life of the
 *  process would keep the DB (and its WAL) open inside whatever tree the store
 *  points at — a temp repo cannot be removed under an open handle. */
async function withStore(dbPath: string, seams: BuildSeams, fn: (mod: SqliteModule) => any): Promise<any> {
  const mod = seams.stampLoad ? await seams.stampLoad() : await store();
  try {
    return fn(mod);
  } finally {
    mod.closeDb(storeKey(dbPath));
  }
}

/** This extension's own DB — one per extension directory, located from this
 *  module's URL so a project-tree copy owns its own store. */
export function stampDbPath(): string {
  return path.join(path.dirname(fileURLToPath(import.meta.url)), "reactive-xaf-build.db");
}

function dbPathOf(seams: BuildSeams): string {
  return seams.stampPath ?? stampDbPath();
}

/** The row's key: one profile, one proof. */
function profileKey(seams: BuildSeams): string {
  return profileOf(seams).label;
}

function message(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** What the live store's shape says about itself. */
function schemaVersion(db: any): number {
  const row = db.prepare("PRAGMA user_version").get() as { user_version?: number } | undefined;
  return Number(row?.user_version ?? 0);
}

/** Open the store for WRITING and bring its shape to this build's schema. The
 *  stamp is a CACHE, not a record: a store written by an older shape is dropped
 *  and rebuilt, and one rebuild is the correct price. Without this the old table
 *  keeps its old columns, every write fails on a column it never had, and
 *  "Build & Publish" rebuilds forever with only a warning to say why. */
function openStore(mod: SqliteModule, dbPath: string): any {
  const db = mod.openDb(storeKey(dbPath), { path: dbPath, schema: SCHEMA, busyTimeoutMs: BUSY_TIMEOUT_MS });
  if (schemaVersion(db) >= SCHEMA_VERSION) return db;
  db.exec("DROP TABLE IF EXISTS build_stamp");
  db.exec(SCHEMA);
  db.exec(`PRAGMA user_version = ${SCHEMA_VERSION}`);
  return db;
}

/** The store path in force for these seams. */
export function storePath(seams: BuildSeams): string {
  return dbPathOf(seams);
}

/** Run `fn` against this extension's own store, opened (and schema-applied) for
 *  writing, then released. The release rows live in the same DB; this is the one
 *  door they use, so the sqlite plumbing stays here instead of being copied. The
 *  answer is untyped on purpose: a caller knows the row shape it asked for, and
 *  `any` here keeps one door serving both tables. */
export async function withDb(seams: BuildSeams, fn: (db: any, key: string) => any): Promise<any> {
  const dbPath = dbPathOf(seams);
  return withStore(dbPath, seams, (mod) => fn(openStore(mod, dbPath), profileKey(seams)));
}

/** What the profile's version file names, or "" when it cannot be read: a label
 *  for the reader, never part of the decision. The ONE reader lives in
 *  profile.ts — the release attempt and the menu ask the same question. */
function versionOf(repo: string, versionFile: string): string {
  return publishedVersion(repo, versionFile) ?? "";
}

/** A git read's answer: the text it printed, the reason it could not be read.
 *  Both fields always exist, so no caller depends on union narrowing — the
 *  gate's tsc flags do not narrow this shape. */
interface GitRead {
  ok: boolean;
  text: string;
  reason: string;
}

async function gitRead(seams: BuildSeams, repo: string, cmd: string): Promise<GitRead> {
  try {
    const res = await seams.run(cmd, { cwd: repo, timeoutMs: GIT_TIMEOUT_MS });
    if (res.code !== 0) return { ok: false, text: "", reason: `${cmd} failed (exit ${res.code}): ${res.stderr.trim().slice(0, 200)}` };
    return { ok: true, text: res.stdout, reason: "" };
  } catch (err) {
    return { ok: false, text: "", reason: `${cmd} did not run: ${message(err)}` };
  }
}

/** HEAD, or why it could not be read. An EMPTY read is a failure: a green
 *  `rev-parse` that printed nothing proved nothing. */
async function headNow(seams: BuildSeams, repo: string): Promise<{ sha: string } | { reason: string }> {
  const read = await gitRead(seams, repo, "git rev-parse HEAD");
  if (!read.ok) return { reason: read.reason };
  const sha = read.text.trim();
  return sha === "" ? { reason: "git rev-parse HEAD printed no sha" } : { sha };
}

/** A porcelain line's changed path: status letters dropped, quotes unwrapped,
 *  and a rename read as its new name. */
function changedPath(line: string): string {
  const rest = line.slice(3).trim().replace(/^"|"$/g, "");
  return rest.includes(" -> ") ? rest.split(" -> ")[1].trim() : rest;
}

/** The changed tracked files the porcelain text names, minus the build's own
 *  version stamp. */
function sourceChanges(text: string, versionFile: string): string[] {
  const stamp = versionFile.replace(/\\/g, "/");
  return text
    .split("\n")
    .map((line) => line.trimEnd())
    .filter((line) => line.trim() !== "")
    .filter((line) => changedPath(line) !== stamp);
}

/** How many source files are modified RIGHT NOW, or why the tree could not be
 *  read. The one read both the decision and the recording take. */
async function sourcesChanged(seams: BuildSeams, repo: string): Promise<{ changed: number } | { reason: string }> {
  const read = await gitRead(seams, repo, `git status --porcelain -- ${SOURCE_PATHS}`);
  if (!read.ok) return { reason: read.reason };
  return { changed: sourceChanges(read.text, profileOf(seams).versionFile).length };
}

function short(sha: string): string {
  return sha.slice(0, 8);
}

/** Minutes, never below 1: an age does not read "0 min". */
function age(builtAt: number): string {
  return `${Math.max(1, Math.round((Date.now() - builtAt) / 60_000))} min old`;
}

interface StampRow {
  head_sha?: string;
  clean_src?: number;
  built_at?: number;
  version?: string;
}

interface StampRead {
  stamp: GreenStamp | null;
  reason: string;
}

/** The stored proof for one profile AND one configuration, plus the reason when
 *  there is none: a store that cannot be read is REPORTED (the reason goes into
 *  the notice), never silently treated as a green build. */
async function readStamp(seams: BuildSeams, dbPath: string, profile: string, mode: string): Promise<StampRead> {
  const absent: StampRead = { stamp: null, reason: `no green build is recorded for ${profile} ${mode} yet` };
  if (!existsSync(dbPath)) return absent;
  try {
    const found = await withStore(dbPath, seams, ({ readDb }) => readDb(
      storeKey(dbPath),
      (db) => {
        if (schemaVersion(db) < SCHEMA_VERSION) return LEGACY;
        return db
          .prepare("SELECT head_sha, clean_src, built_at, version FROM build_stamp WHERE profile = ? AND mode = ?")
          .get(profile, mode) as StampRow | undefined;
      },
      { path: dbPath },
    ));
    if (found === LEGACY) {
      return { stamp: null, reason: "the build stamp store predates this rule — the next green build rebuilds it" };
    }
    const row = found as StampRow | undefined;
    if (!row) return absent;
    return {
      stamp: {
        headSha: String(row.head_sha ?? ""),
        cleanSrc: row.clean_src === 1,
        builtAt: Number(row.built_at ?? 0),
        version: String(row.version ?? ""),
      },
      reason: "",
    };
  } catch (err) {
    return { stamp: null, reason: `the build stamp could not be read: ${message(err)}` };
  }
}

/** Build, or go straight to publish? The four conditions, for THIS profile and
 *  THIS configuration — Lab and Release compile different code, so one proves
 *  nothing about the other — each answered with the sentence the user reads;
 *  anything unreadable is STALE, which builds. */
export async function buildNeeded(seams: BuildSeams, repo: string, mode: string): Promise<BuildVerdict> {
  const stored = await readStamp(seams, dbPathOf(seams), profileKey(seams), mode);
  if (!stored.stamp) return { fresh: false, reason: stored.reason };
  const head = await headNow(seams, repo);
  if (!("sha" in head)) return { fresh: false, reason: head.reason };
  if (stored.stamp.headSha !== head.sha) {
    return { fresh: false, reason: `HEAD moved since the last green build (${short(stored.stamp.headSha)} → ${short(head.sha)})` };
  }
  if (!stored.stamp.cleanSrc) {
    return { fresh: false, reason: "the last green build ran on a tree with uncommitted source changes" };
  }
  const sources = await sourcesChanged(seams, repo);
  if (!("changed" in sources)) return { fresh: false, reason: sources.reason };
  if (sources.changed > 0) return { fresh: false, reason: `${sources.changed} uncommitted source file(s) under src/` };
  return { fresh: true, reason: `the last green build covers ${short(head.sha)} (${age(stored.stamp.builtAt)})` };
}

/** Record the green build — the proof the next "Build & Publish" consults.
 *  Returns null when it was written, the reason when it was not: a store that
 *  cannot be written never fails the build that just succeeded. */
export async function recordGreen(seams: BuildSeams, repo: string, mode: string): Promise<string | null> {
  const head = await headNow(seams, repo);
  if (!("sha" in head)) return `build stamp not recorded: ${head.reason}`;
  const sources = await sourcesChanged(seams, repo);
  if (!("changed" in sources)) return `build stamp not recorded: ${sources.reason}`;
  const dbPath = dbPathOf(seams);
  const version = versionOf(repo, profileOf(seams).versionFile);
  try {
    await withStore(dbPath, seams, (mod) => {
      openStore(mod, dbPath)
        .prepare(
          "INSERT INTO build_stamp (profile, mode, head_sha, clean_src, built_at, version) VALUES (?, ?, ?, ?, ?, ?) " +
            "ON CONFLICT(profile, mode) DO UPDATE SET head_sha = excluded.head_sha, clean_src = excluded.clean_src, " +
            "built_at = excluded.built_at, version = excluded.version",
        )
        .run(profileKey(seams), mode, head.sha, sources.changed === 0 ? 1 : 0, Date.now(), version);
    });
    return null;
  } catch (err) {
    return `build stamp not recorded: ${message(err)} (store: ${dbPath})`;
  }
}
