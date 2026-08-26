---
name: reactive-xaf-build/profile
description: Use when changing RepoProfile — RX default plus expand. Detect, cmds, chain, version file, nuget id, GitHub, optional depPins. Engine never branches on repo id. Menu pick RX-XAF | eXpand uses profileByPick + resolveRepo.
---

# profile.ts — RepoProfile

Companion of `.pi/extensions/reactive-xaf-build/profile.ts`.

`rxProfile` is the default (`profileOf(seams)`). `expandProfile` is the
second object of the same shape. The engine calls profile methods; it
never tests `label === "eXpand"`.

## Fields

- `label` — display name ("Reactive.XAF" / "eXpand"). Not `name` (SelectItem gate).
- `menuProjectPick` — "RX-XAF" / "eXpand". Always shown on Build/Publish.
- `knownRoots` — extra paths `resolveRepo` tries after cwd (`C:/Work/...`, `D:/...`).
- `detect(cwd)` — props + marker dir. RX: `src/Extensions`. Expand:
  `Xpand/Xpand.ExpressApp.Modules`.
- `buildCmd` / `queueCmd` / `queueLabel` / `pushRemote` — RX: `brx`/`prx`,
  push null (`prx` already pushes). Expand: `bx lab`/`bx Release`, `px` /
  `px -Release`, push `lab` or `eXpand` then queue.
- `chain` — RX 23 → 72 (assert nugets) → 89. Expand Lab 94 / Release 39
  → 38 (nugets) → 37. Def 32 is `_Xpand-Lab`, a 2023 leftover; `px` queues 94.
- `versionFile` — RX `src/Common/AssemblyInfoVersion.cs`. Expand
  `Xpand/Xpand.Utils/Properties/XpandAssemblyInfo.cs` (build writes it;
  we never edit it).
- `nugetId` — RX `xpand.extensions`. Expand `eXpandSystem`.
- `githubRepo` / `githubOnSuccess` — RX both choices: publish draft
  (`eXpandFramework/Reactive.XAF`). Expand Lab: assert published
  (`eXpand.lab`). Expand Release: publish draft (`eXpand`).
- `depPins` — expand only. After DX, rewrite `Xpand.Extensions*` /
  `Xpand.XAF.*` to the latest from the matching feed (lab server / nuget.org).
  Ask-first, same as DX pins. RX leaves this unset.

Helpers: `profileByPick`, `resolveRepo`, `nugetAssertUrl`,
`nugetOrgNuspecUrl`, `githubReleasesUrl`, `githubReleaseUrl`,
`getLatestPackage`, `nextReleaseVersion` (next release on the DX minor:
max(dxBase.Build, last published build + 1), revision 0), `readPrefixedPins`,
`rewritePrefixedPins`, `compareVersions`, `profileOf`.
