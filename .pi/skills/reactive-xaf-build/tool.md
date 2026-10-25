---
name: reactive-xaf-build/tool
description: Use when an agent needs to start or inspect the Reactive.XAF/eXpand build — the devexpress_build tool (actions start and status), why it needs no UI, how a second start answers with the run already in flight, and what it cannot see (another window's build).
---

# tool.ts — the agent's door to the build

Companion of `.pi/extensions/reactive-xaf-build/tool.ts`.

`/devexpress` is a menu, and an agent cannot type a slash command. With no door,
an agent in this repo ran `pwsh -NoLogo -Command "brx" > /tmp/brx.log 2>&1`,
held the call 227 s and grepped a `Build FAILED` out of the log — while the
extension already owned a pane, an exit marker and a watch that would have
reported the same failure unprompted.

This module is a SURFACE, not a second engine: `run.ts` owns the run's
lifecycle, and `menu.ts` and `tool.ts` both drive `runFlow` through
`createFlowRunners`. `5af5e1424`'s rule — the lifecycle belongs to the run, the
surface to the menu — still holds; the tool is a second surface.

## The tool

`devexpress_build`, registered from `index.ts`'s factory
(`registerBuildTool(pi, seams?)`, so a suite activates the real thing with its
own ports).

| Param | Values | Default |
|---|---|---|
| `action` | `start` \| `status` | required |
| `choice` | `Lab` \| `Release` | `Lab` |
| `project` | `RX-XAF` \| `eXpand` | `RX-XAF` |

- **start** — refuses (as text) when a run is already watched, naming its run
  id, its pane and its age: two builds in this repo collide on `obj/`, `bin/`,
  `Build/nuspec/*` and `src/Common/AssemblyInfoVersion.cs`. Otherwise it drives
  the flow and answers `started: run <id>, pane <id>` (or `in-process …`),
  followed by the flow's own message. It returns as soon as the pane has the
  command — never at the end of the build.
- **status** — the run in flight (id, age, pane) plus
  `(seams.capturePane ?? defaultCapturePane)(getBuildPane())`, the pane tail.
  Nothing watched: it says so, and says that another window's build is
  invisible here, because the pane and the run live in that process.

## The flow it drives

`start` calls `runners.runFlow(choice, "build", project)` — the FULL flow, never
the `"buildIfNeeded"` decision the menu's Build & Publish row makes. A caller
that asked for a build gets a build; the green-build stamp (`stamp.ts`) is the
menu's convenience, not the door's. `FlowRunner` is
`(choice, mode?, projectPick?)`, so the mode is named at every call site
instead of a boolean whose meaning lived in the reader's head.

## No UI is required

Worker and headless windows are exactly where a blind shell does the most
damage, so the flow runs behind `uiFreeCtx`: a notice is forwarded when a UI
exists and dropped when it does not; a PROMPT throws instead of answering. The
flows read a NON-answer as consent on two paths — `dxPhase` rewrites
`Directory.Packages.props` on anything but Skip/Abort, and `pushDirtyPhase`
pushes the tree as-is on anything but Abort/Commit — so a missing UI must never
return `undefined` into them. The throw surfaces as the build's own outcome
(`… build aborted: the build needs a decision (…)`), and nothing is written.

## Envelope

The answer is built inline as `{ content: [{ type: "text", text }] }`:
`pi-dev/tool-result.ts` is the canonical seam, but it lives in the home tree and
this project extension imports no `@pi/` module at load time — a load-time
dependency there would take the whole extension down.
