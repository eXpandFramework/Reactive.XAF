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
- T6 — build failure: the command returns on a started build, then the watch
  reports FAILED with the exit code and the pane tail (pane kept, one warning
  steer).
- T7 — Release flow: `brx -Release` in the pane, `prx -Release` runs (stage,
  force-push `lab:master`, queue def 23 on master — prx knows the right
  pipe; the def-39 queue script was removed 2026-08-25), published.
- T8 — user aborts never deliver.
- T9-T13 — publish flows: VMs, commit, pane fallback, close pane, Starting
  VM wait. Each build flow returns on a started build and continues from the
  watch once the test writes the exit marker.
- T14-T15 — publish starts the background watcher (seam) and returns
  immediately — no blocking monitor, no failure steer from the flow (the
  watcher steers at the end; its contract lives in watcher-tests.ts).
- T16 — the "Last build status" menu item (the retired `/devexpress status`
  word) parses the STATUS= line (id + reason + link).
- T18-T19 — menu picks run in the invoking window (status in-window; the
  Lab pick opens the build pane and publishes here from the watch).
- T21-T30 — the run watch: the command returns before the build ends, a
  failed build steers once with the code and tail, a dead pane reports
  instead of waiting, the stall fires once on silence plus CPU idleness, the
  overrun never closes the pane, abort reports no failure, a second build is
  refused, the supervisor's real exit code survives an `exit`, and a green
  marker publishes.
- T31-T47 — the VM probe contract: a probe that exits nonzero twice refuses
  the QUEUE with the commit already standing, and steers the exit code plus
  stderr (T31);
  a silent exit-0 read is probed twice, is named as silent instead of as an
  empty agent list, and still names all four agents (T32); a partial list names
  exactly the agent it missed (T33), and the killed-mid-list shape (nonzero
  code, truncated stdout) refuses on the code (T41); `Paused` is refused,
  `Saved` is started and publishes (T34-T35); a build start pre-warms the
  agents before the pane opens and never waits (T36), blind-starts all four
  when the probe is unreadable and steers a no-turn warning (T37), a failed
  Start-VM stays a note plus that warning (T38); the publish-only flow probes
  once (T39); an agent that never boots fails at the timeout (T40); a probe
  that answers the retry publishes and asked for a profile-free invocation
  (T42); an exit-0 read that listed nobody is probed twice and its own stderr
  reaches the refusal (T43), and the same shape that answers on that retry
  publishes (T44). The probe fixtures carry the real seam shape (CRLF, the
  last line terminated too; the read is split on its line terminator, see
  `publish.md`; `VM_LF` keeps the bare-LF one): a CRLF read names every
  agent, probes once and publishes without a Start-VM (T45); a CRLF read that
  names C11 `Off` starts exactly C11 (T46); a bare-LF read still parses (T47).

- T48-T53 — the commit summary and the pre-push check: the agents are already
  booting when the commit prompt is answered, and the prompt carries counts and
  areas, never a path (T48); a dirty tree at the push is asked about with that
  summary, and "Commit before push" commits then pushes then queues (T49);
  "Push as is" pushes the tree untouched (T50); "Abort" stops before the push
  AND the queue and is named in the report without a warning (T51); a
  `git status` that failed refuses instead of reading as clean (T52); aborting
  the commit drops the VM outcome with it (T53).

The watcher's own contract (toast per poll, terminal steer, give-up,
replace) is pinned by `watcher-tests.ts`; the CRLF status/cancel parse
contract by `azdo-tests.ts`.

## Failure delivery (the contract that changed 2026-08-25)

That date's change (a `triggerTurn` steer that started no turn) is superseded
by the run watch: `report.ts` owns delivery now. `warn(pi, ctx, msg)` pairs a
warning toast with `steerWarning`, which rides `globalThis.__steer` (customType
`reactive-xaf-build:build`, severity warning, `triggerTurn: true`) and falls
back to `pi.sendUserMessage(msg, { deliverAs: "steer" })` when llm-utils is
not loaded. The harness installs a capturing `__steer` (the `steers` array,
with `warnings()` filtering on severity), and T28 pins the fallback path with
`__steer` deleted — there the mock pi's `_userMessages` records the delivery.
The old `steerFailure` name is gone from the module.

## Write-gate conformance (2026-09-19, the VM-probe rework)

The VM-probe cases (T31-T44, then the CRLF read in T45-T47) and the sibling
fixture edits were re-derived against `test-runner/write-gate.md` — that
doc's CURRENT eight-rule list is the index below. The numbering this section
carried earlier (a "rule 9", a "rule 10") is retired: the gate file holds
eight rules, and the checks those lines named are rules 7 and 8 here.

- rule 1 — every added block sits under a `// Section:` comment (T31-T44, the
  three CRLF sections, and T48-T53);
- rule 4 — every case drives the registered command handler through the
  mock-pi harness (`activate(pi)` / `registerBuildCommand`), no helper-only
  assertions; the CRLF cases are no exception — they run `/devexpress`
  Publish end to end with the probe injected as a seam;
- rule 6 — no assertion description repeats within this file or across the
  sibling suites: the added labels are unique strings (`T45: the CRLF read
  published`, `T45: one probe from a complete CRLF read, no warning`,
  `T45: no agent started from a complete CRLF read`, the two `T46:` labels,
  the `T47:` label), and `profile-tests.ts` / `release-tests.ts` only gained
  fixture entries, no new assertions;
- rule 7 — no assertion sits inside an iteration construct: the CRLF/LF
  choice is data (a fixture constant and a ternary), never a loop;
- rule 8 — the T48-T53 batch added no import either (it extends `mkRepo` with
  a marker argument and adds one VM fixture), so the typebox-free import graph
  is intact;
- rules 2, 3 and 5 — untouched: no `test()` wrapper pattern beside `check()`,
  no pi spawn, no process-boundary mock enters these cases.
