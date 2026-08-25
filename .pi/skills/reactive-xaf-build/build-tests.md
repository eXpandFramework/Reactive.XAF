---
name: reactive-xaf-build/build-tests
description: Behavior contract for the /devexpress workflow — build/commit/publish flows, the background watcher start, the status/cancel surface, and the failure delivery.
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
- T7 — Release flow: `brx -Release` in the pane, `prx -Release` runs (stage,
  force-push `lab:master`, queue def 23 on master — prx knows the right
  pipe), published.
- T8 — user aborts never deliver.
- T9-T13 — publish flows: VMs, commit, pane fallback, close pane, Starting
  VM wait.
- T14-T15 — publish starts the background watcher (seam) and returns
  immediately — no blocking monitor, no failure steer from the flow (the
  watcher steers at the end; its contract lives in watcher-tests.ts).
- T16 — `/devexpress status` parses the STATUS= line (id + reason + link).
- T18-T19 — menu picks run in the invoking window (status in-window; the
  Lab pick opens the build pane and publishes here).

The watcher's own contract (toast per poll, terminal steer, give-up,
replace) is pinned by `watcher-tests.ts`; the CRLF status/cancel parse
contract by `azdo-tests.ts`.

## Failure delivery (the contract that changed 2026-08-25)

`steerFailure` delivers via `pi.sendUserMessage(msg, { deliverAs: "steer" })`
— a triggered turn, so the failure always lands in the agent's context (the
triggerTurn steer was delivered but started no turn in long-lived sessions).
The harness records `sendUserMessage` per mock pi (`_userMessages`); the
assertions pin the delivery, not the steer type.
