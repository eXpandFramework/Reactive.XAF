/**
 * steer-repro — TEMP diagnostic extension (removed after the triggerTurn
 * investigation). A command that fires a triggerTurn steer from the
 * command-handler path — reproduces the "delivered as custom_message but
 * no turn starts" bug (reactive-xaf-build failure steers, 2026-08-25).
 *
 * Usage: pi child with the prompt "/steer-repro" — if the trigger works,
 * an assistant turn follows the steer; if the bug reproduces, the child
 * goes idle after the custom message.
 */

let _pi: any = null;

export default function (pi: any): void {
  _pi = pi;
  pi.registerCommand({
    invocationName: "steer-repro",
    description: "fire a triggerTurn steer (diagnostic)",
    handler: async (_args: string, _ctx: any) => {
      (globalThis as any).__steer(_pi, "steer-repro:test", "triggerTurn repro — a turn must start now", "", "steer", { triggerTurn: true });
    },
  });
}
