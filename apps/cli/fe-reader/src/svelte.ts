// Use-case module reading (both web stacks) and Svelte route reading. A module
// entry comes from a use-case folder below the modules root: a module-level
// use case two levels deep, {module}/{use-case}/, or an aggregate use case
// three levels deep, {module}/{aggregate}/{use-case}/; its marks are every
// implements value of every exported litespec constant. A litespec.ts
// elsewhere names no module. A Svelte route entry is a folder
// holding a +page.svelte; its mark comes from the module script of the
// +page.svelte, because SvelteKit refuses any other export from a +page.ts.
// Each route also counts the calls and navigations of its ancestor layouts,
// the +layout.ts and +layout.server.ts files above it, with the frame modules
// those layouts import.

import { collectLayoutCalls, collectLayoutNavigations, collectRouteCalls, collectRouteNavigations, CallScope, RouteEntry } from "./calls";
import { baseName, canonical, isFile, isWithin, joinPath, listDirsRecursive, listFilesRecursive, parentDir, relativeTo, toPosix } from "./files";
import { firstMarkLine, markValues, readFacts } from "./parse";
import { CallFacts, ModuleFacts, NavigationFacts, ReaderContext, RouteFacts } from "./protocol";

export interface ModuleScan {
  modules: ModuleFacts[];
  folders: Set<string>;
}

// Every use-case folder below the modules root: a folder two levels deep,
// {module}/{use-case}/, holding a mark file or no child folder, or a folder
// three levels deep, {module}/{aggregate}/{use-case}/, holding a mark file.
// A two-level folder with child folders and no mark file is an aggregate
// container, and a three-level folder without a mark file reads as a partials
// folder: neither names a module. A litespec.ts at any other depth names no
// module. Entries come out sorted by folder.
export function readModules(modulesAbs: string, context: ReaderContext): ModuleScan {
  const modules: ModuleFacts[] = [];
  const folders = new Set<string>();
  for (const { dir, hasChildDir } of listDirsRecursive(modulesAbs)) {
    const rel = relativeTo(modulesAbs, dir);
    const depth = rel === null ? 0 : rel.split("/").length;
    const hasMarkFile = isFile(joinPath(dir, "litespec.ts"));
    const isModule = (depth === 2 && (hasMarkFile || !hasChildDir)) || (depth === 3 && hasMarkFile);
    if (!isModule) continue;
    folders.add(canonical(dir));
    const folder = relativeTo(context.root, dir) ?? toPosix(dir);
    const markAbs = joinPath(dir, "litespec.ts");
    if (!hasMarkFile) {
      modules.push({ folder, file: `${folder}/litespec.ts`, line: 1, marks: [] });
      continue;
    }
    const facts = readFacts(markAbs, "ts", context);
    if (facts === null) {
      modules.push({ folder, file: relativeTo(context.root, markAbs) ?? toPosix(markAbs), line: 1, marks: [] });
      continue;
    }
    modules.push({
      folder,
      file: relativeTo(context.root, markAbs) ?? toPosix(markAbs),
      line: firstMarkLine(facts.marks),
      marks: markValues(facts.marks),
    });
  }
  modules.sort((a, b) => (a.folder < b.folder ? -1 : a.folder > b.folder ? 1 : 0));
  return { modules, folders };
}

function routeAddress(routesAbs: string, pageDirAbs: string): string {
  const rel = relativeTo(routesAbs, pageDirAbs);
  if (rel === null || rel === "") return "/";
  const kept = rel.split("/").filter(segment => !/^\(.*\)$/.test(segment));
  return kept.length === 0 ? "/" : `/${kept.join("/")}`;
}

// The layout entries above a route folder: the +layout.ts and +layout.server.ts
// at each level from the route folder up to the routes root. Layouts nest, so
// a route counts every ancestor layout's contribution.
function layoutEntries(routesAbs: string, dirAbs: string): RouteEntry[] {
  const entries: RouteEntry[] = [];
  let current = dirAbs;
  while (isWithin(routesAbs, current)) {
    for (const name of ["+layout.ts", "+layout.server.ts"]) {
      const absPath = joinPath(current, name);
      if (isFile(absPath)) entries.push({ absPath, dirAbs: current });
    }
    if (canonical(current) === canonical(routesAbs)) break;
    const parent = parentDir(current);
    if (parent === current) break;
    current = parent;
  }
  return entries;
}

function byFileLine(a: { file: string; line: number }, b: { file: string; line: number }): number {
  return a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line;
}

// Every Svelte route entry: each folder below the routes root holding a
// +page.svelte. Entries come out sorted by file. Each route counts its own
// calls and navigations plus every ancestor layout's, with the frame modules
// the layouts import.
export function readSvelteRoutes(
  routesAbs: string,
  modulesAbs: string,
  frameAbs: string,
  scope: Omit<CallScope, "routeDirAbs" | "modulesAbs">,
  context: ReaderContext,
): RouteFacts[] {
  const routes: RouteFacts[] = [];
  const pages = listFilesRecursive(routesAbs, [".svelte"]).filter(page => baseName(page) === "+page.svelte");
  for (const pageAbs of pages) {
    const dirAbs = parentDir(pageAbs);
    const scriptAbs = joinPath(dirAbs, "+page.ts");
    const hasScript = isFile(scriptAbs);
    const file = relativeTo(context.root, pageAbs) ?? toPosix(pageAbs);
    let line = 1;
    let marks: string[] = [];
    const facts = readFacts(pageAbs, "svelte", context);
    if (facts !== null) {
      line = firstMarkLine(facts.marks);
      marks = markValues(facts.marks);
    }
    const entries = hasScript
      ? [
        { absPath: pageAbs, dirAbs },
        { absPath: scriptAbs, dirAbs },
      ]
      : [{ absPath: pageAbs, dirAbs }];
    const layouts = layoutEntries(routesAbs, dirAbs);
    const layoutScope = { frameAbs, operations: scope.operations, aliases: scope.aliases };
    const calls: CallFacts[] = [
      ...collectRouteCalls(entries, { ...scope, routeDirAbs: dirAbs, modulesAbs }, context),
      ...collectLayoutCalls(layouts, layoutScope, context),
    ].sort(byFileLine);
    const navigations: NavigationFacts[] = [
      ...collectRouteNavigations(entries, { ...scope, routeDirAbs: dirAbs, modulesAbs }, context),
      ...collectLayoutNavigations(layouts, layoutScope, context),
    ].sort(byFileLine);
    routes.push({
      file,
      line,
      address: routeAddress(routesAbs, dirAbs),
      marks,
      calls,
      navigations,
    });
  }
  routes.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  return routes;
}
