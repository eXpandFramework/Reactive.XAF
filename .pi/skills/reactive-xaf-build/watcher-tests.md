---
name: reactive-xaf-build/watcher-tests
description: Behavior contract for the AzDO chain watcher — toast per poll, chain advance, nuget assertion, GitHub draft publish, empty-poll retry, missing-token steer, terminal steer, give-up, expand Lab 94, fail-reason, this-run.
---

# watcher-tests.ts — watcher behavior contract

Companion of `.pi/extensions/reactive-xaf-build/watcher-tests.ts`. Drives the
Publish menu item through pi's OWN runtime — the resolver-first route this file
carries itself, in the shape `menu-tests.ts` established — with the extension's
OWN ports injected: the command runner, the feed fetcher, `ghFetch` and a fast
`startAzDoWatcher` seam (20 ms interval).

The hand-written pi this suite used to build (`mkPi` plus a `mkCtx` answering
from per-case pick lists) is GONE: there is no stand-in for pi here, and the
`PUBLISH_PICKS` / `PUBLISH_RELEASE_PICKS` / `PUBLISH_EXPAND_PICKS` queues went
with it. The picks are now keyed by a SUBSTRING of each prompt's title
(`PUBLISH_LAB`, `PUBLISH_RELEASE`, `PUBLISH_EXPAND`), a prompt the case did not
map fails the case, and the retired `["publish", "lab"]` / `["publish",
"release"]` word forms are inert — every case opens the menu the way the user
does, which `build-tests.ts` pins. The run seam serves CRLF `STATUS=` fixtures
per chain step, and `mkSeams` copies its poll queue so a shared `GREEN` fixture
is not emptied across cases.

watcher.ts delivers through `ctx.ui.notify` and
`pi.sendUserMessage(msg, { deliverAs: "steer" })`, so a case asserts the toasts
(`toastsOf` / `toastText`, over `host.notices` with the type) and the agent's
messages (`steersOf`, over `host.messages` with `messageOptions` index-aligned).
Nothing spawns: the real AzDO, GitHub, nuget.org and pwsh are never touched, and
the cases that need a slower or faster chain inject their own `intervalMs` /
`maxMs`.

Run: `npx tsx .pi/extensions/reactive-xaf-build/watcher-tests.ts`

## Pinned behaviors

- W1 — full chain green (Lab): toast per poll, nugets asserted, GitHub
  DRAFT published (`prerelease:true`), chain complete, no steer.
- W2 — failed Reactive.XAF build: warning + steer, then stop.
- W3 — give-up deadline: warning + **steer** + stop.
- W4 — a new publish replaces the previous watcher.
- W5 — command registers through the real index boot.
- W6 — missing nuget version: warning + steer, chain continues.
- W7 — failed release consumers build: steer + stop.
- W8 — missing GitHub draft: warning + steer after retries.
- W9 — draft appears on a retry: published toast, no steer.
- W10 — Release chain polls def 23, nugets on nuget.org, FULL release.
- W11 — missing GH_TOKEN: warning + steer naming the token.
- W12 — empty first polls retry, chain completes, no give-up.
- W13 — Release nugets missing on nuget.org: warning + steer, chain continues.
- W14 — expand lab polls def 94; `26.1.400.0` matches feed `26.1.400`.
- W15 — failed poll with a LOGSTART block: steer carries
  `Release 26.1.301.1 exists`, not "no error lines".
- W16 — finished HEAD build of a different version is not this run: wait
  toast, then give-up steers.
- W17 — nugets missing on the first assert, present on retry (index
  lag): confirmed toast, no warning, no steer.
- W18 — the real AzDO build numbers (`<version>-<dxVersion>` head,
  `yyyyMMdd.N` downstream): the chain runs to completion, no wrong-version
  wait toast, no give-up steer.
