---
name: reactive-xaf-build/tool-tests
description: Use when changing the devexpress_build tool (start/status), the no-UI shim, or the in-process run state — the behavior contract that drives the registered tool through pi's runner and pins the started answer, the second-start refusal, the status tail, the headless start and the sanctioned path staying outside the fence.
---

# tool-tests.ts — the door's behavior contract

Companion of `.pi/extensions/reactive-xaf-build/tool-tests.ts`.

Run: `npx tsx .pi/extensions/reactive-xaf-build/tool-tests.ts`

Nothing spawns, and the real nuget.org, pwsh, psmux and VMs are never touched:
the extension's OWN seams (command runner, feed, pane machinery) stand in.

## The route

The same resolver-first route as every suite here: `installRoute()` installs
`./resolve.mjs`, then DYNAMIC-imports `real-runner`, `llm-utils` (publishes
`__steer`, the reporter's warning path) and `tracked-write` (publishes
`__writeFileSync`, the pin rewrite path). `activate` is the extension's REAL
factory (`./index.ts`), so the command, the tool and the FENCE are all
registered by the shipped boot.

The tool is called the way pi calls it —
`definition.execute(null, params, undefined, undefined, ctx)` — with a ctx that
carries a `cwd` and NO `ui`, which is the point of most cases.

## The contract

- **T1** a start answers `started:`, opens the pane seam once, sends one
  supervisor command, carries the run id and the pane, returns while
  `isBuildRunActive()` is still true, and raises NO prompt — the case passes a
  recording UI and asserts the list stayed empty, which is the headless
  behaviour the tool exists for.
- **T2** a second start answers `not started:` naming the running run id, opens
  no second pane and makes no second DX fetch.
- **T3** status names the run in flight and returns the captured pane tail.
- **T4** status with nothing watched says so, and says another window's build
  is invisible here.
- **T5** the sanctioned path is not behind the fence: the fence is live on the
  same host (`ctx_shell "brx"` is refused) and the tool's own start still runs.
- **T6** no pane available falls back in-process — the answer says so, the run
  is watched (`activeBuildRun().pane === null`), and a second start is still
  refused with its id.
- **T7** a genuinely required decision refuses instead of answering: pins
  26.1.0 against a 26.1.4 feed, no UI, so the start aborts at the pin prompt,
  no pane opens, `Directory.Packages.props` is untouched and no run is watched.
- **T8** aborting an in-process run returns that run, names no pane and stops
  the watch (the build itself cannot be killed from here).

## Traps this file paid for

- `hangBuilds` must hold ONLY the build command. Hanging every command made the
  VM pre-warm's probe never answer, so the flow blocked on a promise the event
  loop no longer had a timer for and Node exited 0 with no summary at all. A
  suite that dies silently is worse than one that fails, so the file also
  installs `uncaughtException` / `unhandledRejection` handlers that print the
  stack and `process.exit(1)`.
- The in-process case hands the flow a build promise that never settles on
  purpose: releasing it would run the reporter, and a green build continues into
  the publish phase.
- Every case ends with `resetRunState()` (`stopBuildRun` + `setBuildPane(null)`),
  because the run state and the pane id live on `globalThis` and would leak into
  the next case.
