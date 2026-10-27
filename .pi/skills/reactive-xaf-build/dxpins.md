---
name: reactive-xaf-build/dxpins
description: Use when changing the DevExpress pin questions — which version nuget.org offers, what the props file pins say, how those pins are rewritten, and how build.ps1's -version is aligned.
---

# dxpins.ts — the DevExpress pin questions

Companion of `.pi/extensions/reactive-xaf-build/dxpins.ts`, split out of
`build.ts` when the engine reached the 400-line cap. Pure text and feed readers:
no prompt, no write, no pane. The flow decides WHEN a rewrite lands (`dxPhase`
in `build.ts`), so every function here can be exercised on a string.

## `getLatestDx(fetchFeed)`

The newest STABLE `DevExpress.ExpressApp` on nuget.org: one request to the
flat-container index (`DX_FEED_URL`), and the versions kept only when they match
`^\d+\.\d+\.\d+$` — a `-preview` or `-beta` entry must never become the target,
which is what "stable" means here. The survivors are sorted by
`compareVersions` and the last one wins. An index with no stable version throws
instead of answering something plausible: the flow's answer to that is to abort
the phase, and a silent fallback would move the pins to nothing.

## `readDxPins(text)` → `{ count, unique }`

`DX_PIN_RE` matches only `Include="DevExpress.…" Version="…"` pairs, so the
Xpand pins in the same props file are somebody else's question (`depPins`,
`profile.md`) and stay untouched. `count` is how many DISTINCT versions the file
carries and `unique` is that version when they all agree; mixed pins answer
`null`, and the flow leaves such a file alone — that is the one case where
nothing is asked and nothing is rewritten.

## `rewriteDxVersion(text, newVersion)` / `rewriteBuildVersion(text, version)`

Two targeted replacements, no parsing and no re-serialization, so the props file
keeps its formatting and its comments:

- every `DevExpress.*` pin moves to `newVersion`;
- `build.ps1`'s `-version "X.Y.Z.W"` moves to the target the flow computed
  (`dxBaseVersion` on Lab, the next release on Release). It only matches the
  four-part form, so a different flag spelling is left visibly untouched rather
  than half-rewritten.
