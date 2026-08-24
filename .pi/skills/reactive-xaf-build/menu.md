---
name: reactive-xaf-build/menu
description: The /devexpress command surface — menu picks (Build → RX-XAF → Lab | Release | Lab (skip build) | Release (skip build), Last build status, Close build pane) and direct args (status | build lab|release | publish lab|release). Read when changing or invoking the /devexpress menu, delegation, or task strings.
---

# menu.ts — the /devexpress command surface

Companion of `.pi/extensions/reactive-xaf-build/menu.ts`.

## Entry (`runDevexpressMenu`)

Args are split on whitespace and matched in order:

| Args | Behavior |
|---|---|
| `status` | One-shot AzDO status (statusPhase). |
| `build lab` / `build release` | Full flow: `runFlow("Lab"|"Release", false)`. |
| `publish lab` / `publish release` | Skip-build flow: `runFlow("Lab"|"Release", true)`. |
| none / anything else | Interactive menu (menuFlow). |

The flow runner type is `(choice: string, skipBuild?: boolean) => Promise<string>`
— `build.ts`'s `runBuildFlow` closure.

## Menu (`menuFlow`)

- Top: **DevExpress** → Build | Last build status (+ Close build pane while a
  pane is open).
- Build → **RX-XAF** → Lab | Release | **Lab (skip build)** | **Release (skip
  build)**.
- A pick containing `"skip build"` maps to `runFlow(choice, true)` and uses the
  `publishTask` delegation text; plain picks use `buildTask`.

## Delegation (`delegateOrRun`)

Every pick delegates to a NEW psmux window via `delegateWindow` (default:
`defaultDelegateWindow` from delegate.ts — spawns `psmux new-window ... pwsh
-c "pi '<task>'"`, so the task text must contain NO single quotes). The
delegated window runs the corresponding direct-arg command. On failure to
spawn, the flow falls back to running in the invoking session with a warning
notify.

Task strings (menu.ts):

- `buildTask(choice)` — "Run the /devexpress build lab|release command now..."
  (DX update, commit and publish confirmations appear in the delegated window).
- `publishTask(choice)` — "Run the /devexpress publish lab|release command
  now..." (commit and publish confirmations; AzDO monitor included).

## Guards

- `repoRootOf(cwd)` (build.ts) rejects invocation outside the Reactive.XAF
  repo before anything runs.
- User aborts (menu returns early) never reach the flow engine.
