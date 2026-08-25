/**
 * steer-repro — TEMP diagnostic extension (removed after the triggerTurn
 * investigation). A command that fires the failure delivery via
 * pi.sendUserMessage — the "always triggers a turn" path — to validate the
 * candidate fix for the no-turn delivery bug (reactive-xaf-build failure
 * steers, 2026-08-25).
 *
 * Usage: pi child with the prompt "/steer-repro" — if the delivery works,
 * an assistant turn follows; the child must not go idle.
 */

let _pi: any = null;

export default function (pi: any): void {
  _pi = pi;
  pi.registerCommand({
    invocationName: "steer-repro",
    description: "fire a failure delivery via sendUserMessage (diagnostic)",
    handler: async (_args: string, _ctx: any) => {
      _pi.sendUserMessage("build-failed repro — a turn must start now", { deliverAs: "steer" });
    },
  });
}
