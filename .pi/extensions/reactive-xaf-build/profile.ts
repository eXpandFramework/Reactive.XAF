/**
 * reactive-xaf-build/profile — the RepoProfile the /devexpress engine consumes.
 *
 * RX is the default. Expand is a second object of the same shape. The engine
 * never branches on repo id. Build/Publish always pick RX-XAF | eXpand, then
 * Lab | Release. resolveRepo uses known roots only when cwd is the other repo
 * (a tmpdir fixture does not leak to C:/Work).
 */

import * as fs from "node:fs";
import * as path from "node:path";

export type Choice = "Lab" | "Release";
export type GithubOnSuccess = "publishDraft" | "assertPublished";
export type NugetFeed = "xpand-server" | "nuget.org";

export interface ChainStep {
  definition: string;
  label: string;
  assertNugets?: boolean;
}

export interface DepPins {
  prefixes: string[];
  discoverId: string;
}

export interface RepoProfile {
  label: string;
  menuProjectPick: string;
  versionFile: string;
  nugetId: string;
  knownRoots: string[];
  depPins?: DepPins;
  detect(cwd: string): string | null;
  buildCmd(choice: Choice): string;
  queueCmd(choice: Choice): string;
  queueLabel(choice: Choice): string;
  pushRemote(choice: Choice): string | null;
  chain(choice: Choice): ChainStep[];
  statusDef(choice: Choice): string;
  githubRepo(choice: Choice): string;
  githubOnSuccess(choice: Choice): GithubOnSuccess;
  nugetFeed(choice: Choice): NugetFeed;
}

export const XPAND_NUGET_SERVER = "https://xpandnugetserver.azurewebsites.net/nuget";

export function profileOf(seams: { profile?: RepoProfile }): RepoProfile {
  return seams.profile ?? rxProfile;
}

export function profileByPick(pick: string): RepoProfile {
  if (pick === expandProfile.menuProjectPick) return expandProfile;
  return rxProfile;
}

function detectAt(cwd: string, marker: string): string | null {
  const p = path.resolve(cwd);
  const hasProps = fs.existsSync(path.join(p, "Directory.Packages.props"));
  const hasMarker = fs.existsSync(path.join(p, marker));
  return hasProps && hasMarker ? p : null;
}

export function resolveRepo(profile: RepoProfile, cwd: string): string | null {
  const here = profile.detect(cwd);
  if (here) return here;
  const other = profile === rxProfile ? expandProfile : rxProfile;
  if (!other.detect(cwd)) return null;
  for (const root of profile.knownRoots) {
    const hit = profile.detect(root);
    if (hit) return hit;
  }
  return null;
}

function rxChain(choice: Choice): ChainStep[] {
  return [
    { definition: "23", label: choice === "Release" ? "Reactive.XAF Release build" : "Reactive.XAF build" },
    { definition: "72", label: "nuget publish pipeline", assertNugets: true },
    { definition: "89", label: "release consumers pipeline" },
  ];
}

function expandChain(choice: Choice): ChainStep[] {
  const head = choice === "Release"
    ? { definition: "39", label: "eXpand Release build" }
    : { definition: "94", label: "eXpand Lab build" };
  return [
    head,
    { definition: "38", label: "nuget publish pipeline", assertNugets: true },
    { definition: "37", label: "release consumers pipeline" },
  ];
}

export const rxProfile: RepoProfile = {
  label: "Reactive.XAF",
  menuProjectPick: "RX-XAF",
  versionFile: path.join("src", "Common", "AssemblyInfoVersion.cs"),
  nugetId: "xpand.extensions",
  knownRoots: ["C:/Work/Reactive.XAF", "D:/Reactive.XAF"],
  detect: (cwd) => detectAt(cwd, path.join("src", "Extensions")),
  buildCmd: (choice) => choice === "Release" ? "brx -Release" : "brx",
  queueCmd: (choice) => choice === "Release" ? "prx -Release" : "prx",
  queueLabel: (choice) => choice === "Release"
    ? "prx -Release (stage, force-push lab:master, queue def 23 on master)"
    : "prx (stage, force-push, queue AzDO Reactive.XAF)",
  pushRemote: () => null,
  chain: rxChain,
  statusDef: () => "23",
  githubRepo: () => "eXpandFramework/Reactive.XAF",
  githubOnSuccess: () => "publishDraft",
  nugetFeed: (choice) => choice === "Release" ? "nuget.org" : "xpand-server",
};

export const expandProfile: RepoProfile = {
  label: "eXpand",
  menuProjectPick: "eXpand",
  versionFile: path.join("Xpand", "Xpand.Utils", "Properties", "XpandAssemblyInfo.cs"),
  nugetId: "eXpandSystem",
  knownRoots: ["C:/Work/expand", "D:/expand"],
  depPins: {
    prefixes: ["Xpand.Extensions", "Xpand.XAF."],
    discoverId: "Xpand.Extensions",
  },
  detect: (cwd) => detectAt(cwd, path.join("Xpand", "Xpand.ExpressApp.Modules")),
  buildCmd: (choice) => choice === "Release" ? "bx Release" : "bx lab",
  queueCmd: (choice) => choice === "Release" ? "px -Release" : "px",
  queueLabel: (choice) => choice === "Release"
    ? "git push eXpand then px -Release (queue Xpand-Release def 39)"
    : "git push lab then px (queue Xpand-Lab def 94)",
  pushRemote: (choice) => choice === "Release" ? "eXpand" : "lab",
  chain: expandChain,
  statusDef: (choice) => choice === "Release" ? "39" : "94",
  githubRepo: (choice) => choice === "Release"
    ? "eXpandFramework/eXpand"
    : "eXpandFramework/eXpand.lab",
  githubOnSuccess: (choice) => choice === "Release" ? "publishDraft" : "assertPublished",
  nugetFeed: (choice) => choice === "Release" ? "nuget.org" : "xpand-server",
};

export function nugetAssertUrl(nugetId: string): string {
  return `${XPAND_NUGET_SERVER}/FindPackagesById()?id=%27${nugetId}%27`;
}

export function nugetOrgNuspecUrl(nugetId: string, version: string): string {
  const id = nugetId.toLowerCase();
  return `https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.nuspec`;
}

export function githubReleasesUrl(repo: string): string {
  return `https://api.github.com/repos/${repo}/releases?per_page=50`;
}

export function githubReleaseUrl(repo: string, id: number): string {
  return `https://api.github.com/repos/${repo}/releases/${id}`;
}

export function compareVersions(a: string, b: string): number {
  const pa = a.split(".").map(Number);
  const pb = b.split(".").map(Number);
  const n = Math.max(pa.length, pb.length);
  for (let i = 0; i < n; i++) {
    const da = pa[i] ?? 0;
    const db = pb[i] ?? 0;
    if (da !== db) return da - db;
  }
  return 0;
}

function maxVersion(versions: string[]): string {
  if (!versions.length) throw new Error("no versions on the feed");
  const sorted = [...versions].sort((a, b) => compareVersions(b, a));
  return sorted[0];
}

export async function getLatestPackage(
  fetchFeed: (url: string) => Promise<string>,
  id: string,
  feed: NugetFeed,
): Promise<string> {
  if (feed === "nuget.org") {
    const text = await fetchFeed(`https://api.nuget.org/v3-flatcontainer/${id.toLowerCase()}/index.json`);
    const versions = (JSON.parse(text).versions as string[]).filter((v) => /^\d+(\.\d+)*$/.test(v));
    return maxVersion(versions);
  }
  const text = await fetchFeed(nugetAssertUrl(id));
  const versions = [...text.matchAll(/Version='([^']+)'/g)].map((m) => m[1]);
  return maxVersion(versions);
}

/** Next release version on the DX minor: max(dxBase.Build, last published build + 1), revision 0.
 *  Published versions on other DX minors are ignored (26.1.400.0 + [26.1.400, 26.1.301] → 26.1.401.0). */
export function nextReleaseVersion(dxBase: string, published: string[]): string {
  const base = dxBase.split(".").map(Number);
  const minorKey = `${base[0]}.${base[1]}`;
  let build = base[2] ?? 0;
  for (const v of published) {
    const p = v.split(".").map(Number);
    if (p.length < 3 || `${p[0]}.${p[1]}` !== minorKey) continue;
    const pb = p[2] ?? 0;
    if (pb >= build) build = pb + 1;
  }
  return `${base[0]}.${base[1]}.${build}.0`;
}

export function readPrefixedPins(text: string, prefixes: string[]): { count: number; unique: string | null; ids: string[] } {
  const versions = new Set<string>();
  const ids: string[] = [];
  const re = /Include="([^"]*)"\s+Version="([^"]*)"/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(text)) !== null) {
    if (!prefixes.some((p) => m![1] === p || m![1].startsWith(p))) continue;
    versions.add(m[2]);
    ids.push(m[1]);
  }
  return { count: versions.size, unique: versions.size === 1 ? [...versions][0] : null, ids };
}

export function rewritePrefixedPins(text: string, prefixes: string[], newVersion: string): string {
  return text.replace(/Include="([^"]*)"(\s+Version=")([^"]*)(")/g, (full, id, mid, _old, end) => {
    if (!prefixes.some((p) => id === p || id.startsWith(p))) return full;
    return `Include="${id}"${mid}${newVersion}${end}`;
  });
}
