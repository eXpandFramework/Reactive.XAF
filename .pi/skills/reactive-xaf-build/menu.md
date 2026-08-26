---
name: reactive-xaf-build/menu
description: Use when changing the /devexpress command surface — menu picks (Build / Publish → RX-XAF | eXpand → Lab | Release), Last build status, Cancel AzDO build, Close build pane, and direct args.
---

# menu.ts — the /devexpress command surface

Companion of `.pi/extensions/reactive-xaf-build/menu.ts`.

## Entry (`runDevexpressMenu`)

Args are split on whitespace and matched in order:

| Args | Behavior |
|---|---|
| `status` | One-shot AzDO status (`profile.statusDef`). |
| `cancel` | PATCH-cancel the newest running build. |
| `watch` | Start the chain watcher (handled in build.ts). |
| `build lab` / `build release` | Full flow on the current profile. |
| `publish lab` / `publish release` | Skip-build flow on the current profile. |
| none / anything else | Interactive menu (menuFlow). |

## Menu (`menuFlow`)

- Top: **DevExpress** → Build | Publish | Last build status | Cancel AzDO
  build (+ Close build pane while a pane is open).
- Build and Publish always pick **Project** → RX-XAF | eXpand, then Lab |
  Release. The pick is passed to `runFlow` as `projectPick`; build.ts
  switches `seams.profile` and `resolveRepo` finds that tree (cwd, then
  `C:/Work` and `D:/` known roots).
- Direct args stay on the current profile (no project pick).

Every pick runs in the invoking window.
