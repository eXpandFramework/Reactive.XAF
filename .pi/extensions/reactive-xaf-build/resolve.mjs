/**
 * resolve.mjs — the module-resolution rules this project's suites need from a
 * plain-Node host. Adapted, deliberately near-verbatim, from the same-named
 * file in the expenses tree (.pi/extensions/expenses/resolve.mjs): the two are
 * the same mechanism and must stay in step.
 *
 * Why it exists: the shared test harness (pi-dev's real-runner, boot-proof,
 * llm-utils, tracked-write) lives in the home pi tree, which this repo cannot
 * reach by a relative import — another drive. Two rules close that gap:
 *
 *   1. a `.js` specifier maps onto its `.ts` sibling, which pi's extension
 *      loader does and tsx does for in-tree imports;
 *   2. `@pi/<name>/...` (and `@pi/contracts/<file>`) resolves into the
 *      platform's agent tree, GATED by ~/.pi/agent/extensions/shared-utilities
 *      .json — the prefix is a whitelist over the platform's own list, never a
 *      hole into the install.
 *
 * A suite installs it as the first statement of its async main:
 *
 *   await import(new URL("./resolve.mjs", import.meta.url).href);
 *
 * and then DYNAMIC-imports `@pi/...` names. A static import is linked before
 * any hook can exist, so a harness import can never be static. Under pi itself
 * neither rule is needed: pi's loader resolves both.
 *
 * The file only reads the shared list and answers resolution questions. It
 * writes nothing, opens no socket and reaches no network.
 */

import { existsSync, readFileSync } from "node:fs";
import { registerHooks } from "node:module";
import { homedir } from "node:os";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

const PI_AGENT = join(homedir(), ".pi", "agent");
const EXTENSIONS = join(PI_AGENT, "extensions");
const SHARED_LIST = join(EXTENSIONS, "shared-utilities.json");

/** The names the platform shares. A malformed list shares nothing, loudly. */
function sharedNames() {
  try {
    const raw = JSON.parse(readFileSync(SHARED_LIST, "utf8"));
    return Array.isArray(raw.shared) ? raw.shared.map(String) : [];
  } catch (err) {
    process.stderr.write(`resolve.mjs: the shared list is unreadable, no name resolves: ${String(err)}\n`);
    return [];
  }
}

const NAMES = sharedNames();

/** The shapes a module tail may name: the file itself, its `.ts` sibling when
 *  the specifier ends in `.js`, and the directory's own index. The pi loader
 *  maps a `.js` onto its `.ts` sibling, so this has to as well, or
 *  `@pi/pi-dev/llm-utils.js` resolves to nothing and the host dies at load. */
function moduleCandidates(base, tail) {
  if (tail === "") return [join(base, "index.ts")];
  const named = join(base, tail);
  const candidates = [named];
  if (tail.endsWith(".js")) candidates.push(`${named.slice(0, -3)}.ts`);
  candidates.push(`${named}.ts`, join(named, "index.ts"));
  return candidates;
}

/** `@pi/<ext>` or `@pi/contracts/<file>` to a real file, or null. */
function sharedTarget(specifier) {
  const rest = specifier.slice("@pi/".length);
  const cut = rest.indexOf("/");
  const name = cut === -1 ? rest : rest.slice(0, cut);
  if (name !== "contracts" && !NAMES.includes(name)) return null;
  const base = name === "contracts" ? join(PI_AGENT, "contracts") : join(EXTENSIONS, name);
  const tail = cut === -1 ? "" : rest.slice(cut + 1);
  for (const candidate of moduleCandidates(base, tail)) {
    if (existsSync(candidate)) return candidate;
  }
  return null;
}

registerHooks({
  resolve(specifier, context, nextResolve) {
    if (specifier.startsWith("@pi/")) {
      const target = sharedTarget(specifier);
      if (target) return nextResolve(pathToFileURL(target).href, context);
    }
    try {
      return nextResolve(specifier, context);
    } catch (err) {
      if (specifier.endsWith(".js")) {
        return nextResolve(`${specifier.slice(0, -3)}.ts`, context);
      }
      throw err;
    }
  },
});
