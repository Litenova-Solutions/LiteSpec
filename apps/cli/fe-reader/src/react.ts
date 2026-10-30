// React route reading for TanStack file routes. A route entry is a file below
// the routes root that declares a route for an address. Pathless layout files
// (names starting with an underscore, including the root), TanStack ignored
// paths (segments starting with a dash), layout suffix files (*.route.*), lazy
// chunks (*.lazy.*) and error components (*.error.*) are not route entries.
// The address is the createFileRoute argument when present, else derived from
// the file name with groups, pathless segments and index files removed.

import { collectRouteCalls, CallScope } from "./calls.js";
import { baseName, listFilesRecursive, parentDir, relativeTo, toPosix } from "./files.js";
import { firstMarkLine, markValues, readFacts } from "./parse.js";
import { ReaderContext, RouteFacts } from "./protocol.js";

function withoutExtension(name: string): string {
  return name.endsWith(".tsx") ? name.slice(0, -4) : name.endsWith(".ts") ? name.slice(0, -3) : name;
}

function isRouteFile(routesAbs: string, fileAbs: string): boolean {
  const rel = relativeTo(routesAbs, fileAbs);
  if (rel === null) return false;
  const segments = rel.split("/");
  const name = segments[segments.length - 1];
  if (name === undefined || name === "") return false;
  if (name.startsWith("_")) return false;
  if (segments.some(segment => segment.startsWith("-"))) return false;
  const stem = withoutExtension(name);
  if (stem.endsWith(".lazy") || stem.endsWith(".error") || stem.endsWith(".route")) return false;
  return true;
}

function visibleAddress(route: string): string {
  const kept: string[] = [];
  for (const segment of route.split("/")) {
    if (segment === "") continue;
    if (/^\(.*\)$/.test(segment)) continue;
    if (segment.startsWith("_")) continue;
    if (segment === "index") continue;
    kept.push(segment);
  }
  return kept.length === 0 ? "/" : `/${kept.join("/")}`;
}

function derivedAddress(routesAbs: string, fileAbs: string): string {
  const rel = relativeTo(routesAbs, fileAbs);
  if (rel === null) return "/";
  const segments = rel.split("/");
  const last = segments.pop();
  if (last === undefined) return "/";
  segments.push(withoutExtension(last));
  const kept: string[] = [];
  for (const segment of segments) {
    if (/^\(.*\)$/.test(segment)) continue;
    if (segment.startsWith("_")) continue;
    if (segment === "index") continue;
    if (segment === "$") kept.push("[...rest]");
    else if (segment.startsWith("$")) kept.push(`[${segment.slice(1)}]`);
    else kept.push(segment);
  }
  return kept.length === 0 ? "/" : `/${kept.join("/")}`;
}

// Every React route entry below the routes root, sorted by file. A file that
// fails to parse keeps its derived address with no marks and no calls.
export function readReactRoutes(
  routesAbs: string,
  modulesAbs: string,
  scope: Omit<CallScope, "routeDirAbs" | "modulesAbs">,
  context: ReaderContext,
): RouteFacts[] {
  const routes: RouteFacts[] = [];
  const files = listFilesRecursive(routesAbs, [".ts", ".tsx"]).filter(file => isRouteFile(routesAbs, file));
  for (const fileAbs of files) {
    const dirAbs = parentDir(fileAbs);
    const file = relativeTo(context.root, fileAbs) ?? toPosix(fileAbs);
    const facts = readFacts(fileAbs, "ts", context);
    const marks = facts === null ? [] : markValues(facts.marks);
    const line = facts === null ? 1 : firstMarkLine(facts.marks);
    const address = facts !== null && facts.route !== null ? visibleAddress(facts.route) : derivedAddress(routesAbs, fileAbs);
    const calls = collectRouteCalls([{ absPath: fileAbs, dirAbs }], { ...scope, routeDirAbs: dirAbs, modulesAbs }, context);
    routes.push({ file, line, address, marks, calls });
  }
  routes.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  return routes;
}
