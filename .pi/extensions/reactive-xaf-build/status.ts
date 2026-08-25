/**
 * reactive-xaf-build/status — one-shot AzDO status for the last build.
 *
 * statusPhase queries the newest build of a definition (azdoStatusScript via
 * the run seam) and notifies the outcome: id, current state, the ##[error]
 * reason on failure, and the AzDO definition link. cancelPhase PATCH-cancels
 * the newest running build of a definition. Both default to the Reactive.XAF
 * definition (def 23) — Lab and Release builds run on the same pipeline
 * (Release queues branch master). Info only — no steering.
 */

import { azdoStatusScript, parseStatus, extractFailReason, failLogFromStdout, azdoBuildUrl, cancelAzDoScript, parseCancel, LAB_DEF } from "./azdo.js";
import type { BuildSeams } from "./build.js";

/** Query the last build and notify the outcome; returns the message. */
export async function statusPhase(ctx: any, seams: BuildSeams, definition = LAB_DEF): Promise<string> {
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

/** PATCH-cancel the newest running build and notify the outcome. */
export async function cancelPhase(ctx: any, seams: BuildSeams, definition = LAB_DEF): Promise<string> {
  const res = await seams.run(cancelAzDoScript(definition), { timeoutMs: 60000 });
  const c = parseCancel(res.stdout);
  let msg: string;
  if (!c) {
    const err = res.stderr.trim().slice(-300);
    msg = err ? `AzDO cancel failed: ${err}` : "AzDO cancel failed: no CANCEL= line";
  } else if (c.id === 0) {
    msg = "No AzDO builds found to cancel.";
  } else if (c.ok) {
    msg = `Cancel requested for AzDO build ${c.id} — the agent stops it within ~2 min.`;
  } else {
    msg = `AzDO build ${c.id} is ${c.status} — nothing to cancel.`;
  }
  await ctx.ui.notify(msg, "info");
  return msg;
}
