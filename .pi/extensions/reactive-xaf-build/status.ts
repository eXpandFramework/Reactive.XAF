/**
 * reactive-xaf-build/status — one-shot AzDO status for the last build.
 *
 * statusPhase queries the newest build of a definition (azdoStatusScript via
 * the run seam) and notifies the outcome: id, current state, the ##[error]
 * reason on failure, and the AzDO definition link. cancelPhase PATCH-cancels
 * EVERY running/queued build of the project (project-wide, no definition
 * filter). Definition comes from profile.statusDef (RX default Lab = 23).
 * Info only — no steering.
 *
 * "Last build" answers a second question too: what became of the GITHUB RELEASE
 * for the version that was built (`stamp.ts` `lastRelease`). "Never attempted"
 * and "attempted and failed" both used to look like nothing at all — which is
 * how a draft came to be published by hand.
 */

import { azdoStatusScript, parsePoll, extractFailReason, failLogFromStdout, azdoBuildUrl, cancelAzDoScript, parseCancel } from "./azdo.js";
import type { AzDoFailure } from "./azdo.js";
import type { BuildSeams } from "./build.js";
import { profileOf } from "./profile.js";
import { lastRelease } from "./release-store.js";

/** Why an AzDO call could not be read, in the user's words. `auth` is the one
 *  only they can fix. */
function failureMessage(failure: AzDoFailure, message: string): string {
  if (failure === "auth") {
    return `the AzDO token is expired or refused, so nothing can be read (${message}). Renew it (Azure DevOps → User settings → Personal access tokens), then queue the pipeline again with Publish.`;
  }
  return `the call failed (${message}) — the pipeline may be fine, retry.`;
}

/** The release half of "last build": what happened to the GitHub release for the
 *  version that was built, or the reason nothing is recorded. */
async function releaseLine(seams: BuildSeams): Promise<string> {
  const read = await lastRelease(seams);
  if (!read.record) return read.reason;
  const rec = read.record;
  // A row whose version is empty is the release that could not even start: it
  // must read as NOT ATTEMPTED, never as a blank version that looks pending.
  if (!rec.version) return `GitHub release: not attempted — ${rec.detail}`;
  return `GitHub release ${rec.version} ${rec.status} — ${rec.repo}: ${rec.detail}`;
}

/** Query the last build and notify the outcome; returns the message. */
export async function statusPhase(ctx: any, seams: BuildSeams, definition?: string): Promise<string> {
  definition ??= profileOf(seams).statusDef("Lab");
  const url = azdoBuildUrl(definition);
  const res = await seams.run(azdoStatusScript(definition), { timeoutMs: 60000 });
  const poll = parsePoll(res.stdout);
  let msg: string;
  if (!poll) {
    const err = res.stderr.trim().slice(-300);
    msg = err ? `AzDO status check failed: ${err}` : "AzDO status check failed: no STATUS= line";
  } else if (poll.kind === "error") {
    msg = `AzDO status check failed: ${failureMessage(poll.failure, poll.message)}`;
  } else if (poll.kind === "empty") {
    msg = "No AzDO builds found for this definition.";
  } else {
    const s = poll.build;
    if (["notStarted", "inProgress", "cancelling"].includes(s.status)) {
      msg = `AzDO build ${s.id} is ${s.status} — ${url}`;
    } else if (s.result === "failed") {
      const reason = s.reason || extractFailReason(failLogFromStdout(res.stdout));
      msg = `AzDO build ${s.id} FAILED — ${reason || "no error lines"} — ${url}`;
    } else if (s.result === "canceled") {
      msg = `AzDO build ${s.id} canceled — ${url}`;
    } else {
      msg = `AzDO build ${s.id} ${s.result} — ${url}`;
    }
  }
  const text = `${msg}\n${await releaseLine(seams)}`;
  await ctx.ui.notify(text, "info");
  return text;
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
  } else if (c.failure) {
    msg = `AzDO cancel failed: ${failureMessage(c.failure, c.message ?? "")}`;
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
