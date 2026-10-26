---
name: reactive-xaf-build/test-watch
description: Use when the failing-test report or its delegation offer behaves wrong — test-watch.ts owns what the watcher says about a failing AzDO test (one report per name, one delegation prompt per build) and opens the Test-Runner window. Read before changing the prompt, the brief, or the role lookup.
---

# test-watch.ts — a failing test's voice and its one offer

Companion of `.pi/extensions/reactive-xaf-build/test-watch.ts`. Split out of
`watcher.ts` and `watcher-notice.ts`, both near the 400-line cap: every other
voice in this extension is one line about a build, while this one owns per-build
state and can open a window.

## What the operator sees

- First failing test → one warning toast PLUS one steer: the build id, the build
  URL and the test names. `urgent` spends the agent's next turn.
- The same test on later polls → nothing.
- A NEW name later → one more steer naming only the new name. Never a second
  prompt.
- ONE prompt per build: `Delegate to Test-Runner` | `Keep watching` |
  `Don't ask again this run`. The third answer silences the ASKING for the rest
  of the run; the steer lines continue.

A failure retried into a pass is NOT retracted — the poll saw what it saw, and
the chain's own verdict still reports the truth.

## Every poll says what it looked at

`TestWatch.runs` and `TestWatch.failedRuns` record the counts the scan printed
(`TESTRUNS=<runs>;<failedRuns>`), so an empty answer is falsifiable: "the build's
runs carried no failure" is a different statement from "the scan reached
nothing". Both stay `null` until a readable scan answers, and a refused scan
leaves them `null` rather than a fabricated zero — build 36120's silence was
once read as a verdict the scan never made.

## The prompt never holds the chain

`reportTestFails` returns as soon as the steer is out and offers the delegation
detached (`void offerDelegate(...)`). An unanswered prompt must not stop the
polling that still has to deliver the terminal verdict. `pending` guards one open
prompt; `asked` guards one prompt per build.

## The delegation

`roleLaunch(role, store?)` lives in `delegate.ts` — that module owns the launch —
and reads the model-role store off `globalThis.__pi_model_roles` at call time, so
a role assigned while a build runs is picked up by the next offer. An entry with
no `provider` or no `modelId` resolves to NOTHING: an invented model would spawn
a window on whatever the caller guessed.

`startDelegate` reports every refusal loudly, in this order:

- the chain already ended → an info line, nothing spawned;
- no such role → a warning plus a steer naming it;
- `spawn` throws, or answers `null` → "no delegate window could be opened" plus
  the exact `pi --provider … --model … '<brief>'` line to run by hand. A
  delegation that did not happen is never reported as one.

`delegateBrief` names the build, the URL and the failing tests, then the narrowed
run — `dotnet test src\Tests\Tests.sln --settings Build\Tests.runsettings
--filter "FullyQualifiedName~<name>|…"`, with `TMP`/`TEMP` at `C:\temp` because
MSBuild reads an inherited `b:\Temp` and dies with MSB1025. The whole suite is
never the answer to one failing test (AGENTS.md).

## Wiring

`watcher.ts` `pollTests` hands over PORTS — a shell runner and a window spawner
(`AzDoWatcherOptions.spawnWindow`, defaulting in the watcher to
`defaultDelegateWindow`) — and `pollTestFails` builds the whole scan, including
the script. `test-watch.ts` therefore depends on no concrete module of this
extension except the role lookup in `delegate.ts`. Options: `watchTests` (default
on), `testRole` (default `DEFAULT_TEST_ROLE` = `Test-Runner`), `spawnWindow`.

## Contracts

`watcher-tests.ts` W40–W58: reported once, new names only, one prompt, the role
launch and brief, the launch following the PUBLISHED store (W50) and a role
entry with no model resolving to nothing (W52), the command to run by hand, the
chain surviving, the watermark, muting, and the counts an empty poll leaves
behind (W58). Roles are published through model-role's own `initModelRoles`,
never hand-assigned. `delegate-tests.ts` S4/S5 pin the role lookup out of that
store and the quote doubling the brief relies on.
