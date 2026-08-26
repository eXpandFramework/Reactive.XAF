/**
 * reactive-xaf-build/status — one-shot AzDO status for the last build.
 *
 * statusPhase queries the newest build of a definition (azdoStatusScript via
 * the run seam) and notifies the outcome: id, current state, the ##[error]
 * reason on failure, and the AzDO definition link. cancelPhase PATCH-cancels
 * EVERY running/queued build of the project (project-wide, no definition
 * filter). Definition comes from profile.statusDef (RX default Lab = 23).
 * Info only — no steering.
 */

import { azdoStatusScript, parseStatus, extractFailReason, failLogFromStdout, azdoBuildUrl, cancelAzDoScript, parseCancel } from "./azdo.js";
import type { BuildSeams } from "./build.js";
import { profileOf } from "./profile.js";

/** Query the last build and notify the outcome; returns the message. */
export async function statusPhase(ctx: any, seams: BuildSeams, definition?: string): Promise<string> {
  definition ??= profileOf(seams).statusDef("Lab");
  const url = azdoBuildUrl(definition);
  const res = await seams.run(azdoStatusScript(definition), { timeoutMs: 60000 });
  const s = parseStatus(res.stdout);
  let msg: string;
  if (!s) {
    const err = res.stderr.trim().slice(-300);
    msg = err ? `AzDO status check failed: ${err}` : "AzDO status check failed: no STATUS= line";
  } else if (s.id === 0) {
    msg = "No AzDO builds found for this definition.";
  } else if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
    msg = `AzDO build ${s.id} is ${s.status} — ${url}`;
  } else if (s.result === "failed") {
    const reason = s.reason || extractFailReason(failLogFromStdout(res.stdout));
    msg = `AzDO build ${s.id} FAILED — ${reason || "no error lines"} — ${url}`;
  } else if (s.result === "canceled") {
    msg = `AzDO build ${s.id} canceled — ${url}`;
  } else {
    msg = `AzDO build ${s.id} ${s.result} — ${url}`;
  }
  await ctx.ui.notify(msg, "info");
  return msg;
}

/** PATCH-cancel every running/queued build of the project and notify the
 *  outcome with the canceled count. */
export async function cancelPhase(ctx: any, seams: BuildSeams): Promise<string> {
  const res = await seams.run(cancelAzDoScript(), { timeoutMs: 60000 });
  const c = parseCancel(res.stdout);
  let msg: string;
  if (!c) {
    const err = res.stderr.trim().slice(-300);
    msg = err ? `AzDO cancel failed: ${err}` : "AzDO cancel failed: no CANCEL= line";
  } else if (c.id === 0) {
    msg = "No AzDO builds found to cancel.";
  } else if (c.ok) {
    const n = Number(c.status);
    msg = `Cancel requested for ${c.status} AzDO build${n === 1 ? "" : "s"} — agents stop them within ~2 min.`;
  } else {
    msg = `AzDO build ${c.id} is ${c.status} — nothing to cancel.`;
  }
  await ctx.ui.notify(msg, "info");
  return msg;
}
