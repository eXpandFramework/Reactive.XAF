/**
 * steer-repro-run — scratch diagnostic (deleted after use). Spawns a pi
 * child with /steer-repro via runPi and reports whether the triggerTurn
 * steer starts an assistant turn (HAS_ASSISTANT) or the child goes idle
 * after the custom message (bug reproduced).
 */
import { runPi } from "C:/Users/Tolis/.pi/agent/extensions/pi-dev/pi-runner.js";

const r = runPi("/steer-repro", { cwd: "C:/Work/Reactive.XAF", timeoutSec: 180, maxBuffer: 8388608 });
console.log("STATUS=" + r.status);
const out = r.stdout || "";
console.log("OUT_LEN=" + out.length);
console.log("HAS_CUSTOM=" + out.includes("steer-repro:test"));
console.log("HAS_ASSISTANT=" + /assistant/.test(out));
console.log("OUT_TAIL=" + JSON.stringify(out.slice(-1800)));
console.log("ERR=" + JSON.stringify((r.stderr || "").slice(-1200)));
