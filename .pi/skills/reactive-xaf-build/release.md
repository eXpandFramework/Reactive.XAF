---
name: reactive-xaf-build/release
description: Use when changing the Release version bump — dxBaseVersion (DX → eXpand base) and releaseVersionTarget (feed consultation, next release version).
---

# release.ts — Release version bump

Companion of `.pi/extensions/reactive-xaf-build/release.ts`. Owns the
build.ps1 version derivation for the Release flow.

- `dxBaseVersion(dx)` — the eXpand base for a DX version: 26.1.4 → 26.1.400.0.
- `releaseVersionTarget(seams, dx, choice)` — Lab: the DX base. Release:
  consults the last published `profile.nugetId` on the Xpand server and
  nuget.org and returns the next release (same DX minor, build + 1,
  revision 0 — 26.1.400 → 26.1.401.0). A failed consultation throws; the
  flow aborts instead of silently falling back to a colliding version.

Tests: `release-tests.ts` / `release-tests.md`.
