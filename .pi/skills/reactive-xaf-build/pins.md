---
name: reactive-xaf-build/pins
description: Use when changing the expand-only RX package pin rewrite — latest Xpand.Extensions from the matching feed, ask-first update of Xpand.Extensions* / Xpand.XAF.* in Directory.Packages.props.
---

# pins.ts — RX package pins

Companion of `.pi/extensions/reactive-xaf-build/pins.ts`.

`depPinsPhase` is a no-op when `profile.depPins` is unset (RX). Expand:
read prefixed pins, if none skip the feed query, else fetch latest
`Xpand.Extensions` from lab-server or nuget.org and ask Update | Skip |
Abort. Abort throws. Update rewrites via `rewritePrefixedPins` +
`__writeFileSync`.
