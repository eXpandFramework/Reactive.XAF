---
name: reactive-xaf-build
description: Use when working on or invoking the /devexpress extension in Reactive.XAF — the Lab/Release build-publish flow (DX check, brx in a pane, commit, queue, AzDO monitor, GitHub draft publish) and its skip-build publish-only variant, including the command menu, the flow engine, RepoProfile, and their tests.
---

# /devexpress (reactive-xaf-build)

Repo-local extension at `.pi/extensions/reactive-xaf-build/` (auto-loads in
Reactive.XAF sessions). One command: `/devexpress` — the Lab/Release build +
publish workflow. Repo-specific values live on `RepoProfile` (`profile.ts`);
RX is the default. Expand is a second profile, picked from the same menu.

Both RX choices run the `Reactive.XAF` pipeline (**def 23**) — Lab queues
branch lab, Release (`prx -Release`) queues branch master — followed by
PublishNugets (**def 72**) and release consumers (**def 89**).

Expand Lab queues **def 94** (`Xpand-Lab`; `px`). Def 32 is `_Xpand-Lab`,
a 2023 leftover. Release is def 39 → 38 → 37.

## Command surface

- `/devexpress` — interactive menu: Build | Publish → **RX-XAF | eXpand**
  → Lab | Release, plus "Last build status", "Cancel AzDO build" and
  "Close build pane" while a build pane is open.
- `/devexpress status` — one-shot AzDO status of the newest build (`profile.statusDef`).
- `/devexpress cancel` — PATCH-cancel the newest running build.
- `/devexpress watch` — start the chain watcher for the current build.
- `/devexpress build lab|release` — full flow on the current profile.
- `/devexpress publish lab|release` — skip-build on the current profile.

Menu picks run in the INVOKING window. The eXpand pick uses
`resolveRepo` (cwd, then known roots only when cwd is the other repo).

## Flow (Lab | Release)

1. **DX check** — nuget.org flat-container, max stable `DevExpress.ExpressApp`.
2. **Props compare** — `Directory.Packages.props` DevExpress.* pins.
3. **depPins** (expand only) — latest `Xpand.Extensions` from the matching feed.
4. **Build** — `profile.buildCmd` in a right-side pane.
5. **Publish** — VMs C11–C14, commit, optional `git push`, `profile.queueCmd`,
   AzDO watcher (`publish.ts`).

## Module map

| Module | Doc | Purpose |
|---|---|---|
| `index.ts` | — | Boot: registers the command (thin). |
| `profile.ts` | `profile.md` | RepoProfile: RX default + expand. `profileByPick`, `resolveRepo`. |
| `pins.ts` | `pins.md` | Expand-only RX package pin rewrite. |
| `publish.ts` | `publish.md` | VMs, commit, queue, watcher start. |
| `menu.ts` | `menu.md` | Command surface: RX-XAF | eXpand then Lab | Release. |
| `build.ts` | `build.md` | Flow engine (DX, local build, menu wiring). |
| `watcher.ts` | `watcher.md` | Background AzDO chain watcher. |
| `menu-tests.ts` | `menu-tests.md` | Skip-build contract. |
| `delegate-tests.ts` | `delegate-tests.md` | Delegation fallback. |
| `build-tests.ts` | `build-tests.md` | Full flow. |
| `watcher-tests.ts` | `watcher-tests.md` | Watcher W1–W16. |
| `profile-tests.ts` | `profile-tests.md` | RepoProfile (RX vs expand). |
| `azdo.ts` / `status.ts` | `azdo.md` | AzDO status/cancel. |
| `delegate.ts` | `delegate.md` | Dormant. |
| `pane.ts` | — | Build pane primitives. |

Run: `npx tsx C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/{menu,build,watcher,azdo,profile}-tests.ts`
