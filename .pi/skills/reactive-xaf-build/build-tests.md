---
name: reactive-xaf-build/build-tests
description: Behavior contract for the /devexpress workflow — build/commit/publish flows, the background watcher start, the status/cancel surface, the failure delivery and the VM gate — driven through pi's own runtime.
---

# build-tests.ts — the /devexpress workflow contract

Companion of `.pi/extensions/reactive-xaf-build/build-tests.ts`. The whole flow
runs against the extension's OWN injectable ports (`mkRunner`'s command runner,
the feed fetcher, `mkPaneSeams`' pane machinery and watcher starter, `propsPath`,
`repoRoot`, `pollMs`), so the real nuget.org, pwsh, psmux, Hyper-V VMs and git
are never touched by a case.

Run: `npx tsx .pi/extensions/reactive-xaf-build/build-tests.ts`

## Runtime (pi's own, no hand-written pi)

pi's OWN loader and ExtensionRunner, through the route this file carries itself,
in the shape `menu-tests.ts` established (commit `93e1a4754`):
`installRoute()` installs the resolver hook (`resolve.mjs`) and then imports the
harness and the seam-owning modules DYNAMICALLY by name — a static `@pi/...`
import would be linked before the hook exists — and
`buildRealRunner({ cwd, ui, activate })` builds a runner whose `activate` is the
real `registerBuildCommand`. A case drives it with
`handle.runCommand("devexpress", "")`: pi's own command dispatch, pi's own ctx,
the real prompt surface. Nothing spawns, and no case starts pi. The one
difference from `menu-tests.ts` is that this route carries no boot proof: that
file imports `boot-proof.js` and asserts the ledger's `boot.ok`, this one imports
neither.

The route is a per-suite copy by design (one batch per suite, each carrying the
route rather than sharing it in a helper). What this file's own projections hand
a case:

- `noticesOf(handle)` — the toasts the user was shown;
- `warningToasts(handle)` — the toasts delivered AS warnings;
- `steersOf(handle)` / `steersText(handle)` — the agent-facing deliveries read
  off `host.messages`, with `triggerTurn` from the index-aligned
  `messageOptions`;
- `noWarning(handle)` — no warning toast and no turn-forcing steer;
- `errorsOf(handle)` — the handler errors the runner REPORTED instead of
  throwing (a throwing handler is a no-op unless a case looks for it);
- `uiFor(answers, observe?)` — the case's own prompt stand-in.

The seams the extension reads at run time are the REAL modules' publications —
`globalThis.__steer` from llm-utils, `__writeFileSync` from tracked-write — never
a stub: `report.ts` steers through the former, `run.ts`/`pins.ts` write through
the latter.

## What a case pins (the delivered surface)

Only what is DELIVERED. pi discards a command handler's return value, so the
flow's report is asserted where the user meets it: the summary is notified, so
`published`, `publish stopped`, `committed: Publish`, `push aborted`,
`git status failed (exit 1)` and the milestone lines are read from the toasts; a
failure is the warning toast PLUS the steer that forces a turn; the started
notice is a steer with no turn.

Both refusals are DELIVERED now (2026-10-08): `build.ts` routes them through
`refuse`, which toasts the message as a warning before returning it. They used to
be return-only, so a wrong tree or a second build produced nothing on screen at
all — the case pinned the behavior and could never pin the message.

- the repo guard (`build.ts` `missingRepo`) — T2 asserts the toast names the
  tree, and that zero commands run, no pane opens and no run starts;
- the second-build refusal (`runBuildFlow`) — T21 asserts the toast names the
  running build, and that no second pane opens while the first run stays active.

Every case also fails on a prompt it did not map, or on an answer that is not
one of the offered options (`answered(sel, …)`) — that is what keeps a Build
case from silently driving the Publish branch. Prompts are keyed by a substring
of their title: `DevExpress` (the top menu), `Project`, the project pick itself
(`RX-XAF` / `eXpand`), the DX prompt (`DX … → …: update all DevExpress.* pins?`),
`Commit with message…` (the title spells counts and areas), `Publish: …?` and
`Dirty working tree before pushing to lab …`.

## Fixtures and helpers

`mkRepo` builds a temp checkout (props file, the profile's detect marker) with
`readFileSync`-readable `Directory.Packages.props`; `envOf` parses the generated
`run.ps1` (`runPaths` + `writeRunScript`) into env pairs; `markerOf`/`finishRun`
read and write the supervisor's exit marker the way the real supervisor's
`run.ps1` would; `mkRunner` scripts the command runner and records every call
plus its opts. The VM fixtures carry the probe's real CRLF shape
(`VM_OFF` = `C11=Off`, `VM_SAVED`, `VM_STARTING`, `VM_RUN`) and `VM_LF` keeps
the bare-LF shape a seam may hand back.

## Pinned behaviors

- T1-T2 — registration through the real index factory; outside the repo nothing
  runs.
- T3-T5 — DX update flows (happy path, already latest, mixed pins) reach the real
  prompt and write the real file; T4b repairs a stale `build.ps1`.
- T6 — build failure: the command returns on a started build, the watch reports
  FAILED with the exit code and the pane tail, the pane is kept, one warning
  steer forces a turn.
- T7 — Release runs `brx -Release` in the pane and `prx -Release` later; the
  eXpand push cases assert the `lab`-to-`master` push (`git push lab:master
  HEAD`, `px`) rather than the retired def-39 queue script.
- T8 — a user abort delivers nothing.
- T9-T13 — publish flows: VMs, commit, the pane-open fallback, close pane, and a
  Starting agent that is waited for instead of re-started.
- T14-T19 — the background watcher starts and every pick runs in the invoking
  window; the status pick parses the `STATUS=` line.
- T21-T30 — the run watch: started without a turn, a second build opens no second
  pane, one stall on silence plus idle CPU, the overrun never closes the pane, a
  dead pane reports, abort reports no failure, a corrupt marker is a failure, and
  the fallback path (shared sender absent) still lands.
- T31-T47 — the VM probe contract: two failed probes refuse the queue while the
  commit stands, a silent read is named silent, exactly the missed agent is
  named, `Paused` refuses while `Saved` starts, the pre-warm runs before the pane
  and never waits, and the CRLF read names every agent while a bare-LF read still
  parses. The labels are unique strings in this file — `T45: the CRLF read
  published`, `T45: one probe from a complete CRLF read, no warning`,
  `T45: no agent started from a complete CRLF read`, the two `T46:` labels and
  the single `T47:` label.
- T48-T53 — the commit summary (counts and areas, never a path), the push prompt
  (`Commit before push` / `Push as is` / `Abort`), a failed status read, and an
  abort that drops the VM outcome with it.
- T54-T56 — the run's build env: `profile.buildEnv` owns it, the template in
  `run.ts` adds no policy of its own, and the no-pane fallback is handed the same
  env through `RunOpts.env` on the command runner.
- T57 — a read that never answers cannot wedge the watch: with the CPU sampler,
  the pane capture and the pane probe all answering long past the tick's budget,
  the build's outcome still lands and the run stops (`run.md` has the design).

The watcher's own contract (toast per poll, terminal steer, give-up, replace) is
pinned by `watcher-tests.ts`; the CRLF status/cancel parse contract by
`azdo-tests.ts`; the profile picks and loud repo guard by `profile-tests.ts`; the
build.ps1 version bump by `release-tests.ts`. The CRLF read's own story — why the
split is on the line terminator — is in `publish.md`.

## Failure delivery (the contract that changed 2026-08-25)

That date's change (a `triggerTurn` steer that started no turn) is superseded by
the run watch: `report.ts` owns delivery now. `warn(pi, ctx, msg)` pairs a
warning toast with `steerWarning`, which rides `globalThis.__steer` (customType
`reactive-xaf-build:build`, severity warning, `triggerTurn: true`) and falls back
to `pi.sendUserMessage(msg, { deliverAs: "steer" })` when llm-utils is not
loaded. On the real route that fallback is what T28 reaches, by taking the
published seam away for that case and putting it back straight after; the
delivery is asserted from `host.messages` with `deliverAs` on
`messageOptions`. `steerStarted` sends the same customType with no turn asked
for, and `steerWatch` the same with severity warning and no toast — the started
notice and the pre-warm warning are the two steers a case must NOT read as
failures. The old `steerFailure` name is gone from the module.

## Write-gate conformance (2026-09-19, the VM-probe rework)

The VM-probe cases (T31-T44, then the CRLF read in T45-T47) and the sibling
fixture edits were re-derived against `test-runner/write-gate.md` — that doc's
CURRENT rule list is the index below. The numbering this section carried earlier
(a "rule 9", a "rule 10") is retired.

- rule 1 — every added block sits under a `// Section:` comment (T31-T47, the
  three CRLF sections, and T48-T53);
- rule 4 — every case drives the registered command through pi's own dispatch
  (`handle.runCommand`), no helper-only assertions; the CRLF cases are no
  exception — they run `/devexpress` Publish end to end with the probe injected
  as a seam;
- rule 6 — no assertion description repeats within this file or across the
  sibling suites: the added labels are unique strings (the three `T45:` labels,
  the two `T46:` labels, the `T47:` label), and `profile-tests.ts` /
  `release-tests.ts` only gained fixture entries, no new assertions;
- rule 7 — no assertion sits inside an iteration construct: the CRLF/LF choice is
  data (a fixture constant and a ternary), never a loop;
- rule 8 — the T48-T53 batch added no import either (it extends `mkRepo` with a
  marker argument and adds one VM fixture), so the import graph is intact;
- rules 2, 3 and 5 — untouched: no `test()` wrapper pattern beside `check()`, no
  pi spawn, no process-boundary mock enters these cases. The conversion this file
  went through (2026-10-08) keeps that: it carries its own resolver-first route,
  drives pi's real runtime and no longer holds a pi fake at all.

## What the harness cannot cover

- the extension's boot under pi's own loader (jiti plus the alias table): the
  suite builds through that loader with only the host stubbed, and no case
  spawns pi;
- UI rendering, and the real psmux/pwsh/Hyper-V/nuget.org: the case's own ports
  stand in, which is the point — nothing real is touched;
- T29 stays the one case that runs a real process (`runArgv` on a generated
  `run.ps1` under pwsh), because the supervisor's own exit-code contract is what
  it pins.

## Island

The suite spawns nothing, so it runs inside the test gate's island as it stands:
no boot proof is needed here, and none is wanted (the route is the per-suite
copy described under Runtime).
