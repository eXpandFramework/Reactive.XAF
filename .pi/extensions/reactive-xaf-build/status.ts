/**
 * reactive-xaf-build/status — one-shot AzDO status for the last build.
 *
 * statusPhase queries the newest Reactive.XAF build (azdoStatusScript via the
 * run seam) and notifies the outcome: id, current state, the ##[error] reason
 * on failure, and the AzDO definition link. Info only — no steering.
 */

import { azdoStatusScript, parseStatus, AZDO_BUILD_URL } from "./azdo.js";
import type { BuildSeams } from "./build.js";

/** Task handed to the delegated status window. */
export const STATUS_TASK = "Run the /devexpress status command and report its result.";

/** Query the last build and notify the outcome; returns the message. */
export async function statusPhase(ctx: any, seams: BuildSeams): Promise<string> {
  const res = await seams.run(azdoStatusScript(), { timeoutMs: 60000 });
  const s = parseStatus(res.stdout);
  let msg: string;
  if (!s) {
    const err = res.stderr.trim().slice(-300);
    msg = err ? `AzDO status check failed: ${err}` : "AzDO status check failed: no STATUS= line";
  } else if (s.id === 0) {
    msg = "No AzDO builds found for Reactive.XAF.";
  } else if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
    msg = `AzDO build ${s.id} is ${s.status} — ${AZDO_BUILD_URL}`;
  } else if (s.result === "failed") {
    msg = `AzDO build ${s.id} FAILED — ${s.reason || "no error lines"} — ${AZDO_BUILD_URL}`;
  } else if (s.result === "canceled") {
    msg = `AzDO build ${s.id} canceled — ${AZDO_BUILD_URL}`;
  } else {
    msg = `AzDO build ${s.id} ${s.result} — ${AZDO_BUILD_URL}`;
  }
  await ctx.ui.notify(msg, "info");
  return msg;
}
