---
name: reactive-xaf-build/pane-tests
description: Use when the build pane lands wrong or takes the focus away, or when its tests change — pane-tests.ts drives the REAL defaultOpenBuildPane against a real psmux server on an isolated socket and pins that a detached split leaves the invoking pane active, with the plain split as the control. Read before editing the pane placement or these tests.
---

# pane-tests.ts — the build pane's placement contract

Companion of `.pi/extensions/reactive-xaf-build/pane-tests.ts`.

The contract is the one the user feels: starting a build puts a live output pane
to the RIGHT of the invoking pane and leaves the invoking pane at the keyboard.
A plain `split-window` makes the new pane active, so the user's next keystroke
lands in the build's output instead of their prompt; `-d` is what prevents it.

This is the only suite here that measures a REAL psmux server. The other suites
inject every pane seam, which is why none of them could see the placement.

## Route

`installRoute()` installs the resolver hook (`./resolve.mjs`) first, then imports
BY NAME: `buildRealRunner` (`@pi/pi-dev/real-runner.js`), the two `globalThis`
publications the extension reads (`llm-utils` → `__steer`, `tracked-write` →
`__writeFileSync`), and `withIsolatedPsmuxServer`
(`@pi/psmux/psmux-test-harness.js`). The harness owns a unique `PSMUX_SOCKET`,
prefixes the binary and `-L` exactly once, and strips the live attach env, so
nothing here can reach the live server. `TMUX_PANE` is set to the isolated
pane for the case and restored in a `finally`.

## Contracts

- **P0** — the extension boots on pi's own runtime (`buildRealRunner`) and
  registers `/devexpress`, with no handler errors.
- **P1** — on a real isolated server: `defaultOpenBuildPane` returns a pane,
  the session's ACTIVE pane is still the caller's, the new pane is listed beside
  it, and sending it the run command through `defaultRunInPane` does not move
  the view either.
- **P1 control** — the same `split-window` without `-d` moves the active pane on
  that same server. Without this half the assertion above could pass while
  measuring nothing.

A bare shell in a detached session exits without a TTY and takes the session
with it, so the case holds the pane with the production send path before listing
it. `capture-pane` is not usable on an isolated socket; the active pane and the
pane listing are the working surfaces.

## Why the socket seam changed

`psmuxArgs` inserts the socket AFTER the binary (`psmux -L <sock> <command…>`)
when `PSMUX_SOCKET` is set, and returns the argv untouched otherwise. Before, it
prepended the socket in front of the whole argv, so `runArgv` spawned `-L` as
the executable (`spawn -L ENOENT`). Production never set the socket, so only
this suite could see it — P1 is what caught it.

Run: `npx tsx .pi/extensions/reactive-xaf-build/pane-tests.ts`
