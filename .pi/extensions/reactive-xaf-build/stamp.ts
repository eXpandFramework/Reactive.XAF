/**
 * reactive-xaf-build/stamp — the last green build, and the question a
 * "Build & Publish" pick asks before it spends another 10-20 minutes building.
 *
 * A stamp is a PROOF, not a log: one row PER PROFILE, replaced by every green
 * build of that profile, saying "this tree state was compiled and passed". The
 * key is `profile.label`, so an RX-XAF green build never makes an eXpand
 * publish look fresh. Rows live in SQLite — one DB in this extension's own
 * directory, so nothing grows as files.
 *
 * `buildNeeded` applies the four conditions together and answers STALE for
 * anything it cannot read: a build is the safe side of every doubt.
 *   1. a row exists for THIS profile,
 *   2. its head_sha is HEAD now,
 *   3. it was recorded on a tree with no uncommitted source edits,
 *   4. no tracked file under src/ (or Directory.Packages.props) is modified now.
 *
 * The build's OWN version stamp (`profile.versionFile`) is excluded from both
 * dirty reads: every build rewrites it, so counting it would make the tree
 * "dirty" the moment a build finished and no tree would ever read fresh. Its
 * content is a function of the version being built, not a source edit — which
 * is why the `version` column records it, read from that same file.
 */

import { existsSync, readFileSync } from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { profileOf } from "./profile.js";
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

const SCHEMA = `
CREATE TABLE IF NOT EXISTS build_stamp (
  profile   TEXT    PRIMARY KEY,
  head_sha  TEXT    NOT NULL,
  clean_src INTEGER NOT NULL,
  built_at  INTEGER NOT NULL,
  version   TEXT    NOT NULL
);
`;
const BUSY_TIMEOUT_MS = 5000;
const GIT_TIMEOUT_MS = 30_000;
/** The paths a green build is a proof ABOUT: the sources, and the pin file. */
const SOURCE_PATHS = "src Directory.Packages.props";

/** The platform's shared sqlite store. It is a NAME (`@pi/sqlite`, the
 *  whitelist a project tree has instead of a relative path to the install) and
 *  it is imported on FIRST USE, not at load: every suite statically imports
 *  this extension's graph before the `@pi/` name floor is installed, and a
 *  static import fails at link time there. Absence still throws — loudly, on
 *  the first read or write, never a silent fallback to a file. */
type SqliteModule = typeof import("@pi/sqlite/index.js");
let sqlite: SqliteModule | null = null;

async function store(): Promise<SqliteModule> {
  if (sqlite === null) sqlite = await import("@pi/sqlite/index.js");
  return sqlite;
}

/** One operation on the store, with the handle RELEASED before returning: the
 *  stamp is touched twice per flow, and a connection held for the life of the
 *  process would keep the DB (and its WAL) open inside whatever tree the store
 *  points at — a temp repo cannot be removed under an open handle. */
async function withStore(dbPath: string, fn: (mod: SqliteModule) => any): Promise<any> {
  const key = `reactive-xaf-build:${dbPath}`;
  const mod = await store();
  try {
    return fn(mod);
  } finally {
    mod.closeDb(key);
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

/** The row's key: one profile, one proof. The RX and the eXpand trees carry
 *  their own label, so neither vouches for the other. */
function profileKey(seams: BuildSeams): string {
  return profileOf(seams).label;
}

function message(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** What the profile's version file names, or "" when it cannot be read: a
 *  label for the reader, never part of the decision. The watcher parses the
 *  same file for its nuget assertion, which asks a different question (is the
 *  published version the expected one), so that parse stays where it is. */
function versionOf(repo: string, versionFile: string): string {
  try {
    const text = readFileSync(path.join(repo, versionFile), "utf-8");
    return /(\d+\.\d+\.\d+(?:\.\d+)?)/.exec(text)?.[1] ?? "";
  } catch {
    return "";
  }
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

/** The stored proof for this profile, plus the reason when there is none: a
 *  store that cannot be read is REPORTED (the reason goes into the notice),
 *  never silently treated as a green build. */
async function readStamp(dbPath: string, profile: string): Promise<StampRead> {
  const absent: StampRead = { stamp: null, reason: `no green build is recorded for ${profile} yet` };
  if (!existsSync(dbPath)) return absent;
  try {
    const row = await withStore(dbPath, ({ readDb }) => readDb(
      `reactive-xaf-build:${dbPath}`,
      (db) => db
        .prepare("SELECT head_sha, clean_src, built_at, version FROM build_stamp WHERE profile = ?")
        .get(profile) as StampRow | undefined,
      { path: dbPath },
    ) as StampRow | undefined);
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

/** Build, or go straight to publish? The four conditions, each answered with
 *  the sentence the user reads; anything unreadable is STALE, which builds. */
export async function buildNeeded(seams: BuildSeams, repo: string): Promise<BuildVerdict> {
  const stored = await readStamp(dbPathOf(seams), profileKey(seams));
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
export async function recordGreen(seams: BuildSeams, repo: string): Promise<string | null> {
  const head = await headNow(seams, repo);
  if (!("sha" in head)) return `build stamp not recorded: ${head.reason}`;
  const sources = await sourcesChanged(seams, repo);
  if (!("changed" in sources)) return `build stamp not recorded: ${sources.reason}`;
  const dbPath = dbPathOf(seams);
  const profile = profileKey(seams);
  const version = versionOf(repo, profileOf(seams).versionFile);
  try {
    await withStore(dbPath, ({ openDb }) => {
      openDb(`reactive-xaf-build:${dbPath}`, { path: dbPath, schema: SCHEMA, busyTimeoutMs: BUSY_TIMEOUT_MS })
        .prepare(
          "INSERT INTO build_stamp (profile, head_sha, clean_src, built_at, version) VALUES (?, ?, ?, ?, ?) " +
            "ON CONFLICT(profile) DO UPDATE SET head_sha = excluded.head_sha, clean_src = excluded.clean_src, " +
            "built_at = excluded.built_at, version = excluded.version",
        )
        .run(profile, head.sha, sources.changed === 0 ? 1 : 0, Date.now(), version);
    });
    return null;
  } catch (err) {
    return `build stamp not recorded: ${message(err)}`;
  }
}
