/**
 * reactive-xaf-build/report — the flow's messages and its half of the watch.
 *
 * Pure string building plus the delivery helpers: a warning pairs a toast with
 * a steer, the start notice rides the shared sender without spending a model
 * turn, and a failure names the exit code with a bounded tail. Extracted from
 * build.ts so the flow engine stays inside the file-size cap.
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

/** One warning to the user (toast) and to the agent (steer). */
export async function warn(pi: any, ctx: any, msg: string): Promise<void> {
  await ctx.ui.notify(msg, "warning");
  steerWarning(pi, msg);
}

/** Steers ride the shared sender when llm-utils is loaded (severity rendering,
 *  triggerTurn) and fall back to the runtime's message API, which is what this
 *  extension used before the shared sender existed. */
export function steerWarning(pi: any, msg: string): void {
  const send = (globalThis as any).__steer;
  if (typeof send === "function") {
    send(pi, "reactive-xaf-build:build", msg, "", "steer", { severity: "warning", triggerTurn: true });
    return;
  }
  pi.sendUserMessage(msg, { deliverAs: "steer" });
}

/** The started notice: it belongs in the agent's context but must not spend a
 *  model turn, so it goes out without triggerTurn. Absent the shared sender the
 *  user still has the toast, and a started build needs no agent action. */
export function steerStarted(pi: any, msg: string): void {
  const send = (globalThis as any).__steer;
  if (typeof send === "function") send(pi, "reactive-xaf-build:build", msg, "", "steer", {});
}

/** A warning the user must see without spending a model turn: the build is
 *  already running and nothing in the flow is blocked by it (the pre-warm's VM
 *  layer needs attention, the publish gate still decides later). */
export function steerWatch(pi: any, msg: string): void {
  const send = (globalThis as any).__steer;
  if (typeof send === "function") {
    send(pi, "reactive-xaf-build:build", msg, "", "steer", { severity: "warning" });
    return;
  }
  pi.sendUserMessage(msg, { deliverAs: "steer" });
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
