/**
 * reactive-xaf-build/fence — the refusal in front of this repo's build commands.
 *
 * /devexpress owns the build: a right-side pane, a supervisor script, an exit
 * marker and a background watch (run.ts). An agent cannot type a slash command,
 * so without a door it reaches for a shell. Measured 2026-10-25 in this
 * project's window: `pwsh -NoLogo -Command "brx" > /tmp/brx.log 2>&1` held the
 * call for 227 s, returned exit 0, and the `Build FAILED` inside was found by
 * grepping the log afterwards. The door (`tool.ts`) is the sanctioned path;
 * this module is the backstop for the shell, because a door nobody is forced to
 * use is not a fence.
 *
 * Shape: a `tool_call` block carrying a reason, exactly like lean-ctx's
 * shell-block and grep-fix redirects — pi stops dispatch at the first block —
 * so it covers pi's own `bash`, lean-ctx's `ctx_shell` and everything that can
 * carry a command.
 *
 * Scope, stated because it is a limit and not a promise: this file ships in the
 * repo's `.pi/extensions`, so it guards THIS project's sessions and nothing
 * fleet-wide — a window in another project still runs brx blind. Nor does it
 * catch a command assembled to evade text matching (`'b'+'rx'`,
 * `Start-Process`, `Invoke-Expression` on a computed string): the target is the
 * accidental blind call, not a determined bypass.
 */

import { rxProfile, expandProfile, pipelineEntrypoints } from "./profile.js";
import type { Choice, RepoProfile } from "./profile.js";

/** The tools whose input can carry a shell command. pi's own shell tool and
 *  lean-ctx's `ctx_shell` are the two an agent reaches for; `ctx_execute`,
 *  `ctx_batch_execute` and the `ctx_call` wrapper run commands too, and a fence
 *  that misses one of them is routed around in one move. */
const SHELL_TOOLS: ReadonlySet<string> = new Set(["ctx_shell", "bash", "ctx_execute", "ctx_batch_execute"]);
const LEAN_CTX_TOOL = "lean_ctx";
const WRAP_TOOL = "ctx_call";

/** A shell wrapper is unwrapped, never trusted as the command's name. */
const SHELL_HEADS: ReadonlySet<string> = new Set(["pwsh", "powershell", "cmd"]);

const DOTNET_VERBS: ReadonlySet<string> = new Set(["build", "test", "pack"]);

/** The scripts and solutions that run the pipeline, from `profile.ts` — ONE
 *  owner of the vocabulary, so a new entrypoint cannot leave this fence blind
 *  while the menu builds the new command. Matched by filename, and only where
 *  it is EXECUTED: reading one (`cat Build/BuildPipeline.ps1`) stays legal. */
const PIPELINE_SCRIPTS: ReadonlySet<string> = new Set(pipelineEntrypoints.scripts);
const PIPELINE_SOLUTIONS: ReadonlySet<string> = new Set(pipelineEntrypoints.solutions);

/** `> file`, `>> file`, `2> file` — a redirect into a named file, not `2>&1`. */
const REDIRECT_RE = /(^|\s)\d?>>?\s*[^\s>&]+/;

type PipelineKind = "build" | "publish";

/** What a segment was recognised as, before the reason is written. */
interface Refusal {
  label: string;
  kind: PipelineKind;
}

function firstWord(cmd: string): string {
  return (cmd.trim().split(/\s+/)[0] ?? "").toLowerCase();
}

/** The build and publish entrypoints, read off the profiles so this list can
 *  never drift from what a menu flow actually runs. */
function pipelineCommands(): Map<string, PipelineKind> {
  const map = new Map<string, PipelineKind>();
  const profiles: RepoProfile[] = [rxProfile, expandProfile];
  for (const profile of profiles) {
    for (const choice of ["Lab", "Release"] as Choice[]) {
      map.set(firstWord(profile.buildCmd(choice)), "build");
      map.set(firstWord(profile.queueCmd(choice)), "publish");
    }
  }
  return map;
}

const PIPELINE_COMMANDS: ReadonlyMap<string, PipelineKind> = pipelineCommands();

/** The command text a tool call carries, or null when the call is not a shell
 *  call this gate owns. An empty command is null: there is nothing to run. */
export function shellText(toolName: string, input: any): string | null {
  if (!input || typeof input !== "object") return null;
  if (SHELL_TOOLS.has(toolName)) {
    const command = input.command ?? input.cmd;
    return typeof command === "string" && command.trim() ? command : null;
  }
  if (toolName === LEAN_CTX_TOOL) {
    const args = input.args;
    if (Array.isArray(args)) return args.length ? args.map(String).join(" ") : null;
    return typeof args === "string" && args.trim() ? args : null;
  }
  return null;
}

/** One command line as executable segments: `a && b | c` is three, so a build
 *  hidden behind `cd` or a pipe is still seen. */
function segments(command: string): string[] {
  return command.split(/&&|\|\||[;|\n]/).map((s) => s.trim()).filter(Boolean);
}

function tokensOf(segment: string): string[] {
  return segment
    .replace(/^[&@]\s*/, "")
    .split(/\s+/)
    .map((token) => token.replace(/^["']|["']$/g, ""))
    .filter(Boolean);
}

/** The filename a token points at, lowercased: `.\go.ps1` → `go.ps1`. */
function baseName(token: string): string {
  const clean = String(token ?? "").replace(/^["']|["']$/g, "");
  const parts = clean.split(/[\\/]/);
  return (parts[parts.length - 1] ?? "").toLowerCase();
}

/** What a shell wrapper runs: `pwsh -Command "brx"` → the nested text,
 *  `pwsh -File go.ps1` → the script. A bare `pwsh` answers neither. */
function unwrap(tokens: string[]): { nested?: string; script?: string } {
  const head = baseName(tokens[0]).replace(/\.exe$/, "");
  if (!SHELL_HEADS.has(head)) return {};
  const fileAt = tokens.findIndex((token) => /^-+file$/i.test(token));
  if (fileAt !== -1 && tokens[fileAt + 1]) return { script: tokens[fileAt + 1] };
  const cmdAt = tokens.findIndex((token) => /^-+c(ommand)?$/i.test(token));
  if (cmdAt !== -1) return { nested: tokens.slice(cmdAt + 1).join(" ") };
  return {};
}

/** A `dotnet build|test|pack` naming a pipeline solution. A project is never
 *  refused, and neither is a NARROWED test run: the documented local loop
 *  selects the failing tests by an expression (a token carrying `~` or `=`), and
 *  refusing that would teach evasion. The unnarrowed suite run is the blind,
 *  120-minute shape. */
function dotnetRefusal(tokens: string[]): Refusal | null {
  const verb = tokens.find((token, at) => at > 0 && !token.startsWith("-"));
  if (!verb || !DOTNET_VERBS.has(verb.toLowerCase())) return null;
  const solution = tokens.map(baseName).find((name) => name.endsWith(".sln"));
  if (!solution || !PIPELINE_SOLUTIONS.has(solution)) return null;
  const narrowed = verb.toLowerCase() === "test" && tokens.some((token) => /[~=]/.test(token));
  if (narrowed) return null;
  return { label: `dotnet ${verb} ${solution}`, kind: "build" };
}

function segmentRefusal(segment: string, depth: number): Refusal | null {
  const tokens = tokensOf(segment);
  if (!tokens.length) return null;
  const wrapped = unwrap(tokens);
  if (wrapped.script) {
    const name = baseName(wrapped.script);
    return PIPELINE_SCRIPTS.has(name) ? { label: name, kind: "build" } : null;
  }
  if (wrapped.nested !== undefined) {
    if (!wrapped.nested.trim() || depth >= 2) return null;
    return lineRefusal(wrapped.nested, depth + 1);
  }
  const head = baseName(tokens[0]).replace(/\.exe$/, "");
  if (PIPELINE_SCRIPTS.has(head)) return { label: head, kind: "build" };
  const known = PIPELINE_COMMANDS.get(head);
  if (known) return { label: head, kind: known };
  return head === "dotnet" ? dotnetRefusal(tokens) : null;
}

/** The first refusal on a command line, or null. */
export function lineRefusal(command: string, depth = 0): Refusal | null {
  for (const segment of segments(command)) {
    const hit = segmentRefusal(segment, depth);
    if (hit) return hit;
  }
  return null;
}

/** The command inside a `ctx_call` wrapper, when that wrapper names a shell
 *  tool: without this, `ctx_call {name:"ctx_shell"}` is the bypass. */
function wrappedText(input: any): string | null {
  const name = typeof input?.name === "string" ? input.name : "";
  const inner = input?.arguments ?? input?.args;
  if (!name || !inner || typeof inner !== "object") return null;
  return shellText(name, inner);
}

/** The reason the agent reads. It names the door, the pane and what two runs
 *  destroy, because a refusal that only says "no" teaches a workaround. */
function reasonFor(hit: Refusal, command: string): string {
  const pipeline = hit.kind === "publish" ? "publish pipeline" : "build pipeline";
  const logged = REDIRECT_RE.test(command)
    ? " Its output was going into a log file, so a failure would surface only after the call returned."
    : "";
  return `"${hit.label}" is this repo's ${pipeline} — refused through a shell.${logged}` +
    " A shell call is blind: no live pane, no exit-code watch, and a second run collides on" +
    " obj/, bin/, Build/nuspec/* and src/Common/AssemblyInfoVersion.cs." +
    ` Start it with the devexpress_build tool (action "start") — it builds in a right-side pane` +
    ' and reports the outcome; look mid-run with action "status".' +
    " A narrowed run is never refused: name the test selection, not the whole suite.";
}

/** The block for one tool call, or null to let it through. */
export function refusalFor(toolName: string, input: any): { block: true; reason: string } | null {
  if (typeof toolName !== "string") return null;
  const text = toolName === WRAP_TOOL ? wrappedText(input) : shellText(toolName, input);
  if (!text) return null;
  const hit = lineRefusal(text);
  if (hit) return { block: true, reason: reasonFor(hit, text) };
  // lean-ctx's first argument is its own subcommand, so the command it runs is
  // the rest: `lean_ctx exec brx` never reaches the scan above as a head token.
  if (toolName === LEAN_CTX_TOOL) {
    const tail = tokensOf(text).slice(1).join(" ");
    const nested = tail ? lineRefusal(tail) : null;
    if (nested) return { block: true, reason: reasonFor(nested, text) };
  }
  return null;
}

/** Register the shell fence on pi's tool_call dispatch. */
export function registerBuildFence(pi: any): void {
  pi.on("tool_call", (event: any) => refusalFor(event?.toolName, event?.input));
}
