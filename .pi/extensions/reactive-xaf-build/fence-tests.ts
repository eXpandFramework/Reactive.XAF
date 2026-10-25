/**
 * reactive-xaf-build/fence-tests — behavior contract for the shell fence
 * (companion of fence.ts): a build that would run blind through a shell tool is
 * refused with a reason that names the door, and the fast local loops — one
 * project, or the documented filtered test run — still go through.
 *
 * Runtime: pi's OWN loader and ExtensionRunner through the resolver-first route
 * this file carries itself (`menu-tests.ts` established the shape), so every
 * case fires `tool_call` through pi's own dispatch. The gate is registered from
 * its real entry (`registerBuildFence`) — the same call index.ts makes at boot —
 * and nothing spawns: no pwsh, no psmux, no network.
 *
 * Why these cases and not more: the fence is a refusal, so the contract is
 * "this exact text is refused, that exact text is not". The command set itself
 * is derived from profile.ts (`buildCmd`/`queueCmd`), so a case per word is a
 * case per profile, not a transcription of the list.
 *
 * Run: npx tsx .pi/extensions/reactive-xaf-build/fence-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { registerBuildFence } from "./fence.js";

/** A crash or a rejected detached promise must be LOUD. Without this a route
 *  failure or a mid-suite death ends the file with nothing naming the case, and
 *  27 green is indistinguishable from 0 run — the trap `tool-tests.ts` paid for.
 *  The prologue is copied per suite on purpose: every suite here carries its own
 *  route rather than a shared helper (see `menu-tests.md`). */
process.on("uncaughtException", (err) => {
  console.log(`FAIL fence-tests uncaught: ${(err as Error)?.stack ?? err}`);
  process.exit(1);
});
process.on("unhandledRejection", (err) => {
  console.log(`FAIL fence-tests unhandled: ${(err as Error)?.stack ?? err}`);
  process.exit(1);
});

let passed = 0;
let failed = 0;

function check(desc: string, condition: boolean, detail?: string): void {
  if (condition) {
    passed++;
    console.log(`  PASS  ${desc}`);
    return;
  }
  failed++;
  console.log(`  FAIL  ${desc}${detail ? ` — ${detail}` : ""}`);
}

/** The harness's build function, filled in by installRoute(): its types sit
 *  behind a bare `@pi/` specifier a plain-tsx process cannot resolve, and a
 *  static import would be linked BEFORE the resolver hook exists. */
let buildRealRunner: (opts: { activate: (pi: any) => void; cwd?: string }) => Promise<any>;

async function installRoute(): Promise<void> {
  await import(new URL("./resolve.mjs", import.meta.url).href);
  buildRealRunner = (await import("@pi/pi-dev/real-runner.js")).buildRealRunner;
  await import("@pi/pi-dev/llm-utils.js"); // publishes __steer, as every boot does
}

interface Fired {
  block?: boolean;
  reason?: string;
}

(async () => {
  await installRoute();
  const handle = await buildRealRunner({ activate: (pi: any) => registerBuildFence(pi) });
  let fired = 0;
  /** Fire one tool call through pi's own dispatch. */
  const fire = (toolName: string, input?: Record<string, unknown>): Promise<Fired> =>
    handle.emitToolCall({ type: "tool_call", toolName, input, toolCallId: `fx-${(fired += 1)}` });

  try {
    // Section: the pipeline words are refused, and the reason names the door
    console.log("pipeline words refused");
    const direct = await fire("ctx_shell", { command: "brx" });
    check("T1 ctx_shell brx is refused", direct?.block === true, JSON.stringify(direct));
    check(
      "T1 the reason names the tool and the pane",
      typeof direct?.reason === "string" && direct.reason.includes("devexpress_build") &&
        direct.reason.includes("pane") && direct.reason.includes("AssemblyInfoVersion.cs"),
      JSON.stringify(direct?.reason),
    );

    // Section: bash is fenced identically — the surface is wider than one tool
    console.log("bash fenced identically");
    const viaBash = await fire("bash", { command: "brx" });
    check("T2 the same command through bash is refused", viaBash?.block === true, JSON.stringify(viaBash));
    check(
      "T2 bash carries the IDENTICAL reason, not merely a block",
      typeof viaBash?.reason === "string" && viaBash.reason === direct?.reason,
      `ctx_shell="${direct?.reason}" / bash="${viaBash?.reason}"`,
    );
    const release = await fire("bash", { command: "brx -Release" });
    check("T2 brx -Release is refused too", release?.block === true, JSON.stringify(release));

    // Section: a wrapper is unwrapped, never trusted
    console.log("wrappers unwrapped");
    const nested = await fire("ctx_shell", { command: 'pwsh -NoLogo -Command "brx"' });
    check("T3 pwsh -Command \"brx\" is refused", nested?.block === true, JSON.stringify(nested));
    const viaLean = await fire("lean_ctx", { args: ["exec", "brx"] });
    check("T4 lean_ctx exec brx is refused", viaLean?.block === true, JSON.stringify(viaLean));
    const wrapped = await fire("ctx_call", { name: "ctx_shell", arguments: { command: "brx" } });
    check("T4 ctx_call wrapping ctx_shell is refused", wrapped?.block === true, JSON.stringify(wrapped));

    // Section: the fast local loop still runs
    console.log("single-project build proceeds");
    const oneProject = await fire("ctx_shell", {
      command: "dotnet build src\\Modules\\Reactive\\Reactive.csproj",
    });
    check("T5 a .csproj build is not refused", oneProject?.block !== true, JSON.stringify(oneProject));

    // Section: solutions, scripts and the redirected shape
    console.log("solutions and scripts refused");
    for (const [desc, command] of [
      ["Extensions.sln", "dotnet build src\\Extensions\\Extensions.sln"],
      ["Modules.sln", "dotnet build src\\Modules\\Modules.sln"],
      ["Tests.sln unfiltered", "dotnet test src\\Tests\\Tests.sln"],
      ["go.ps1", "./go.ps1"],
      ["BuildPipeline.ps1", "pwsh -File Build\\BuildPipeline.ps1"],
      ["BuildDevExpress.XAF.ps1", "pwsh -File Build\\BuildDevExpress.XAF.ps1"],
      ["QuickBuild.ps1", "C:\\Work\\Reactive.XAF\\QuickBuild.ps1"],
      ["cd then brx", "cd src && brx"],
    ] as Array<[string, string]>) {
      const result = await fire("ctx_shell", { command });
      check(`T6 ${desc} is refused`, result?.block === true, JSON.stringify(result));
    }
    const narrowed = await fire("ctx_shell", {
      command: "dotnet test src\\Tests\\Tests.sln --settings Build\\Tests.runsettings --filter FullyQualifiedName~Login",
    });
    check(
      "T6 the documented narrowed test run proceeds",
      narrowed?.block !== true,
      JSON.stringify(narrowed),
    );
    const projectTest = await fire("ctx_shell", {
      command: "dotnet test src\\Modules\\Reactive\\Xpand.XAF.Modules.Reactive.csproj",
    });
    check("T6 a single-project test run proceeds", projectTest?.block !== true, JSON.stringify(projectTest));
    const redirectedProject = await fire("ctx_shell", {
      command: "dotnet build src\\Modules\\Reactive\\Reactive.csproj > b.log 2>&1",
    });
    check(
      "T6 a redirected single-project build proceeds",
      redirectedProject?.block !== true,
      JSON.stringify(redirectedProject),
    );
    const redirectedPipeline = await fire("bash", {
      command: "dotnet build src\\Modules\\Modules.sln > b.log 2>&1",
    });
    check(
      "T6 a redirected pipeline build names the log in its reason",
      redirectedPipeline?.block === true && String(redirectedPipeline.reason).includes("log file"),
      JSON.stringify(redirectedPipeline?.reason),
    );

    // Section: an argument that merely spells a word is not a command
    console.log("no false positives");
    for (const [desc, command] of [
      ["echo", 'echo "brx"'],
      ["git log", "git log --oneline brx"],
      ["cat a script", "cat Build/BuildPipeline.ps1"],
      ["dotnet run", "dotnet run --project src\\Modules\\Reactive\\Reactive.csproj"],
      ["git log redirected", "git log > out.txt"],
    ] as Array<[string, string]>) {
      const result = await fire("ctx_shell", { command });
      check(`T7 ${desc} proceeds`, result?.block !== true, JSON.stringify(result));
    }

    // Section: the fence owns shell calls only
    console.log("non-shell surface untouched");
    const write = await fire("write", { path: "a.txt", content: "brx" });
    check("T8 a write is untouched", write?.block !== true, JSON.stringify(write));
    const empty = await fire("ctx_shell", { command: "" });
    check("T8 an empty command passes through", empty?.block !== true, JSON.stringify(empty));
    const noInput = await fire("ctx_shell");
    check("T8 a command-less shell call passes through", noInput?.block !== true, JSON.stringify(noInput));
  } catch (err) {
    failed++;
    console.log(`FAIL fence-tests crashed: ${(err as Error)?.stack ?? err}`);
  } finally {
    handle.dispose();
  }

  // Section: Summary separator
  const total = passed + failed;
  console.log(`\n${"=".repeat(40)}`);
  console.log(`${passed}/${total} passed`);
  if (failed > 0) {
    console.log(`${failed} FAILED`);
    process.exitCode = 1;
  }
  // Section: All fence tests passed
  else console.log("All fence tests passed.");
})();
