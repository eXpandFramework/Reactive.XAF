/**
 * reactive-xaf-build/gitphase — every decision the flow makes about the work
 * tree: what is dirty, what a prompt says about it, and where a commit is
 * asked for.
 *
 * Split out of publish.ts (the 400-line cap): publish.ts owns the VM layer and
 * the phase order, this module owns the git half.
 *
 * Two prompts, ONE commit core. The build's commit step asks whether to commit
 * at all; the pre-push check asks again only when something wrote in the window
 * between them (an IDE, another agent, a background build) and offers to push
 * the tree as it is. Neither answer turns into a second question, so `commitWith`
 * never prompts.
 */

import type { BuildSeams } from "./build.js";

/** A bounded tail for this module's own failure text. report.ts owns the shared
 *  one, which belongs to the flow's messages rather than to a git call. */
function tail(s: string, n = 1500): string {
  const t = s.trim();
  return t.length <= n ? t : "..." + t.slice(-n);
}

export interface DirtyRead {
  ok: boolean;
  lines: string[];
  reason: string;
}

/** One `git status --short` read: the changed lines, or a named failure. A read
 *  that FAILED is never a clean tree — counting the stdout of a broken read
 *  reported "nothing to commit" and let the flow walk over a dirty tree. */
export async function dirtyStatus(seams: BuildSeams, repoRoot: string): Promise<DirtyRead> {
  let res: { code: number; stdout: string; stderr: string };
  try {
    res = await seams.run("git status --short", { cwd: repoRoot, timeoutMs: 30000 });
  } catch (err) {
    return { ok: false, lines: [], reason: `git status did not run: ${err instanceof Error ? err.message : String(err)}` };
  }
  if (res.code !== 0) {
    return { ok: false, lines: [], reason: `git status failed (exit ${res.code}): ${tail(res.stderr)}` };
  }
  return { ok: true, lines: res.stdout.split("\n").filter((l) => l.trim()), reason: "" };
}

/** git's two status letters as one word. An unknown pair reports its own letters
 *  rather than being guessed into a state the tree is not in. */
function stateWord(xy: string): string {
  const t = xy.trim();
  if (t === "??") return "untracked";
  if (t.includes("R")) return "renamed";
  if (t.includes("A")) return "new";
  if (t.includes("D")) return "deleted";
  if (t.includes("U")) return "conflicted";
  if (t.includes("M") || t.includes("T")) return "modified";
  return t || "changed";
}

/** The area a changed path belongs to, as a directory and never a file: one
 *  segment when the rest of the path is the file, two when there is a deeper
 *  directory worth naming, "(root)" for a top-level file. A rename reads as its
 *  new path. */
function areaOf(raw: string): string {
  const next = raw.includes(" -> ") ? raw.split(" -> ")[1] : raw;
  const parts = next.trim().replace(/^"|"$/g, "").split("/").filter(Boolean);
  if (parts.length <= 1) return "(root)";
  return parts.length === 2 ? parts[0] : parts.slice(0, 2).join("/");
}

/** The order the states are counted in, so the same tree always reads alike. */
const STATE_ORDER = ["modified", "new", "deleted", "renamed", "untracked", "conflicted", "changed"];
/** Areas named before the rest are counted. */
const AREA_CAP = 3;

/** The dirty set as a prompt can carry it: how many files by state, then the
 *  areas that carry them. No paths — a prompt has to stay readable. */
export function dirtySummary(lines: string[]): string {
  const counts = new Map<string, number>();
  const areas = new Map<string, number>();
  for (const line of lines) {
    const word = stateWord(line.slice(0, 2));
    counts.set(word, (counts.get(word) ?? 0) + 1);
    const area = areaOf(line.slice(3));
    areas.set(area, (areas.get(area) ?? 0) + 1);
  }
  const byState = STATE_ORDER.filter((w) => counts.has(w))
    .map((w) => `${counts.get(w)} ${w}`)
    .join(", ");
  const ranked = [...areas.entries()].sort((a, b) => b[1] - a[1]);
  const named = ranked.slice(0, AREA_CAP).map(([a, n]) => `${a} (${n})`).join(", ");
  const rest = ranked.length > AREA_CAP ? `, +${ranked.length - AREA_CAP} more areas` : "";
  return `${lines.length} file${lines.length === 1 ? "" : "s"}: ${byState}\nareas: ${named}${rest}`;
}

/** The message a commit carries: the DX bump wins over the flow's label. */
export function commitMessage(label: string, dxChanged: boolean, latest: string, changed: number): string {
  if (dxChanged) return `Update DX to ${latest}`;
  return `${label} (${changed} files)`;
}

export interface CommitOutcome {
  committed: boolean;
  failed: boolean;
  notes: string[];
}

/** Stage the tree and commit it, with no prompt of its own: both callers ask
 *  their question first, so a "commit" answer never becomes a second one. */
export async function commitWith(seams: BuildSeams, repoRoot: string, msg: string): Promise<CommitOutcome> {
  const notes: string[] = [];
  const add = await seams.run("git add -A", { cwd: repoRoot, timeoutMs: 60000 });
  if (add.code !== 0) {
    notes.push(`git add failed: ${tail(add.stderr)}`);
    return { committed: false, failed: true, notes };
  }
  const safeMsg = msg.replace(/"/g, "'");
  const commit = await seams.run(`git commit -m "${safeMsg}"`, { cwd: repoRoot, timeoutMs: 60000 });
  if (commit.code !== 0) {
    notes.push(`git commit failed: ${tail(commit.stderr)}`);
    return { committed: false, failed: true, notes };
  }
  notes.push(`committed: ${msg}`);
  return { committed: true, failed: false, notes };
}

/** The build's commit step: read the tree, ask, commit. A clean tree is not a
 *  prompt ("nothing to commit"), and a read that failed refuses here rather than
 *  committing over a tree nobody could read. */
export async function commitPhase(
  ctx: any, seams: BuildSeams, repoRoot: string, dxChanged: boolean, latest: string, label = "Build fixes",
): Promise<CommitOutcome> {
  const notes: string[] = [];
  const dirty = await dirtyStatus(seams, repoRoot);
  if (!dirty.ok) {
    notes.push(dirty.reason);
    return { committed: false, failed: true, notes };
  }
  if (dirty.lines.length === 0) {
    notes.push("nothing to commit");
    return { committed: true, failed: false, notes };
  }
  const msg = commitMessage(label, dxChanged, latest, dirty.lines.length);
  const pick = await ctx.ui.select(
    `Commit with message: "${msg}"?\n${dirtySummary(dirty.lines)}`,
    ["Commit", "Abort"],
  );
  if (pick !== "Commit") {
    notes.push("commit aborted");
    return { committed: false, failed: false, notes };
  }
  const done = await commitWith(seams, repoRoot, msg);
  return { committed: done.committed, failed: done.failed, notes: [...notes, ...done.notes] };
}

export interface PushCheck {
  failed: boolean;
  aborted: boolean;
}

/** The pre-push check. The commit step runs first and normally leaves the tree
 *  clean, so this fires when something wrote between the two: the push carries a
 *  summary of that work and the user decides whether it goes with a commit or as
 *  it is. Abort stops before the push AND the queue — a pipeline queued on a
 *  commit the user just declined is not a publish. */
export async function pushDirtyPhase(
  ctx: any, seams: BuildSeams, repoRoot: string, remote: string,
  msgOf: (changed: number) => string, notes: string[],
): Promise<PushCheck> {
  const dirty = await dirtyStatus(seams, repoRoot);
  if (!dirty.ok) {
    notes.push(dirty.reason);
    return { failed: true, aborted: false };
  }
  if (dirty.lines.length === 0) return { failed: false, aborted: false };
  const pick = await ctx.ui.select(
    `Dirty working tree before pushing to ${remote}\n${dirtySummary(dirty.lines)}\nCommit before pushing?`,
    ["Commit before push", "Push as is", "Abort"],
  );
  if (pick === "Abort") {
    notes.push("push aborted, the tree is left as it is");
    return { failed: false, aborted: true };
  }
  if (pick !== "Commit before push") return { failed: false, aborted: false };
  const done = await commitWith(seams, repoRoot, msgOf(dirty.lines.length));
  notes.push(...done.notes);
  return { failed: done.failed, aborted: false };
}
