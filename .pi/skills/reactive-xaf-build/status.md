---
name: reactive-xaf-build/status
description: Use when changing the one-shot AzDO status and cancel surface — statusPhase (newest build state + fail reason + link + the recorded GitHub release outcome) and cancelPhase (PATCH-cancel). Definition from profile.statusDef.
---

# status.ts — AzDO status + cancel one-shots

Companion of `.pi/extensions/reactive-xaf-build/status.ts`. Both commands
run one pwsh spawn via the run seam and notify the outcome in the invoking
window. Info only — no steering.

## `statusPhase(ctx, seams)` — `/devexpress status`

Runs `azdoStatusScript(definition)` (azdo.md) and switches on `parsePoll`, the
typed answer (`empty` / `build` / `error`) — never a guess from prose.
`definition` defaults to `profile.statusDef("Lab")` (RX: 23; expand: 94). The
AzDO link reflects the definition.

- running → id + state + AzDO link
- failed → id + `extractFailReason` + link
- canceled / succeeded / none → the plain outcome
- error `auth` → the token is expired or refused, with the renewal instruction
- error `transient` → the message plus "the pipeline may be fine, retry"
- no `STATUS=` line at all → the stderr (a crashed script, not a refusal)
- and then the RELEASE line, read by `releaseLine(seams)` from `lastRelease`
  (`release-store.ts`) and appended to the same notice — `GitHub release
  <version> <status>` with its detail. A profile with no row says so in words
  (`no GitHub release attempt is recorded yet`) instead of printing nothing, so
  "did the release happen?" is answered here rather than by opening GitHub. A
  row whose version is EMPTY is the release that could not even start, and it
  reads `GitHub release: not attempted — <reason>` — never a blank version,
  which would look pending. One notify, because two same-type notifies back to
  back lose the first.

## `cancelPhase(ctx, seams)` — `/devexpress cancel`

Runs `cancelAzDoScript()` — PATCH `{"status":"cancelling"}` on every
running/queued build.

- `ok` → "Cancel requested for AzDO build <id>"
- `notrunning` → id + current status, nothing to cancel
- `0;none;none` → no builds found
- `CANCEL=ERR;<auth|transient>;<msg>` → a refused or failed cancel, reported as
  such instead of reading as "nothing to cancel"
