---
name: reactive-xaf-build/menu
description: The /devexpress command surface — menu picks (Build → RX-XAF → Lab | Release, Publish → RX-XAF → Lab | Release, Last build status, Cancel AzDO build, Close build pane) and direct args (status | cancel | watch | build lab|release | publish lab|release).
---

# menu.ts — the /devexpress command surface

Companion of `.pi/extensions/reactive-xaf-build/menu.ts`.

## Entry (`runDevexpressMenu`)

Args are split on whitespace and matched in order:

| Args | Behavior |
|---|---|
| `status` / `status release` | One-shot AzDO status — Lab def 23 by default, `release` queries def 39 (statusPhase). |
| `cancel` / `cancel release` | PATCH-cancel the newest running build — def 23 by default, `release` targets def 39 (cancelPhase). |
| `watch` / `watch release` | Start the chain watcher for the current build (no publish flow); `release` walks the def-39 chain. |
| `build lab` / `build release` | Full flow: `runFlow("Lab"|"Release", false)`. |
| `publish lab` / `publish release` | Skip-build flow: `runFlow("Lab"|"Release", true)`. |
| none / anything else | Interactive menu (menuFlow). |

The flow runner type is `(choice: string, skipBuild?: boolean) => Promise<string>`
— `build.ts`'s `runBuildFlow` closure.

## Menu (`menuFlow`)

- Top: **DevExpress** → Build | Publish | Last build status | Cancel AzDO
  build (+ Close build pane while a pane is open). Cancel runs `cancelPhase`
  in this window (no delegation).
- Build → **RX-XAF** → Lab | Release — full flow, run in this window.
- Publish → **RX-XAF** → Lab | Release — skip-build flow
  (`runFlow(choice, true)`), run in this window.

## In-window execution

Every menu pick runs in the invoking window — the build pane splits it to the right (pane.ts), with milestones notified there.

## Guards

- `repoRootOf(cwd)` (build.ts) rejects invocation outside the Reactive.XAF
  repo before anything runs.
- User aborts (menu returns early) never reach the flow engine.
