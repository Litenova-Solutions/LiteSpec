// Use-case module reading (both web stacks) and Svelte route reading. A module
// entry comes from a folder holding a litespec.ts, a leaf folder three levels
// deep, or a reads/ folder; its marks are every implements value of every
// exported litespec constant. A Svelte route entry is a folder holding a
// +page.svelte; its mark comes from the +page.ts beside it when one exists,
// else from the module script of the +page.svelte.

import { collectRouteCalls, CallScope } from "./calls.js";
import { baseName, canonical, isFile, joinPath, listDirsRecursive, listFilesRecursive, parentDir, relativeTo, toPosix } from "./files.js";
import { firstMarkLine, markValues, readFacts } from "./parse.js";
import { ModuleFacts, ReaderContext, RouteFacts } from "./protocol.js";

export interface ModuleScan {
  modules: ModuleFacts[];
  folders: Set<string>;
}

// Every use-case folder below the modules root: a folder holding a litespec.ts
// (any depth), a leaf folder three levels deep, or a {module}/reads/{use-case}
// folder. Entries come out sorted by folder.
export function readModules(modulesAbs: string, context: ReaderContext): ModuleScan {
  const modules: ModuleFacts[] = [];
  const folders = new Set<string>();
  for (const { dir, hasChildDir } of listDirsRecursive(modulesAbs)) {
    const rel = relativeTo(modulesAbs, dir);
    const segments = rel === null ? [] : rel.split("/");
    const depth = rel === null ? 0 : segments.length;
    const hasMarkFile = isFile(joinPath(dir, "litespec.ts"));
    const isReadsFolder = depth === 3 && segments[1] === "reads";
    if (!hasMarkFile && !(depth === 3 && !hasChildDir) && !isReadsFolder) continue;
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

// Every Svelte route entry: each folder below the routes root holding a
// +page.svelte. Entries come out sorted by file.
export function readSvelteRoutes(
  routesAbs: string,
  modulesAbs: string,
  useCaseFolders: Set<string>,
  scope: Omit<CallScope, "routeDirAbs" | "modulesAbs" | "useCaseFolders">,
  context: ReaderContext,
): RouteFacts[] {
  const routes: RouteFacts[] = [];
  const pages = listFilesRecursive(routesAbs, [".svelte"]).filter(page => baseName(page) === "+page.svelte");
  for (const pageAbs of pages) {
    const dirAbs = parentDir(pageAbs);
    const scriptAbs = joinPath(dirAbs, "+page.ts");
    const hasScript = isFile(scriptAbs);
    const file = relativeTo(context.root, hasScript ? scriptAbs : pageAbs) ?? toPosix(hasScript ? scriptAbs : pageAbs);
    let line = 1;
    let marks: string[] = [];
    if (hasScript) {
      const facts = readFacts(scriptAbs, "ts", context);
      if (facts !== null) {
        line = firstMarkLine(facts.marks);
        marks = markValues(facts.marks);
      }
    } else {
      const facts = readFacts(pageAbs, "svelte", context);
      if (facts !== null) {
        line = firstMarkLine(facts.marks);
        marks = markValues(facts.marks);
      }
    }
    const entries = hasScript
      ? [
        { absPath: pageAbs, dirAbs },
        { absPath: scriptAbs, dirAbs },
      ]
      : [{ absPath: pageAbs, dirAbs }];
    const calls = collectRouteCalls(entries, { ...scope, routeDirAbs: dirAbs, modulesAbs, useCaseFolders }, context);
    routes.push({
      file,
      line,
      address: routeAddress(routesAbs, dirAbs),
      marks,
      calls,
    });
  }
  routes.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
  return routes;
}
