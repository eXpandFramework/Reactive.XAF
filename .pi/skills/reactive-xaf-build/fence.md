---
name: reactive-xaf-build/fence
description: Use when a shell call is refused for running brx/bx/prx/px, go.ps1, BuildPipeline.ps1, BuildDevExpress.XAF.ps1, QuickBuild.ps1 or a pipeline solution build — the tool_call gate in front of this repo's build; it also covers why a filtered Tests.sln run and a single-project build still pass.
---

# fence.ts — the refusal in front of the build

Companion of `.pi/extensions/reactive-xaf-build/fence.ts`.

`/devexpress` owns the build: a pane, a supervisor script, an exit marker and a
watch (`run.md`). An agent cannot type a slash command, so without a door it
reaches for a shell — measured 2026-10-25 in this project's window:
`pwsh -NoLogo -Command "brx" > /tmp/brx.log 2>&1` held the call for 227 s,
returned exit 0, and the `Build FAILED` inside was found by grepping the log
afterwards. `tool.ts` is the door; this module is the backstop, because a door
nobody is forced to use is not a fence.

## Shape

A `tool_call` handler returning `{ block: true, reason }` — pi stops dispatch at
the first block, exactly like lean-ctx's `shell-block.ts` and `grep-fix.ts`. The
reason is what the agent reads as the tool result.

Covered tools: `ctx_shell`, `bash`, `ctx_execute`, `ctx_batch_execute`,
`lean_ctx`, and `ctx_call` when it wraps one of those. A fence that misses one
of them is routed around in one move.

## What is refused

| Rule | Example | Why |
|---|---|---|
| a profile command word | `brx`, `bx lab`, `prx`, `px -Release` | the pipeline itself, run blind |
| a pipeline script, where EXECUTED | `./go.ps1`, `pwsh -File Build\BuildPipeline.ps1`, `QuickBuild.ps1` | same, and `QuickBuild.ps1` deletes every `bin`/`obj` first |
| `dotnet build\|test\|pack` naming a pipeline solution | `dotnet build src\Modules\Modules.sln` | `BuildDevExpress.XAF.ps1` builds `Extensions.sln`/`Modules.sln` and compiles `Tests.sln` |

The word list is not a constant: `pipelineCommands()` reads `buildCmd` /
`queueCmd` off `rxProfile` and `expandProfile`, and the script/solution lists
come from `pipelineEntrypoints` in the same file, so neither the refusal nor the
build can drift from the other.

## What is deliberately allowed

- `dotnet build <one>.csproj` — the fast local loop (6 s on the window that
  measured it).
- `dotnet test <project>.csproj` — a project is never refused.
- a NARROWED `dotnet test` on a pipeline solution — the command carries its test
  selection as an expression (a token with `~` or `=`, which is exactly what the
  documented local loop passes). The unnarrowed suite run is the blind,
  120-minute shape and stays refused.
- reading the scripts (`cat Build/BuildPipeline.ps1`), `git log --oneline brx`,
  `echo "brx"`, `dotnet run` — an argument that spells a word is not a command
  position.
- a redirect into a log (`> b.log 2>&1`) is NOT a rule of its own: it only
  sharpens the reason when the command was already refused. A redirected
  single-project build is a legitimate call whose output the agent wants.

## How the text is read

`segments()` splits on `&&`, `||`, `;`, `|` and newlines, so `cd src && brx` is
seen. Each segment is tokenised, quotes stripped, and the head checked — after
`unwrap()` resolves one level of shell wrapper (`pwsh -Command "…"` → the nested
text, `pwsh -File x.ps1` → the script). `lean_ctx`'s first argument is its own
subcommand, so the command it runs is the tail; `ctx_call`'s wrapped input is
re-read by name.

## Scope, stated because it is a limit

- This file ships in the repo's `.pi/extensions`, so it guards THIS project's
  sessions and nothing fleet-wide. A window in another project still runs brx
  blind.
- A command assembled to evade text matching (`'b'+'rx'`, `Start-Process`,
  `Invoke-Expression` on a computed string) is not caught. The target is the
  accidental blind call, not a determined bypass.

## API

- `shellText(toolName, input)` — the command a tool call carries, or null.
- `lineRefusal(command, depth?)` — the first refusal on a command line.
- `refusalFor(toolName, input)` — the `{ block, reason }` for one call.
- `registerBuildFence(pi)` — the hook, registered from `index.ts`'s factory.
