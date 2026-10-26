/**
 * reactive-xaf-build/pane-tests — where the build pane lands, and what it does
 * to the pane the user is typing in.
 *
 * The contract this file pins is the one the user feels: starting a build puts
 * a live output pane to the RIGHT of the invoking pane and leaves the invoking
 * pane at the keyboard. A plain `split-window` makes the new pane active, which
 * is the bug this suite exists for; the case below drives the REAL
 * `defaultOpenBuildPane` against a REAL psmux server on its own socket, so the
 * production argv is what is measured, and the plain form runs beside it as the
 * CONTROL that proves the measurement can fail.
 *
 * The second contract here is the other half of the same machinery: `runArgv`
 * runs every command the pane and the gates issue, and it must ANSWER. A child
 * that outlives its budget is killed and reported as `timedOut`, never waited on:
 * the publish hung for minutes on a probe whose kill never reported.
 *
 * Runtime: pi's own runtime through `buildRealRunner` (the extension loads for
 * real, only the host stubbed), plus the shared psmux harness on an isolated
 * socket, plus real `pwsh` children for the deadline and reap cases. Nothing here
 * spawns pi, so no boot proof is carried.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/pane-tests.ts
 */
// test-timeout: 120000 — the real runner boots, then a real psmux server, one split and one control
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import activate from "./index.js";
import { defaultOpenBuildPane, defaultRunInPane, runArgv, REAP_MS } from "./pane.js";

let ok = 0;
let fail = 0;
function check(label: string, cond: boolean, detail?: string): void {
  if (cond) {
    ok++;
    console.log("PASS " + label);
  } else {
    fail++;
    console.log("FAIL " + label + (detail ? " — " + detail : ""));
  }
}

const handles: any[] = [];
const SESSION = "rxafbuild";
/** Keeps the build pane alive long enough to be listed: a bare shell in a
 *  detached session exits without a TTY and takes the session with it. */
const HOLD = "ping -n 3600 127.0.0.1";

/** The harness's isolated server. Its types sit behind a bare `@pi/` specifier
 *  a plain-tsx process cannot resolve, and a static import would be linked
 *  BEFORE the resolver hook exists. */
interface Isolated {
  socket: string;
  run(cmd: string): Promise<string>;
  newSession(name: string): Promise<string>;
  listPanes(target?: string): Promise<string[]>;
  cleanup(): Promise<void>;
}
let withIsolatedPsmuxServer: (() => Isolated) | null = null;
let buildRealRunner: ((opts: { activate: (pi: any) => void }) => Promise<any>) | null = null;

/** The resolver hook first, then the harnesses BY NAME. Idempotent: the hook is
 *  a process-wide registration. */
async function installRoute(): Promise<void> {
  await import(new URL("./resolve.mjs", import.meta.url).href);
  await import("@pi/pi-dev/llm-utils.js");
  await import("@pi/pi-dev/tracked-write.js");
  const piDev = (await import("@pi/pi-dev/real-runner.js")) as {
    buildRealRunner: (opts: { activate: (pi: any) => void }) => Promise<any>;
  };
  buildRealRunner = piDev.buildRealRunner;
  const harness = (await import("@pi/psmux/psmux-test-harness.js")) as { withIsolatedPsmuxServer: () => Isolated };
  withIsolatedPsmuxServer = harness.withIsolatedPsmuxServer;
}

/** The session's current window and its ACTIVE pane, e.g. "0:%3" — the only
 *  surface that answers "did the view move" (`capture-pane` is not usable on an
 *  isolated socket). */
async function activeOf(iso: Isolated): Promise<string> {
  return (await iso.run(`display-message -p -t ${SESSION} "#{window_index}:#{pane_id}"`)).trim();
}

/** P0 — the extension loads in pi's own runtime with the placement change. */
async function caseBoot(): Promise<void> {
  if (buildRealRunner === null) {
    fail++;
    console.log("FAIL  the pi harness resolved to nothing");
    return;
  }
  const handle = await buildRealRunner({ activate: (pi: any) => activate(pi) });
  handles.push(handle);
  check("P0: the extension loads and registers /devexpress", handle.commands().includes("devexpress"), handle.commands().join(", "));
  const errors = JSON.stringify(handle.host.errors);
  check("P0: the boot reported no handler errors", errors === "[]", errors);
}

/** The CONTROL: the same split without the detach flag moves the active pane on
 *  this very server, so the detached case is a measurement, not a tautology. */
async function casePlainControl(iso: Isolated, caller: string, before: string, cwd: string): Promise<void> {
  const tail = `-- pwsh -NoLogo -NoProfile -Command "Start-Sleep -Seconds 120"`;
  const plain = (await iso.run(`split-window -h -t ${caller} -P -F "#{pane_id}" -c "${cwd}" ${tail}`)).trim();
  const afterPlain = await activeOf(iso);
  check("P1 control: a plain split moves the active pane", plain !== "" && afterPlain !== before, `plain=${plain} ${before} -> ${afterPlain}`);
  if (plain !== "") await iso.run(`kill-pane -t ${plain}`);
}

/** P1 — the build pane opens beside the caller and the caller keeps the view. */
async function caseDetachedSplit(iso: Isolated): Promise<void> {
  await iso.newSession(SESSION);
  const before = await activeOf(iso);
  const caller = before.split(":")[1] ?? "";
  check("P1: the isolated session has one active pane", caller !== "", before);
  process.env.TMUX_PANE = caller;
  const cwd = process.cwd().replace(/\\/g, "/");
  const pane = await defaultOpenBuildPane(cwd);
  check("P1: the build pane was opened", pane !== null, String(pane));
  const afterSplit = await activeOf(iso);
  check("P1: a detached split leaves the caller's pane active", afterSplit === before, `${before} -> ${afterSplit}`);
  if (pane === null) return;
  await defaultRunInPane(pane, HOLD);
  const panes = await iso.listPanes(SESSION);
  check("P1: the build pane exists beside the caller", panes.includes(pane), JSON.stringify(panes));
  const afterSend = await activeOf(iso);
  check("P1: handing the pane its command does not move the view", afterSend === before, `${before} -> ${afterSend}`);
  await casePlainControl(iso, caller, before, cwd);
  await iso.run(`kill-pane -t ${pane}`);
}

/** What an answer may take beyond its deadline, in the TEST's own numbers: the reap
 *  bound plus `taskkill` and pipe teardown. Deliberately NOT derived from `REAP_MS` —
 *  a bound built from the constant it guards goes green when that constant grows, and
 *  a reap wait becoming the hang is exactly the regression this suite exists to catch. */
const ANSWER_CEILING_MS = 2000;
function answerBound(deadlineMs: number): number {
  return deadlineMs + ANSWER_CEILING_MS;
}

/** The reap stays a verification, not a wait. Asserted below, because a bound nobody
 *  measures is a bound that drifts. */
const REAP_CEILING_MS = 1000;

/** P2 — a command that outlives its budget comes back, marked, AT its deadline.
 *  `runArgv` used to resolve on the close event and nothing else, so a kill that
 *  never reported was a call that never returned. */
async function caseRunDeadline(): Promise<void> {
  const started = Date.now();
  const killed = await runArgv(["pwsh", "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 30"], 1500);
  const elapsed = Date.now() - started;
  check("P2: a child past its deadline answers instead of hanging", killed.timedOut === true && killed.code === -1, JSON.stringify(killed));
  check("P2: the deadline settles the call, not the child's death report", elapsed < answerBound(1500), `${elapsed}ms`);
  const quick = await runArgv(["pwsh", "-NoProfile", "-NonInteractive", "-Command", "Write-Output done"], 30000);
  check("P2: a child that finishes carries no timeout mark", quick.code === 0 && quick.stdout.trim() === "done" && quick.timedOut === undefined, JSON.stringify(quick));
}

/** P3 — P2's control: the kill is a no-op, so nothing ever reports a death. The
 *  invariant the fix exists for is that the call answers anyway; on the pre-fix
 *  shape this is the case that hangs. */
async function caseMissedKill(): Promise<void> {
  const started = Date.now();
  const res = await runArgv(["pwsh", "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 30"], 1000, undefined, undefined, () => undefined);
  const elapsed = Date.now() - started;
  check("P3: a kill that never lands still answers, marked", res.timedOut === true && res.code === -1 && res.reaped === false, JSON.stringify(res));
  check("P3: and it answers inside the deadline plus the reap bound", elapsed < answerBound(1000), `${elapsed}ms`);
}

/** P4 — the REAP, measured at the process boundary rather than at the return
 *  value: the child prints its own pid and then outlives its deadline, so the pid
 *  can be looked up afterwards. The old flow killed and assumed, and this box
 *  carried 300+ leftover `pwsh` as the evidence that assuming is not enough. */
async function caseReap(): Promise<void> {
  const res = await runArgv(["pwsh", "-NoProfile", "-NonInteractive", "-Command", "$PID; Start-Sleep -Seconds 30"], 2000);
  const pid = Number(res.stdout.trim().split("\n")[0]);
  check("P4: the child under test answered its deadline, marked", res.timedOut === true && res.reaped === true && Number.isInteger(pid) && pid > 0, JSON.stringify(res));
  check("P4: the reap bound stays a verification, not a wait", REAP_MS <= REAP_CEILING_MS, `${REAP_MS}ms`);
  if (!Number.isInteger(pid) || pid <= 0) return;
  const alive = await runArgv(["pwsh", "-NoProfile", "-NonInteractive", "-Command", `(Get-Process -Id ${pid} -ErrorAction SilentlyContinue).Count`], 30000);
  check("P4: the killed child is gone, not left running", alive.code === 0 && alive.stdout.trim() === "0", `pid ${pid} -> ${alive.stdout.trim()} (exit ${alive.code})`);
}

async function main(): Promise<void> {
  console.log("reactive-xaf-build/pane-tests — the build pane's placement, real psmux\n");
  await installRoute();
  await caseBoot();
  await caseRunDeadline();
  await caseMissedKill();
  await caseReap();
  if (withIsolatedPsmuxServer === null) {
    fail++;
    console.log("FAIL  the shared psmux harness resolved to nothing — nothing was measured");
    return;
  }
  const iso = withIsolatedPsmuxServer();
  const prevPane = process.env.TMUX_PANE;
  try {
    await caseDetachedSplit(iso);
  } finally {
    if (prevPane === undefined) delete process.env.TMUX_PANE;
    else process.env.TMUX_PANE = prevPane;
    await iso.cleanup();
  }
}

main()
  .catch((e) => {
    fail++;
    console.log(`FAIL  the run crashed: ${(e as Error)?.stack ?? e}`);
  })
  .finally(() => {
    for (const handle of handles) handle.dispose();
    console.log(`\n${ok} passed, ${fail} failed`);
    process.exitCode = fail > 0 ? 1 : 0;
  });
