---
name: reactive-xaf-build/azdo-refusal
description: Use when an AzDO query's refusal handling changes — azdo-refusal.ts owns the ONE try/catch body that classifies a refused call (auth vs transient) and its reader, shared privately by the status, cancel and test-scan queries.
---

# azdo-refusal.ts — the shared refusal grammar

Companion of `.pi/extensions/reactive-xaf-build/azdo-refusal.ts`. Three queries
in this extension spawn pwsh (`STATUS=`, `CANCEL=`, `TEST=`) and every one of
them can be refused the same way: an expired PAT, a 401, a proxy. The grammar
has one home.

## `errBlock(kind)`

The PS `catch` body. It flattens the message (a newline or a `;` inside it would
break the field protocol the TS side parses), classifies it — `auth` when the
server's own wording says `401|Unauthorized|Access Denied|expired`, `transient`
otherwise — and prints `<kind>=ERR;<auth|transient>;<message>`. `kind` is
`STATUS`, `CANCEL` or `TEST`.

## `classifiedFailure(line, kind)`

The reader: `{ failure, message }` for a `<kind>=ERR;…` line, `null` for any
other line. A consumer switches on the KIND, never on prose.

## Why it is a module and not an export

`azdo.ts` sits at the 400-line cap, and exporting its private helpers to
whichever module needed the same words next fragmented one protocol across three
files. The grammar lives here instead, imported by `azdo.ts` and `test-scan.ts`;
`azdo.ts` re-exports `AzDoFailure` so an importer of that module keeps one name
for the type.

## Why not `$ErrorActionPreference = 'Stop'` script-wide

It made a transient blip indistinguishable from a dead token, and the difference
then had to be guessed from error prose downstream. `auth` is final — no retry
clears it, so the watcher gives up on the FIRST poll and names the remedy;
anything else keeps the chain retrying on its grace clock (`watcher-notice.md`).
