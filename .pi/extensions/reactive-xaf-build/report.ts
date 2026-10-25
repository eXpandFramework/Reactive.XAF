/**
 * reactive-xaf-build/report — the flow's messages and its half of the watch.
 *
 * Pure string building plus the delivery helpers: a warning pairs a toast with
 * a steer, the start notice rides the shared sender without spending a model
 * turn, and a failure names the exit code with a bounded tail. Extracted from
 * build.ts so the flow engine stays inside the file-size cap.
 *
 * ONE DELIVERY DECISION. Every steer variant is a one-line wrapper over
 * `deliver`, which owns the shared-sender lookup, the severity and the turn.
 * The severity is the shared sender's: pi's own `sendMessage` has no channel
 * for it, so the fallback arm (no `__steer` loaded) marks the TEXT instead —
 * `[error] …` — rather than quietly downgrading an error to ordinary chatter.
 */

import type { RunEvent } from "./run.js";

/** Minutes, never below 1: a report does not say "0 min". */
export function minutes(ms: number): number {
  return Math.max(1, Math.round(ms / 60_000));
}

/** The last `n` characters of a captured pane, marked when truncated. */
export function tail(s: string, n = 1500): string {
  const t = s.trim();
  return t.length <= n ? t : "..." + t.slice(-n);
}

export function failureResult(id: string, choice: string, latest: string, notes: string[], build: { code: number; stdout: string }): string {
  return [
    `${id} build — ${choice}`,
    `DX latest: ${latest}`,
    ...notes,
    `Build FAILED (exit ${build.code})`,
    "--- output tail (from the build pane) ---",
    tail(build.stdout, 4000),
    "Fix the warnings, then re-run /devexpress.",
  ].join("\n");
}

export function summaryResult(id: string, choice: string, latest: string, notes: string[], pubOk: boolean): string {
  const dxLine = latest ? `DX latest: ${latest}` : "no DX check (build skipped)";
  return [`${id} build — ${choice}`, dxLine, ...notes, pubOk ? "published" : "publish stopped"].join("\n");
}

/** What a steer asks for. `severity: "error"` is the one the user must act on:
 *  it renders red and its message becomes the blockReason. */
export interface SteerOptions {
  severity?: "error" | "warning";
  triggerTurn?: boolean;
}

const CUSTOM_TYPE = "reactive-xaf-build:build";

/** The single delivery path. With the shared sender (llm-utils, loaded by every
 *  boot) the steer carries a severity and, for an error, the message as its
 *  blockReason. Without it pi's own `sendMessage` has no severity channel, so
 *  the severity rides the TEXT (`[error] …`) — nothing is dropped and nothing is
 *  quietly downgraded to chatter. */
function deliver(pi: any, msg: string, opts: SteerOptions): void {
  const send = (globalThis as any).__steer;
  const severity = opts.severity ?? "";
  if (typeof send === "function") {
    send(pi, CUSTOM_TYPE, msg, severity === "error" ? msg : "", "steer", { ...opts });
    return;
  }
  const marked = severity ? `[${severity}] ${msg}` : msg;
  pi.sendUserMessage(marked, { deliverAs: "steer" });
}

/** One warning to the user (toast) and to the agent (steer). */
export async function warn(pi: any, ctx: any, msg: string): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  steerWarning(pi, msg);
}

/** A failure the flow reports: severity warning, and the agent's next turn is
 *  spent on it. */
export function steerWarning(pi: any, msg: string): void {
  deliver(pi, msg, { severity: "warning", triggerTurn: true });
}

/** A failure the USER has to act on — a dead AzDO token, a queue nothing can
 *  confirm. Severity error: it renders red instead of reading like chatter. */
export function steerError(pi: any, msg: string): void {
  deliver(pi, msg, { severity: "error", triggerTurn: true });
}

/** The started notice: it belongs in the agent's context but must not spend a
 *  model turn, so it goes out without triggerTurn. Absent the shared sender the
 *  notice is dropped, and the toast alone shows the build started. */
export function steerStarted(pi: any, msg: string): void {
  if (typeof (globalThis as any).__steer === "function") deliver(pi, msg, {});
}

/** A warning the user must see without spending a model turn: the build is
 *  already running and nothing in the flow is blocked by it (the pre-warm's VM
 *  layer needs attention, the publish gate still decides later). */
export function steerWatch(pi: any, msg: string): void {
  deliver(pi, msg, { severity: "warning" });
}

/** The watch's two report-only backstops. Neither stops or kills anything. */
export function watchMessage(event: RunEvent, id: string): string | null {
  if (event.kind === "stall") {
    return `${id} build looks stuck: no pane output and no CPU progress for ${minutes(event.silentMs)} minutes. It is still running and nothing was killed — /devexpress → "Abort build" stops it.`;
  }
  if (event.kind === "overrun") {
    return `${id} build has been running for ${minutes(event.elapsedMs)} minutes (a local build takes about ${minutes(event.limitMs)}). Still running, nothing killed.`;
  }
  return null;
}

/** A terminal message: a dead pane without a marker, or a nonzero exit. */
export function finishMessage(event: RunEvent, id: string, choice: string, latest: string, notes: string[], tailText: string): string {
  if (event.kind === "died") {
    return [
      `${id} build — ${choice}`,
      `DX latest: ${latest}`,
      ...notes,
      "Build FAILED — the build pane is gone and no exit code was written.",
      tail(tailText, 4000),
    ].join("\n");
  }
  const code = event.kind === "done" ? event.code : -1;
  const note = event.kind === "done" && event.note ? [event.note] : [];
  return failureResult(id, choice, latest, [...notes, ...note], { code, stdout: tailText });
}
