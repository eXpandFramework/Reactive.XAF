---
name: reactive-xaf-build/resolve
description: Use when a suite in this repo must import the home pi tree's shared harness (pi-dev's real-runner, boot-proof, llm-utils, tracked-write), which is on another drive and so unreachable by a relative import — or when changing `resolve.mjs`, the Node resolve hook that maps `.js` onto its `.ts` sibling and resolves `@pi/<name>/...` through the platform's shared-utilities.json whitelist.
---

# resolve.mjs — the suites' route into the home pi tree

Companion of `.pi/extensions/reactive-xaf-build/resolve.mjs`. Adapted,
deliberately near-verbatim, from `.pi/extensions/expenses/resolve.mjs` — the
same mechanism in another tree, and the two must stay in step.

## Why it exists

The shared test harness lives in the home tree
(`~/.pi/agent/extensions/pi-dev/…`), which this repo cannot reach by a
relative import: it is another drive. Production code never needs this file —
under pi the extension loader resolves both shapes itself — it exists for the
suites, which run under plain Node + tsx.

## The rule it installs

`registerHooks` (the SYNCHRONOUS `node:module` hooks; they cover `require` and
`import` alike) adds two resolution rules:

1. a `.js` specifier maps onto its `.ts` sibling, and a directory specifier
   onto its `index.ts` — pi's loader does the same for extensions, and the
   platform's modules are `.ts` files whose own imports say `.js`;
2. `@pi/<name>/…` resolves into `~/.pi/agent/extensions/<name>/…`, and
   `@pi/contracts/<file>` into `~/.pi/agent/contracts/`, GATED by the
   platform's own `shared-utilities.json`: the name must be on its `shared`
   list (`pi-dev` is). The prefix is a whitelist over the platform's list,
   never a hole into the install.

The home dir is computed (`homedir()`), so no absolute path is committed.

## How a suite installs it (order is load-bearing)

```ts
await import(new URL("./resolve.mjs", import.meta.url).href);
const { buildRealRunner } = await import("@pi/pi-dev/real-runner.js");
```

It must be the FIRST statement of the suite's async `main()`. ESM links a
static import before any module body runs, so a static `@pi/...` import can
never resolve through this hook; and a `.ts` fixture may not use top-level
await (the write gate refuses it). Hence: install dynamically, then import
`@pi/...` names dynamically. `menu-tests.ts` is the worked example and
`menu-tests.md` carries the full recipe.

## Failure modes

- **The shared list is unreadable** (missing file, bad JSON): one line on
  stderr — `resolve.mjs: the shared list is unreadable, no name resolves: …` —
  and NO name resolves. Deliberately loud: an empty whitelist that looks like
  "no such module" sends the next reader hunting in the wrong place.
- **A name the platform does not share**: not resolved here, so Node's own
  resolution runs and fails (`ERR_MODULE_NOT_FOUND`). That refusal is the
  contract `menu-tests.ts` case R2 asserts.
- **A shared name with no such file**: the same fallthrough, the same loud
  failure.
- **Any non-`@pi/` specifier**: untouched, apart from the `.js`→`.ts` retry
  when Node's own resolution throws.

## What it does not do

It reads the shared list once at load. It writes nothing, opens no socket and
reaches no network; it is not a production loader; it does not cache
resolutions; and it says nothing about `node_modules` — a bare specifier that
is not `@pi/...` is Node's business.

## Verified by

`menu-tests.ts`: R2 pins the whitelist refusal, and every `@pi/...` import the
suite makes is this file resolving (`real-runner`, `boot-proof`, `pi-runner`,
`llm-utils`, `tracked-write`). Both measured runs of that suite — the test
island and the real tree — got past resolution, so the hook itself is green in
both.
