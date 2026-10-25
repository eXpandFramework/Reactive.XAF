/**
 * reactive-xaf-build/tool-tests — behavior contract for the agent's build door
 * (companion of tool.ts): a start returns at once with the run's identity, a
 * second start names the run already in flight instead of building, status
 * answers with the pane tail, and none of it needs a UI.
 *
 * Runtime: pi's OWN loader and ExtensionRunner through the resolver-first route
 * this file carries itself, with the extension's real factory (`./index.ts`) as
 * the activation — so the fence is registered beside the tool, which is what T5
 * pins: the sanctioned start must never sit behind the fence that refuses the
 * same command through a shell.
 *
 * The build's ports are the extension's own seams (command runner, feed, pane
 * machinery); the real nuget.org, pwsh, psmux and VMs are never touched, and
 * nothing spawns. The in-process case hands the flow a build promise that never
 * settles, because that is the only honest way to hold a run open without a
 * pane — releasing it would run the reporter, and a green build continues into
 * publish.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/tool-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { EventEmitter, once } from "node:events";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";
import { abortBuildRun, activeBuildRun, isBuildRunActive, stopBuildRun } from "./run.js";
import { setBuildPane } from "./pane.js";

const TOOL = "devexpress_build";
const VM_RUN = "C11=Running\nC12=Running\nC13=Running\nC14=Running\n";
const VM_CHECK_PREFIX = "Get-VM -Name C11,C12,C13,C14*";

/** A crash or a rejected detached promise must be LOUD: without this the
 *  process can end mid-suite and the run reports nothing at all. */
process.on("uncaughtException", (err) => {
  console.log(`FAIL uncaught exception: ${(err as Error)?.stack ?? err}`);
  process.exit(1);
});
process.on("unhandledRejection", (err) => {
  console.log(`FAIL unhandled rejection: ${(err as Error)?.stack ?? err}`);
  process.exit(1);
});

let passed = 0;
let failed = 0;
const handles: any[] = [];
const tempDirs: string[] = [];

function check(desc: string, condition: boolean, detail?: string): void {
  if (condition) {
    passed++;
    console.log(`  PASS  ${desc}`);
    return;
  }
  failed++;
  console.log(`  FAIL  ${desc}${detail ? ` — ${detail}` : ""}`);
}

/** The harness's build function, filled in by installRoute(). */
let buildRealRunner: (opts: { cwd?: string; ui?: Record<string, unknown>; activate: (pi: any) => void }) => Promise<any>;

async function installRoute(): Promise<void> {
  await import(new URL("./resolve.mjs", import.meta.url).href);
  buildRealRunner = (await import("@pi/pi-dev/real-runner.js")).buildRealRunner;
  await import("@pi/pi-dev/llm-utils.js"); // publishes __steer (the reporter's warning path)
  await import("@pi/pi-dev/tracked-write.js"); // publishes __writeFileSync (the pin rewrite path)
}

/** A repo the profile recognises: the props file plus src/Extensions. */
function mkRepo(pin?: string): string {
  const root = mkdtempSync(join(tmpdir(), "rxaf-tool-"));
  tempDirs.push(root);
  mkdirSync(join(root, "src", "Extensions"), { recursive: true });
  const props = pin
    ? `<Project><ItemGroup><PackageReference Include="DevExpress.ExpressApp" Version="${pin}" /></ItemGroup></Project>\n`
    : "<Project />\n";
  writeFileSync(join(root, "Directory.Packages.props"), props);
  return root;
}

interface Runner {
  run: (cmd: string) => Promise<any>;
  calls: string[];
}

/** The command-runner port. `hangBuilds` holds the in-process build open, so a
 *  run without a pane stays a run instead of settling into the publish phase:
 *  the build waits on an event nothing raises, which is composition (a named
 *  Conflux-free signal) rather than a hand-rolled deferred. */
function mkRunner(hangBuilds = false): Runner {
  const calls: string[] = [];
  const builds = new EventEmitter();
  return {
    calls,
    run: async (cmd: string) => {
      calls.push(cmd);
      if (cmd.startsWith(VM_CHECK_PREFIX)) return { code: 0, stdout: VM_RUN, stderr: "" };
      if (hangBuilds && cmd === "brx") await once(builds, "build-done");
      return { code: 0, stdout: "", stderr: "" };
    },
  };
}

interface Pane {
  openBuildPane: (repo: string) => Promise<string | null>;
  runInPane: (pane: string, cmd: string) => Promise<void>;
  capturePane: (pane: string) => Promise<string>;
  closePane: (pane: string) => Promise<void>;
  opened: string[];
  sent: string[];
  tail: string;
}

function mkPane(noPane = false): Pane {
  const opened: string[] = [];
  const sent: string[] = [];
  const pane: Pane = {
    opened,
    sent,
    tail: "",
    openBuildPane: async () => {
      if (noPane) return null;
      opened.push(`%${opened.length + 1}`);
      return opened[opened.length - 1];
    },
    runInPane: async (_pane: string, cmd: string) => {
      sent.push(cmd);
    },
    capturePane: async () => pane.tail,
    closePane: async () => {},
  };
  return pane;
}

interface Session {
  handle: any;
  def: any;
  pane: Pane;
  runner: Runner;
  feeds: () => number;
}

/** Build the extension for one case: pi's real loader + ExtensionRunner, the
 *  real factory (command + tool + fence), the case's own ports. */
async function mkSession(repo: string, opts: { noPane?: boolean; feed?: string; hangBuilds?: boolean } = {}): Promise<Session> {
  const runner = mkRunner(opts.hangBuilds === true);
  const pane = mkPane(opts.noPane === true);
  let feeds = 0;
  const feedText = JSON.stringify({ versions: [opts.feed ?? "26.1.4"] });
  const handle = await buildRealRunner({
    cwd: repo,
    activate: (pi: any) =>
      activate(pi, {
        run: runner.run,
        fetchFeed: async () => {
          feeds++;
          return feedText;
        },
        propsPath: join(repo, "Directory.Packages.props"),
        repoRoot: repo,
        openBuildPane: pane.openBuildPane,
        runInPane: pane.runInPane,
        capturePane: pane.capturePane,
        closePane: pane.closePane,
        runCadence: { signalMs: 60_000, probeMs: 60_000, cpuMs: 60_000, stallMs: 600_000, overrunMs: 1_200_000 },
      }),
  });
  handles.push(handle);
  const found = handle.runner.getAllRegisteredTools().find((t: any) => t?.definition?.name === TOOL);
  return { handle, def: found?.definition, pane, runner, feeds: () => feeds };
}

/** Run the tool the way pi does: (id, params, signal, onUpdate, ctx). A UI is
 *  optional and most cases have none on purpose — a headless or worker window is
 *  exactly where a blind shell does the most damage. */
async function call(
  session: Session, params: Record<string, unknown>, cwd?: string, ui?: Record<string, unknown>,
): Promise<string> {
  const ctx: Record<string, unknown> = {};
  if (cwd) ctx.cwd = cwd;
  if (ui) ctx.ui = ui;
  const result = await session.def.execute(null, params, undefined, undefined, ctx);
  return result?.content?.[0]?.text ?? JSON.stringify(result);
}

/** Every case leaves the run state and the pane global clean for the next one. */
function resetRunState(): void {
  stopBuildRun();
  setBuildPane(null);
}

(async () => {
  await installRoute();
  try {
    // Section: a start returns at once, carrying the run's identity
    console.log("start returns at once");
    {
      const repo = mkRepo();
      const session = await mkSession(repo);
      const run = activeBuildRun();
      check("T0 no run is watched before the first start", run === null, JSON.stringify(run));
      const prompts: string[] = [];
      const recordingUi = {
        notify: async () => {},
        select: async (title: string) => {
          prompts.push(title);
          return undefined;
        },
      };
      const text = await call(
        session, { action: "start", choice: "Lab", project: "RX-XAF" }, undefined, recordingUi,
      );
      const started = activeBuildRun();
      check("T1 the answer says started", text.startsWith("started:"), text);
      check("T1 the pane seam was opened once", session.pane.opened.length === 1, JSON.stringify(session.pane.opened));
      check("T1 the supervisor command reached the pane", session.pane.sent.length === 1, JSON.stringify(session.pane.sent));
      check(
        "T1 the answer carries the run id and the pane",
        !!started && text.includes(started.runId) && text.includes(String(started.pane)),
        text,
      );
      check("T1 the call returned while the build is still watched", isBuildRunActive(), `active=${isBuildRunActive()}`);
      check("T1 the start raised no prompt at all", prompts.length === 0, JSON.stringify(prompts));
      resetRunState();
    }

    // Section: a second start names the run instead of building
    console.log("second start refused");
    {
      const repo = mkRepo();
      const session = await mkSession(repo);
      await call(session, { action: "start" });
      const started = activeBuildRun();
      const again = await call(session, { action: "start" });
      check("T2 the second start is not started", again.startsWith("not started:"), again);
      check("T2 it names the running run id", !!started && again.includes(started.runId), again);
      check("T2 no second pane was opened", session.pane.opened.length === 1, JSON.stringify(session.pane.opened));
      check("T2 no second DX fetch happened", session.feeds() === 1, `feeds=${session.feeds()}`);
      resetRunState();
    }

    // Section: status shows the pane tail
    console.log("status returns the pane tail");
    {
      const repo = mkRepo();
      const session = await mkSession(repo);
      await call(session, { action: "start" });
      const started = activeBuildRun();
      session.pane.tail = "Compiling Xpand.Main\nBuild FAILED\n";
      const text = await call(session, { action: "status" });
      check("T3 status names the run in flight", !!started && text.includes(started.runId), text);
      check("T3 status returns the captured tail", text.includes("Build FAILED"), text);
      resetRunState();
    }

    // Section: status with nothing watched says the truth about other windows
    console.log("status with nothing watched");
    {
      const repo = mkRepo();
      const session = await mkSession(repo);
      const text = await call(session, { action: "status" });
      check("T4 status says nothing is watched here", text.includes("nothing is watched in THIS window"), text);
      check("T4 status says another window is invisible", text.includes("invisible"), text);
      resetRunState();
    }

    // Section: no pane falls back in-process, and it is still a run
    console.log("in-process fallback");
    {
      const repo = mkRepo();
      const session = await mkSession(repo, { noPane: true, hangBuilds: true });
      const text = await call(session, { action: "start" });
      const started = activeBuildRun();
      check("T6 the answer says in-process", text.includes("in-process"), text);
      check("T6 the in-process run is watched", isBuildRunActive() && !!started && started.pane === null, text);
      const again = await call(session, { action: "start" });
      check(
        "T6 a second start is still refused, naming the run",
        again.startsWith("not started:") && !!started && again.includes(started.runId),
        again,
      );
      resetRunState();
    }

    // Section: the door is not behind its own fence
    console.log("sanctioned path is not fenced");
    {
      const repo = mkRepo();
      const session = await mkSession(repo);
      let fired = 0;
      const blocked = await session.handle.emitToolCall({
        type: "tool_call",
        toolName: "ctx_shell",
        input: { command: "brx" },
        toolCallId: `tb-${(fired += 1)}`,
      });
      check("T5 the same command through ctx_shell is refused", blocked?.block === true, JSON.stringify(blocked));
      const text = await call(session, { action: "start" });
      check("T5 the tool's own start still begins the build", text.startsWith("started:"), text);
      check("T5 the build reached the pane", session.pane.opened.length === 1, JSON.stringify(session.pane.opened));
      resetRunState();
    }

    // Section: a genuine decision is refused, never taken silently
    console.log("a genuine pick refuses instead of answering");
    {
      const repo = mkRepo("26.1.0");
      const session = await mkSession(repo, { feed: "26.1.4" });
      const text = await call(session, { action: "start" });
      check("T7 the start aborts at the pin decision", text.includes("aborted") && text.includes("needs a decision"), text);
      check("T7 no pane was opened", session.pane.opened.length === 0, JSON.stringify(session.pane.opened));
      check("T7 the props file is untouched", readFileSync(join(repo, "Directory.Packages.props"), "utf-8").includes('Version="26.1.0"'));
      check("T7 no run is watched after the refusal", !isBuildRunActive(), `active=${isBuildRunActive()}`);
      resetRunState();
    }
    // Section: an in-process run is stopped without killing anything
    console.log("in-process abort");
    {
      const repo = mkRepo();
      const session = await mkSession(repo, { noPane: true, hangBuilds: true });
      await call(session, { action: "start" });
      const started = activeBuildRun();
      const stopped = await abortBuildRun();
      check("T8 the abort returns the run it stopped", !!started && stopped?.runId === started.runId, JSON.stringify(stopped));
      check("T8 an in-process run names no pane", stopped?.pane === null, JSON.stringify(stopped));
      check("T8 the watch is stopped", !isBuildRunActive(), `active=${isBuildRunActive()}`);
      resetRunState();
    }
  } catch (err) {
    failed++;
    console.log(`FAIL tool-tests crashed: ${(err as Error)?.stack ?? err}`);
  } finally {
    for (const handle of handles) {
      try {
        handle.dispose();
      } catch {
        /* a disposed host is not a failure */
      }
    }
    for (const dir of tempDirs) rmSync(dir, { recursive: true, force: true });
  }

  // Section: Summary separator
  const total = passed + failed;
  console.log(`\n${"=".repeat(40)}`);
  console.log(`${passed}/${total} passed`);
  if (failed > 0) {
    console.log(`${failed} FAILED`);
    process.exitCode = 1;
  }
  // Section: All tool tests passed
  else console.log("All tool tests passed.");
})();
