/**
 * reactive-xaf-build/text — the pure string helpers the flow's modules share.
 *
 * A LEAF on purpose. `report.ts` builds and delivers the flow's messages,
 * `gitphase.ts` and `publish.ts` run commands: both need to say what a command
 * said, and neither should have to depend on the delivery layer (or on each
 * other) to say it. Nothing here imports anything.
 */

/** The last `n` characters of a captured stream, marked when truncated. An
 *  absent stream is an empty one: a seam may hand back only what it read. */
export function tail(s: string | undefined, n = 1500): string {
  const t = (s ?? "").trim();
  return t.length <= n ? t : "..." + t.slice(-n);
}

/** What a failed command said: stderr when it spoke, else stdout — git explains
 *  most of its failures there ("nothing to commit, working tree clean" is
 *  stdout, not stderr) — and a named absence when neither stream carried a word.
 *  A bare `failed:` line is the bug this exists to end: the reason was in hand
 *  and thrown away (2026-10-25). */
export function failureText(stderr: string | undefined, stdout: string | undefined, n = 1500): string {
  return tail(stderr, n) || tail(stdout, n) || "no output on either stream";
}
