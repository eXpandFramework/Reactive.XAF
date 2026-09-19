# Reactive.XAF — Agent Guide

Project-specific knowledge distilled from the lab-build workflow session (2026-08-24). Applies on top of the global system.md rules.

## Build workflow (lab branch)

1. **DX check** — query nuget.org flat-container for `DevExpress.ExpressApp` (`https://api.nuget.org/v3-flatcontainer/devexpress.expressapp/index.json`), take the max stable version (26.1.4 as of 2026-08-24). Compare with the `DevExpress.*` pins in `Directory.Packages.props`: all on one version → ask before update-all; mixed versions → leave the file untouched.
2. **Build** — `brx` (lab) / `brx -Release` (master) — the eXpandFramework module alias (`C:\Box\PSModules`). Runs the psake pipeline: `go.ps1` → `BuildPipeline.ps1` → `Build/BuildDevExpress.XAF.ps1` (tasks: Clean, Init, UpdateProjects, Compile, CheckVersions, IndexSources, CompileTests).
3. **Warnings are fatal** — `-WarnAsError` is baked into every compilation: any warning fails the build. Fix the warnings, re-run.
4. **Timings.** The ~39-minute figure belongs to the AzDO pipeline, which runs the test suite. A local Lab build skips the tests and finishes in roughly 10-20 minutes. Never kill a build early, and any timeout must exceed a local build.
5. **Commit then publish** — commit the build state (props update, `src/Common/AssemblyInfoVersion.cs`, nuspec bumps), then `prx` (stage + force-push `lab` → remote + queue the AzDO `Reactive.XAF` pipeline). Ensure the Hyper-V VMs **C11–C14 are running** before `prx`. The `/devexpress` publish step does this itself (VM check → commit → confirm → prx); `Build/nuspec/*` and `AssemblyInfoVersion.cs` change on **any** Lab build, from **any** window — treat them as the build's state, decide commit-vs-revert with the user, never claim or dispose of them unilaterally.

## The /devexpress extension

`.pi/extensions/reactive-xaf-build/` (repo-local, auto-loads in this project's sessions):

- `/devexpress` takes no arguments: the menu opens on Build | Publish | Last build status | Cancel AzDO build | Start AzDO watcher, and the build/publish/watcher items then pick RX-XAF | eXpand and Lab | Release. The old word forms (`/devexpress status`, `cancel`, `watch`, `build lab`, `publish lab`) were retired with the menu-only surface.
- The build runs in a new right-side psmux pane (live output there), driven by a per-run supervisor script whose exit code lands in a transient marker. The command returns on a STARTED build and a background watch reports the outcome, so a build never holds the agent.
- Failure → warning steer (shared sender, `triggerTurn`). A build that goes silent for 10 minutes with no CPU progress, or runs past 20 minutes, is reported once. The watch never kills a build; `/devexpress → "Abort build"` stops it deliberately and reports no failure.
- Success → the publish continuation runs from the watch; the pane is kept for reuse and a conversational ask offers closing it via `/devexpress → "Close build pane"`.
- Falls back to an in-process build when the pane cannot be opened, still reported by the watch.
- The extension lives in the repo — changes need the project-local extension write allowance (see below).

## Environment gotchas

- **pwsh ≠ powershell.exe.** PS 5.1 (`powershell.exe`) reads BOM-less UTF-8 files as ANSI. `Tmux.ps1` (profile, `C:\Box\PSModules\eXpandFramework\Functions\`) is BOM-less with non-ASCII comments — it misparses under PS 5.1, breaking ANY `powershell.exe -command` that loads the profile (build post-steps with `LogStandardErrorAsError="True"` fail with MSB3077). Keep the BOM on that file; run builds via pwsh.
- **Write-guard allowances persist per project** (`agent/state/write-guard/`): the project-local `.pi/extensions` allowance and the per-extension allowance (e.g. devexpress) survive reloads — no re-arming after the first `/write-guard` allow. Session-scoped memos still clear on reload; the persisted lists don't.
- **Tests run in AzDO** (the pipeline) — the suite is very long; don't run the whole Tests.sln locally by default. On an AzDO test failure, run ONLY the failing tests locally (filter to them), investigate, fix — never the full suite. NUnit + Shouldly + Moq; `dotnet test src\Tests\Tests.sln --settings Build\Tests.runsettings` (120-min session timeout). The Tests.sln builds with `-WarnAsError` in Debug via the psake CompileTests task.
