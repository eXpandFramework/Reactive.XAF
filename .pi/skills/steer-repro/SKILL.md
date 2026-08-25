---
name: steer-repro
description: Use when reproducing the triggerTurn delivery bug. TEMP diagnostic extension (removed after the investigation) — a command that fires a triggerTurn steer from the command-handler path.
---

# steer-repro — triggerTurn diagnostic

Temporary extension in `.pi/extensions/steer-repro/` (repo-local). The
`/steer-repro` command fires `__steer(..., { triggerTurn: true })` from a
command handler — the exact path the reactive-xaf-build failure steer uses.
Reproduction: spawn a pi child with the prompt `/steer-repro` and observe
whether an assistant turn follows the custom message.

Deleted after the investigation — do not rely on it.
