# Reactive.XAF — Agent Guide

Project-specific knowledge distilled from the lab-build workflow session (2026-08-24). Applies on top of the global system.md rules.

## Build workflow (lab branch)

1. **DX check** — query nuget.org flat-container for `DevExpress.ExpressApp` (`https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json`), take the max stable version (26.1.4 as of 2026-08-24). Compare with the `DevExpress.*` pins in `Directory.Packages.props`: all on one version → ask before update-all; mixed versions → leave the file untouched.
2. **Build** — `brx` (lab) / `brx -Release` (master) — the eXpandFramework module alias (`C:\Box\PSModules`). Runs the psake pipeline: `go.ps1` → `BuildPipeline.ps1` → `Build/BuildDevExpress.XAF.ps1` (tasks: Clean, Init, UpdateProjects, Compile, CheckVersions, IndexSources, CompileTests).
3. **Warnings are fatal** — `-WarnAsError` is baked into every compilation: any warning fails the build. Fix the warnings, re-run.
4. **A full Lab build takes ~39 minutes** — never kill it early; any timeout must exceed it.
5. **Commit then publish** — commit the build state (props update, `src/Common/AssemblyInfoVersion.cs`, nuspec bumps), then `prx` (stage + force-push `lab` → remote + queue the AzDO `Reactive.XAF` pipeline). Ensure the Hyper-V VMs **C11–C14 are running** before `prx`. The `/devexpress` publish step does this itself (VM check → commit → confirm → prx); `Build/nuspec/*` and `AssemblyInfoVersion.cs` change on **any** Lab build, from **any** window — treat them as the build's state, decide commit-vs-revert with the user, never claim or dispose of them unilaterally.

## The /devexpress extension

`.pi/extensions/reactive-xaf-build/` (repo-local, auto-loads in this project's sessions):

- `/devexpress` → Build → RX-XAF → **Lab** | **Release**.
- The build runs in a new right-side psmux pane (live output there); milestones notify in the invoking window.
- Failure → warning steer with `triggerTurn` (the agent is notified automatically); the pane is kept for reuse.
- Success → silent; a conversational ask offers closing the pane; close via `/devexpress → "Close build pane"` (no modal, no auto-close).
- Falls back to an in-process build when the pane cannot be opened.
- The extension lives in the repo — changes need the project-local extension write allowance (see below).

## Environment gotchas

- **pwsh ≠ powershell.exe.** PS 5.1 (`powershell.exe`) reads BOM-less UTF-8 files as ANSI. `Tmux.ps1` (profile, `C:\Box\PSModules\eXpandFramework\Functions\`) is BOM-less with non-ASCII comments — it misparses under PS 5.1, breaking ANY `powershell.exe -command` that loads the profile (build post-steps with `LogStandardErrorAsError="True"` fail with MSB3077). Keep the BOM on that file; run builds via pwsh.
- **Write-guard allowances persist per project** (`agent/state/write-guard/`): the project-local `.pi/extensions` allowance and the per-extension allowance (e.g. devexpress) survive reloads — no re-arming after the first `/write-guard` allow. Session-scoped memos still clear on reload; the persisted lists don't.
- **Tests run in AzDO** (the pipeline) — the suite is very long; don't run the whole Tests.sln locally by default. On an AzDO test failure, run ONLY the failing tests locally (filter to them), investigate, fix — never the full suite. NUnit + Shouldly + Moq; `dotnet test src\Tests\Tests.sln --settings Build\Tests.runsettings` (120-min session timeout). The Tests.sln builds with `-WarnAsError` in Debug via the psake CompileTests task.
