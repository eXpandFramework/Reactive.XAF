---
name: reactive-xaf-build/profile-tests
description: Behavior contract for RepoProfile — RX default vs expandProfile driven through registerBuildCommand (mock pi). Detect, cmds, menu pick, status def, git push then px.
---

# profile-tests.ts

Companion of `.pi/extensions/reactive-xaf-build/profile-tests.ts`.

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/profile-tests.ts`

- P0 — index still registers `/devexpress`.
- P1 — RX detect rejects an expand-shaped tree (loud, zero commands).
- P2 — expandProfile: menu skips RX-XAF, `bx lab` in the pane, `git push lab` then `px`, published.
- P3 — expand status queries def 94; RX status still queries def 23.
- P4 — default profile still sends `brx` and `prx`.
