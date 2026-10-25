---
name: reactive-xaf-build/menu
description: Use when changing the /devexpress command surface — the composition root that owns the command and drives the engine, the menu picks (Build / Publish / Build & Publish / Last build status / Cancel AzDO build / Start AzDO watcher → RX-XAF | eXpand → Lab | Release), Abort build, Close build pane.
---

# menu.ts — the /devexpress command surface

Companion of `.pi/extensions/reactive-xaf-build/menu.ts`.

The command takes no arguments: `handler(_args, ctx)` parses nothing and goes
straight to the menu. The old arg words (`status`, `cancel`, `watch`,
`build lab`, `publish lab`) were retired under the CLI Flag Gate — every
parameter is a pick or a prompt.

This module is the composition root. It imports the engine (`build.ts`) and
drives it through `createFlowRunners`; the engine imports no surface.
`index.ts` calls `registerBuildCommand(pi)` from here, and registers the agent's
tool (`tool.md`) and the shell fence (`fence.md`) beside it.

## Registration (`registerBuildCommand`)

Merges `defaultSeams()` with the injected seams, resolves the cwd
(`ctx.cwd` → `merged.repoRoot` → `process.cwd()`), builds this invocation's
`runFlow` / `startWatch` through `createFlowRunners`, and hands both to the
menu. Tests call it with fakes; the boot path calls it bare.

## Menu (`runDevexpressMenu`)

- Top: **DevExpress** → Build | Publish | Build & Publish | Last build status
  | Cancel AzDO build | Start AzDO watcher, plus ONE of:
  - **Abort build (`pane`, N min)** while a run is being watched
    (`abortLabel()` reads `activeBuildRun()`), or
  - **Close build pane** while a pane is open but nothing is running.
- Abort is the only deliberate kill: `abortFlow` calls
  `abortBuildRun(seams.closePane ?? defaultClosePane)`, clears the pane key
  and reports "no failure is reported" — a deliberate stop is not a build
  failure, so nothing steers. A run with no pane cannot be killed from here, so
  the message says the build runs on and only its report stops.
- "Last build status" runs `statusPhase` against `profile.statusDef` (RX def
  23 / expand def 94), and "Cancel AzDO build" runs `cancelPhase`
  project-wide.
- Every flow row picks **Project** → RX-XAF | eXpand, then Lab | Release, and
  "Start AzDO watcher" takes the same two picks. The pick is passed to the
  runner as `projectPick`; the engine switches `seams.profile` and
  `resolveRepo` finds that tree (cwd, then `C:/Work` and `D:/` known roots).
- The three flow rows are one table (`FLOW_ROWS`), so a label and the mode it
  means cannot drift apart:
  - **Build** → `"build"` — always builds; a green build already continues into
    the publish on its own (`build.ts` `buildRunReporter`).
  - **Publish** → `"publish"` — never builds (the old skip-build flow).
  - **Build & Publish** → `"buildIfNeeded"` — builds only when the last green
    build no longer covers the tree, then publishes on the same rails; a
    current stamp goes straight to the publish (`stamp.ts`, `build.md`).

  A row that maps to no mode answers "DevExpress menu: aborted." and nothing
  runs.
- `pickProjectAndChoice` is the shared two-pick helper for the three items
  that need both. A non-pick answer returns "DevExpress menu: aborted." and
  nothing runs.

Every pick runs in the invoking window.
