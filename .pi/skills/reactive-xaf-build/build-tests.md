---
name: reactive-xaf-build/build-tests
description: Behavior contract for the /devexpress workflow — build/commit/publish flows, the AzDO monitor + status surface, and the failure delivery. Read when changing build.ts, azdo.ts, or the mock-pi harness.
---

# build-tests.ts — the /devexpress workflow contract

Companion of `.pi/extensions/reactive-xaf-build/build-tests.ts`. Mock-pi
harness with injected seams (fake command runner, feed fetcher, pane seams,
fixture props) — the real nuget.org, pwsh, psmux, Hyper-V VMs and git are
never touched.

Run: `npx tsx d:/Reactive.XAF/.pi/extensions/reactive-xaf-build/build-tests.ts`

## Pinned behaviors

- T1-T2 — the command registers through the real index boot; loud error
  outside the repo.
- T3-T5 — DX update flows (happy path, already-latest, mixed pins).
- T6 — build failure: FAILED surfaced, pane kept, failure delivery fired.
- T7-T8 — Release flow; user aborts never deliver.
- T9-T13 — publish flows: VMs, commit, pane fallback, close pane, Starting
  VM wait.
- T14-T17 — AzDO monitor + status (fake seams): FAILED delivers the reason,
  success/canceled are neutral, `/devexpress status` parses the STATUS=
  line.
- T18-T19 — menu delegation.
- T20 — fail-reason extraction: wrapper noise filtered, real error wins.

## Failure delivery (the contract that changed 2026-08-25)

`steerFailure` delivers via `pi.sendUserMessage(msg, { deliverAs: "steer" })`
— a triggered turn, so the failure always lands in the agent's context (the
triggerTurn steer was delivered but started no turn in long-lived sessions).
The harness records `sendUserMessage` per mock pi (`_userMessages`); the
assertions pin the delivery, not the steer type.
