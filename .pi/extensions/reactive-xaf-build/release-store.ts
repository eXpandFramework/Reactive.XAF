/**
 * reactive-xaf-build/release-store — how the GitHub release for a built version
 * stands, and what the flow has to say about it.
 *
 * Split out of stamp.ts: the two share one DB but not one question — "was this
 * tree built?" versus "did the release happen?" — and stamp.ts was over the
 * 400-line cap. The sqlite plumbing stays there; these rows go through its
 * `withDb` door, so nothing is copied.
 *
 * stamp.ts's one error contract holds here too: a store failure answers a REASON
 * (a read) or a reason string (a write). Nothing throws into a release that
 * already happened.
 */

import { existsSync } from "node:fs";
import { storePath, withDb } from "./stamp.js";
import type { BuildSeams } from "./build.js";

/** How the GitHub release for a version stands. `waiting` is the row a watch
 *  leaves behind while it polls, so a release that never happens still says
 *  something instead of nothing. */
export type ReleaseStatus = "waiting" | "published" | "already-published" | "no-draft" | "no-token" | "failed";

/** One release attempt as the store keeps it: one row per profile and version,
 *  replaced by the next attempt. */
export interface ReleaseRecord {
  version: string;
  mode: string;
  repo: string;
  status: ReleaseStatus;
  detail: string;
  updatedAt: number;
}

export interface ReleaseRead {
  record: ReleaseRecord | null;
  reason: string;
}

interface ReleaseRow {
  version?: string;
  mode?: string;
  repo?: string;
  status?: string;
  detail?: string;
  updated_at?: number;
}

function message(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** Record how a release attempt ended. Null when it was written, the reason
 *  when it was not: the release itself already happened, so a store failure
 *  must never change what the flow reports. */
export async function recordRelease(
  seams: BuildSeams,
  rec: { version: string; mode: string; repo: string; status: ReleaseStatus; detail: string },
): Promise<string | null> {
  const dbPath = storePath(seams);
  try {
    await withDb(seams, (db, key) => db
      .prepare(
        "INSERT INTO release_state (profile, version, mode, repo, status, detail, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?) " +
          "ON CONFLICT(profile, version) DO UPDATE SET mode = excluded.mode, repo = excluded.repo, " +
          "status = excluded.status, detail = excluded.detail, updated_at = excluded.updated_at",
      )
      .run(key, rec.version, rec.mode, rec.repo, rec.status, rec.detail, Date.now()));
    return null;
  } catch (err) {
    return `release outcome not recorded: ${message(err)} (store: ${dbPath})`;
  }
}

/** The newest release row for this profile, plus the reason when there is none.
 *  The read opens the store WRITABLE on purpose: opening materializes the
 *  schema, so a store written before this table existed answers "no attempt is
 *  recorded" instead of "no such table". */
export async function lastRelease(seams: BuildSeams): Promise<ReleaseRead> {
  const dbPath = storePath(seams);
  const absent: ReleaseRead = { record: null, reason: "no GitHub release attempt is recorded yet" };
  if (!existsSync(dbPath)) return absent;
  try {
    const row = await withDb(seams, (db, key) => db
      .prepare("SELECT version, mode, repo, status, detail, updated_at FROM release_state WHERE profile = ? ORDER BY updated_at DESC LIMIT 1")
      .get(key) as ReleaseRow | undefined);
    if (!row) return absent;
    return {
      record: {
        version: String(row.version ?? ""),
        mode: String(row.mode ?? ""),
        repo: String(row.repo ?? ""),
        status: String(row.status ?? "failed") as ReleaseStatus,
        detail: String(row.detail ?? ""),
        updatedAt: Number(row.updated_at ?? 0),
      },
      reason: "",
    };
  } catch (err) {
    return { record: null, reason: `the release record could not be read: ${message(err)} (store: ${dbPath})` };
  }
}
