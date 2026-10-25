---
name: reactive-xaf-build/fence-tests
description: Use when changing the shell fence's refusal set or its text reading — the behavior contract that fires tool_calls through pi's own dispatch and pins what is refused (pipeline words, scripts, pipeline solutions, wrappers) and what must still pass (single-project builds, the filtered test loop, an argument that only spells a word).
---

# fence-tests.ts — the refusal's behavior contract

Companion of `.pi/extensions/reactive-xaf-build/fence-tests.ts`.

Run: `npx tsx .pi/extensions/reactive-xaf-build/fence-tests.ts`

Nothing spawns: no pwsh, no psmux, no network.

## The route

`installRoute()` installs `./resolve.mjs` first (the `.js`→`.ts` map and the
`@pi/` name floor over the platform's `shared-utilities.json`), then
DYNAMIC-imports the harness by name, because a static import is linked before
the hook exists. `buildRealRunner({ activate })` builds one host whose
activation is the fence's own real entry (`registerBuildFence`) — the same call
`index.ts` makes at boot — and every case fires
`handle.emitToolCall({ type: "tool_call", toolName, input, toolCallId })`, pi's
OWN dispatch. `llm-utils` is imported so the boot publishes `__steer`, as a real
boot does; the fence reads no seam.

One host serves every case: the gate is stateless, and a single build already
proves the hook is live the moment the factory returned.

## The contract

- **T1** `ctx_shell "brx"` is refused, and the reason names `devexpress_build`,
  the pane and `AssemblyInfoVersion.cs`.
- **T2** the same command through `bash` is refused, and its reason is IDENTICAL
  to `ctx_shell`'s — the surface is wider than one tool and there is one policy
  behind it.
- **T3** `pwsh -NoLogo -Command "brx"` is refused: a wrapper is unwrapped, never
  trusted as the command's name. This is the incident's exact shape.
- **T4** `lean_ctx ["exec","brx"]` and a `ctx_call` wrapping `ctx_shell` are
  refused: the lean-ctx subcommand is consumed before the scan, and the wrapper
  re-reads its wrapped input.
- **T5** `dotnet build src\Modules\Reactive\Reactive.csproj` is not refused —
  the fast local loop.
- **T6** the three pipeline solutions UNNARROWED, the four build scripts and
  `cd src && brx` are refused; a NARROWED `dotnet test …Tests.sln` (the selection
  carried as an expression), a single-project test run and a redirected
  single-project build pass; a redirected pipeline build names the log in its
  reason.
- **T7** `echo "brx"`, `git log --oneline brx`, `cat
  Build/BuildPipeline.ps1`, `dotnet run` and `git log > out.txt` pass: an
  argument that spells a word is not a command position, and a redirect alone
  is not a refusal.
- **T8** a `write` call, an empty command and a command-less shell call are
  untouched.

## Loud failures

The file installs its own `uncaughtException` / `unhandledRejection` handlers and
a top-level `catch` that prints `FAIL fence-tests crashed: …`, because a suite
that ends in silence reads as 27 green — the trap `tool-tests.ts` hit first (a
pending await with no timer left, Node exit 0, no summary). The prologue is
copied per suite on purpose: every suite here carries its own route rather than a
shared helper.

## What it cannot cover

- a command assembled to evade text matching (`'b'+'rx'`, `Start-Process`) —
  named as a non-goal in `fence.md`;
- a source tree shadowing an extension module, which the resolver floor refuses
  at import time;
- windows in other projects: this extension loads per project, so the fence
  guards this repo's sessions only.
