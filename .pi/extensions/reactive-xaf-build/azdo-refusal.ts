/**
 * reactive-xaf-build/azdo-refusal — the refusal grammar every AzDO query shares.
 *
 * Both queries in this extension spawn pwsh and both can be refused the same
 * way: an expired PAT, a 401, a proxy. The grammar therefore has ONE home — a
 * `try/catch` body that flattens the message and classifies it (`errBlock`) and
 * its reader (`classifiedFailure`) — instead of `azdo.ts` exporting its private
 * helpers to whichever module needed the same words next.
 *
 * `azdo.ts` re-exports `AzDoFailure`, so every existing importer of the type is
 * unchanged. The `kind` union carries the marker each query prints: `STATUS`,
 * `CANCEL`, `TEST`.
 *
 * WHY NOT `$ErrorActionPreference = 'Stop'` script-wide: it made a transient
 * blip indistinguishable from a dead token, and the difference then had to be
 * guessed from error prose downstream. The classifier lives in the script that
 * sees the failure; the consumers switch on the kind.
 */

/** Why a call could not be read. `auth` is final (renew the token); anything
 *  else is worth another poll. */
export type AzDoFailure = "auth" | "transient";

/** The marker a query prints its refusal with. */
export type RefusalKind = "STATUS" | "CANCEL" | "TEST";

/** PS: the catch body that classifies a refused call. The message is flattened
 *  first — a newline or a `;` inside it would break the field protocol the TS
 *  side parses. `auth` is matched on the server's own wording. */
export function errBlock(kind: RefusalKind): string {
  return [
    `  $m = "$($_.Exception.Message)" -replace "\\r?\\n", " " -replace ";", ","`,
    `  $k = if ($m -match '401|Unauthorized|Access Denied|expired') { 'auth' } else { 'transient' }`,
    `  "${kind}=ERR;$k;$m"`,
  ].join("\n");
}

/** The classified failure a line carries, or null when it is a normal line. */
export function classifiedFailure(line: string, kind: RefusalKind): { failure: AzDoFailure; message: string } | null {
  const m = line.match(new RegExp(`^${kind}=ERR;(auth|transient);(.*)$`));
  if (!m) return null;
  return { failure: m[1] as AzDoFailure, message: m[2].trim() };
}
