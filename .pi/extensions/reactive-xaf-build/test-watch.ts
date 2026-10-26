/**
 * reactive-xaf-build/test-watch — what the watcher SAYS about a failing test,
 * and the one delegation it offers.
 *
 * Split out of `watcher.ts`/`watcher-notice.ts` (both near the 400-line cap):
 * every other voice in this extension is a single line about a build, while this
 * one owns per-build state (a watermark, the names already reported, the one
 * prompt already raised) and can open a window.
 *
 * ONE MESSAGE PER FAILURE. A test that keeps failing is reported once; a NEW
 * name on a later poll is one more steer and never a second prompt; and the
 * third answer to the prompt silences the asking for the rest of the run while
 * the steer lines continue. A failure retried into a pass is not retracted —
 * the poll saw what it saw, and the chain's own verdict still reports the truth
 * (the pipeline retries each VSTest task twice).
 *
 * THE PROMPT NEVER HOLDS THE CHAIN. `reportTestFails` returns as soon as the
 * steer is out and offers the delegation in the background, so an unanswered
 * prompt cannot stop the polling that still has to deliver the terminal verdict.
 */

import { azdoBuildUrl } from "./azdo.js";
import { DEFAULT_TEST_ROLE, roleLaunch } from "./delegate.js";
import type { RoleLaunch, WindowSpawner } from "./delegate.js";
import { steerWarning, steerWatch } from "./report.js";
import { azdoTestFailScript, failedTestNames, parseTestLines } from "./test-scan.js";
import type { TestFailLine } from "./test-scan.js";

/** The role's launch triple and the role NAME live in `delegate.ts`, which owns
 *  the launch; this module only asks for one. */
export { DEFAULT_TEST_ROLE } from "./delegate.js";

const ASK_TITLE = "Tests failing — delegate the investigation?";
const ASK_KEEP = "Keep watching";
const ASK_MUTE = "Don't ask again this run";

/** What the watch remembers about this build's tests. Reset when a new watch
 *  starts: one watcher at a time, so in-memory is the whole lifetime. */
export interface TestWatch {
  /** Newest log change the last scan saw; passed back as `since`. */
  watermark: string | null;
  /** Every test name already reported for this build. */
  seen: Set<string>;
  /** The one delegation prompt has been raised for this build. */
  asked: boolean;
  /** The user answered "Don't ask again this run". */
  muted: boolean;
  /** A prompt is still open (answering it twice would spawn two windows). */
  pending: boolean;
  /** The scan failure already reported, so a dead token is one line not a
   *  line per poll. */
  scanError: string | null;
}

export function newTestWatch(): TestWatch {
  return { watermark: null, seen: new Set<string>(), asked: false, muted: false, pending: false, scanError: null };
}

/** Everything this module may do to the outside world, so a suite drives it
 *  without a pi, a window or a role store. */
export interface TestWatchIo {
  buildId: number;
  definition: string;
  role: string;
  notify: (msg: string, type: "info" | "warning") => Promise<void>;
  /** `urgent` spends the agent's next turn (a new failure); absent does not
   *  (the outcome of a delegation the agent just answered a prompt for). */
  steer: (msg: string, urgent?: boolean) => void;
  select: (title: string, options: string[]) => Promise<string | undefined>;
  /** True once the chain has ended — a prompt answered too late spawns nothing. */
  ended: () => boolean;
  spawn: (task: string, launch: RoleLaunch) => Promise<string | null>;
}

/** The names this scan adds to the report: parsed, deduped against everything
 *  already seen, and marked seen. */
export function newTestNames(watch: TestWatch, lines: TestFailLine[]): string[] {
  const fresh = failedTestNames(lines).filter((n) => !watch.seen.has(n));
  for (const n of fresh) watch.seen.add(n);
  return fresh;
}

/** The failure report: one warning toast, one steer, then the background offer. */
export async function reportTestFails(io: TestWatchIo, watch: TestWatch, names: string[]): Promise<void> {
  if (!names.length) return;
  const count = names.length === 1 ? "a test has failed" : `${names.length} tests have failed`;
  const msg = [`AzDO ${io.buildId}: ${count} — ${azdoBuildUrl(io.definition)}`, ...names.map((n) => `- ${n}`)].join("\n");
  await io.notify(msg, "warning");
  io.steer(msg, true);
  if (watch.muted || watch.asked || watch.pending) return;
  watch.asked = true;
  void offerDelegate(io, watch, [...watch.seen]);
}

/** One scan of the build's tests: read the records that moved since the last
 *  poll, advance the watermark, and report whatever is new. A read that throws
 *  or a refused call is a note, never a silence — the chain's own poll keeps
 *  running either way. */
export async function testScan(watch: TestWatch, io: TestWatchIo, read: (since: string) => Promise<{ stdout: string }>): Promise<void> {
  let stdout: string;
  try {
    stdout = (await read(watch.watermark ?? "")).stdout;
  } catch (err) {
    await noteScanError(io, watch, err instanceof Error ? err.message : String(err));
    return;
  }
  const poll = parseTestLines(stdout);
  if (poll.watermark) watch.watermark = poll.watermark;
  if (poll.kind === "error") {
    await noteScanError(io, watch, `${poll.failure}: ${poll.message ?? ""}`);
    return;
  }
  if (poll.kind === "lines") await reportTestFails(io, watch, newTestNames(watch, poll.lines));
}

/** A scan that could not be read: said ONCE. The chain's own poll reports a
 *  dead token too, so this is a note about the test scan, not a second alarm. */
export async function noteScanError(io: TestWatchIo, watch: TestWatch, message: string): Promise<void> {
  if (watch.scanError === message) return;
  watch.scanError = message;
  await io.notify(`AzDO ${io.buildId}: the test scan could not be read (${message}) — tests are still being watched, only this read failed.`, "warning");
}

/** The watcher's in-flight test poll, wired HERE so `watcher.ts` keeps one job
 *  (what the chain does next). `spawnWindow` defaults to the real psmux
 *  spawner; a suite injects its own through the watcher options. */
export interface TestPollArgs {
  watch: TestWatch;
  buildId: number;
  definition: string;
  testRole?: string;
  repoRoot?: string;
  ctx: any;
  pi: any;
  stopped: () => boolean;
  /** The port that opens the delegation window; the composition root wires it. */
  spawnWindow: WindowSpawner;
  /** The shell runner. The scan SCRIPT is built HERE: this module owns the test
   *  protocol end to end, so the watcher never sees its construction. */
  run: (cmd: string) => Promise<{ stdout: string }>;
}

export async function pollTestFails(a: TestPollArgs): Promise<void> {
  const io: TestWatchIo = {
    buildId: a.buildId,
    definition: a.definition,
    role: a.testRole ?? DEFAULT_TEST_ROLE,
    notify: (m, t) => a.ctx.ui.notify(m, t),
    steer: (m, urgent) => (urgent === false ? steerWatch(a.pi, m) : steerWarning(a.pi, m)),
    select: (t, o) => a.ctx.ui.select(t, o),
    ended: a.stopped,
    spawn: (task, launch) => a.spawnWindow(a.repoRoot ?? process.cwd(), task, launch),
  };
  await testScan(a.watch, io, (since) => a.run(azdoTestFailScript(a.buildId, since)));
}

/** The one offer. Runs detached from the poll: a prompt nobody answers must not
 *  hold the chain. */
async function offerDelegate(io: TestWatchIo, watch: TestWatch, names: string[]): Promise<void> {
  watch.pending = true;
  try {
    const delegate = `Delegate to ${io.role}`;
    const answer = await io.select(ASK_TITLE, [delegate, ASK_KEEP, ASK_MUTE]);
    if (answer === ASK_MUTE) {
      watch.muted = true;
      return;
    }
    if (answer !== delegate) return;
    await startDelegate(io, names);
  } finally {
    watch.pending = false;
  }
}

/** The delegate: a window booted as the role, told exactly what failed and the
 *  narrowed command to reproduce it. Every refusal is loud — a delegation that
 *  did not happen is never reported as one. */
async function startDelegate(io: TestWatchIo, names: string[]): Promise<void> {
  if (io.ended()) {
    await io.notify(`AzDO ${io.buildId}: the run ended before the delegation started — nothing was spawned.`, "info");
    return;
  }
  const launch = roleLaunch(io.role);
  if (!launch) {
    const msg = `AzDO ${io.buildId}: no ${io.role} role is configured in the model-role store, so no delegate was started. Assign it, then delegate from the menu.`;
    await io.notify(msg, "warning");
    io.steer(msg, false);
    return;
  }
  const brief = delegateBrief(io, names);
  let pane: string | null = null;
  try {
    pane = await io.spawn(brief, launch);
  } catch (err) {
    pane = null;
    await io.notify(`AzDO ${io.buildId}: opening the delegate window failed (${err instanceof Error ? err.message : String(err)}).`, "warning");
  }
  if (!pane) {
    const msg = `AzDO ${io.buildId}: no delegate window could be opened. Run this yourself:\npi --provider ${launch.provider} --model ${launch.model}${launch.thinkingLevel ? ` --thinking ${launch.thinkingLevel}` : ""} '${brief}'`;
    await io.notify(`AzDO ${io.buildId}: no delegate window could be opened — the failing tests are above.`, "warning");
    io.steer(msg, false);
    return;
  }
  const msg = `AzDO ${io.buildId}: delegated the failing tests to the ${io.role} role in window ${pane}.`;
  await io.notify(msg, "info");
  io.steer(msg, false);
}

/** What the delegate is told. The narrowed run is the rule this repo lives by
 *  (AGENTS.md): the whole suite is never the answer to one failing test. */
export function delegateBrief(io: TestWatchIo, names: string[]): string {
  const filter = names.map((n) => `FullyQualifiedName~${n}`).join("|");
  return [
    `AzDO build ${io.buildId} is failing tests right now: ${azdoBuildUrl(io.definition)}`,
    "Failing so far:",
    ...names.map((n) => `- ${n}`),
    "Run ONLY these locally, never the whole suite:",
    `dotnet test src\\Tests\\Tests.sln --settings Build\\Tests.runsettings --filter "${filter}"`,
    "Set TMP and TEMP to C:\\temp first: MSBuild reads an inherited b:\\Temp and dies with MSB1025 otherwise.",
    "Investigate the cause, fix it, and report which of these tests now passes.",
  ].join("\n");
}
