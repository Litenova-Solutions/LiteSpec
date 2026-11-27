// Generated-client operation discovery and per-route call collection. An
// operation is a named function export of the generated client folder,
// reported as the client exports it, except for the generator's bundled
// runtime under client/ and core/. Calls are collected from the route entry
// files and from every file reached through relative and alias imports within the
// route's own folder, the frame and the use-case modules, following imports
// transitively with cycle protection. Shared packages are never followed:
// only relative and alias imports resolve to project files. Layout files
// contribute separately: each layout counts its own calls and the calls of
// every file reached through the frame and the use-case modules it imports
// for every route under it.

import { absolute, baseName, canonical, isFile, isWithin, joinPath, listFilesRecursive, parentDir, relativeTo, toPosix } from "./files";
import { FileFacts, FileKind, readFacts } from "./parse";
import { CallFacts, NavigationFacts, ReaderContext } from "./protocol";

export function toPascalCase(name: string): string {
  return name
    .split(/[-_]+/)
    .filter(part => part.length > 0)
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

// The called name with a trailing Async removed, so a listExpensesAsync call
// matches the listExpenses operation.
function stripAsync(name: string): string {
  return name.length > 5 && name.endsWith("Async") ? name.slice(0, -5) : name;
}

function operationKey(name: string): string {
  return toPascalCase(stripAsync(name));
}

function kindOf(absPath: string): FileKind {
  return baseName(absPath).endsWith(".svelte") ? "svelte" : "ts";
}

// Longest first, so InfiniteQueryKey is tried before QueryKey.
const QUERY_HELPER_SUFFIXES = ["InfiniteQueryKey", "InfiniteOptions", "QueryKey", "Options", "Mutation"];

// The generated client folder also holds the generator's fetch runtime: hey-api
// writes createConfig, buildUrl and createClient into *.gen.ts files under
// client/ and core/ folders. Those are helpers, not API operations, so a screen
// that builds its client with them must never be reported as calling an
// unrecorded operation. Skip every generated file below a client/ or core/
// folder; operations come from the SDK and the query plugin output only.
const GENERATOR_RUNTIME_FOLDERS = new Set(["client", "core"]);

function isGeneratorRuntimeFile(apiClientAbs: string, file: string): boolean {
  const rel = relativeTo(apiClientAbs, file);
  if (rel === null) return false;
  const folders = rel.split("/");
  folders.pop(); // the last segment is the file name, not a folder
  return folders.some(folder => GENERATOR_RUNTIME_FOLDERS.has(folder));
}

// Every operation the generated client folder exports, keyed by its
// PascalCase form and reported as the client exports it. Only the files the
// client generator writes (named *.gen.ts) hold operations, and the runtime
// folders above are skipped, so a hand-written helper beside them is never
// mistaken for one. A client file that fails to parse is diagnosed and
// contributes no operations.
export function collectOperations(apiClientAbs: string, context: ReaderContext): Map<string, string> {
  const operations = new Map<string, string>();
  for (const file of listFilesRecursive(apiClientAbs, [".ts"])) {
    if (!/\.gen\.ts$/.test(file)) continue;
    if (isGeneratorRuntimeFile(apiClientAbs, file)) continue;
    const facts = readFacts(file, "ts", context);
    if (facts === null) continue;
    for (const name of facts.functions) {
      const key = operationKey(name);
      if (key !== "" && !operations.has(key)) operations.set(key, name);
    }
  }
  // A query-library plugin of the client generator wraps each operation in
  // helpers named after it (listExpensesOptions, listExpensesQueryKey,
  // submitExpenseMutation). A call to a helper is a call to its operation, so
  // the helper reports the operation it wraps.
  for (const [key] of operations) {
    for (const suffix of QUERY_HELPER_SUFFIXES) {
      if (!key.endsWith(suffix) || key.length === suffix.length) continue;
      const wrapped = operations.get(key.slice(0, -suffix.length));
      if (wrapped !== undefined) {
        operations.set(key, wrapped);
        break;
      }
    }
  }
  return operations;
}


function probeCandidates(resolved: string): string | null {
  const candidates = [
    resolved,
    `${resolved}.ts`,
    `${resolved}.svelte`,
    joinPath(resolved, "index.ts"),
  ];
  if (resolved.endsWith(".js")) candidates.push(`${resolved.slice(0, -3)}.ts`);
  for (const candidate of candidates) {
    if (isFile(candidate)) return candidate;
  }
  return null;
}

// Resolves a relative or alias import to an absolute file path, or null when
// the specifier is bare or has no file on disk.
export function resolveImport(specifier: string, importerDirAbs: string, aliases: Record<string, string>): string | null {
  let resolved: string | null = null;
  if (specifier.startsWith("./") || specifier.startsWith("../") || specifier === "." || specifier === "..") {
    resolved = absolute(joinPath(importerDirAbs, specifier));
  } else {
    let bestKey = "";
    for (const key of Object.keys(aliases)) {
      if ((specifier === key || specifier.startsWith(`${key}/`)) && key.length > bestKey.length) bestKey = key;
    }
    if (bestKey !== "") {
      const target = aliases[bestKey];
      if (target !== undefined && target !== "") resolved = absolute(target + specifier.slice(bestKey.length));
    }
  }
  if (resolved === null) return null;
  return probeCandidates(resolved);
}

export interface RouteEntry {
  absPath: string;
  dirAbs: string;
}

export interface CallScope {
  routeDirAbs: string;
  modulesAbs: string;
  frameAbs: string;
  operations: Map<string, string>;
  aliases: Record<string, string>;
}

export interface LayoutScope {
  frameAbs: string;
  modulesAbs: string;
  operations: Map<string, string>;
  aliases: Record<string, string>;
}

interface VisitedFile {
  absPath: string;
  dirAbs: string;
  facts: FileFacts;
}

// Every file reached from the route entries, following imports within the
// route folder, the frame and the modules root: a route reaches its modules
// and the frame helpers it uses, and a module may compose another use case's
// module, which the screen then offers too. Shared packages are never
// followed. Files are visited once.
function walkRouteFiles(entries: RouteEntry[], scope: CallScope, context: ReaderContext): VisitedFile[] {
  const found: VisitedFile[] = [];
  const inScope = (absPath: string): boolean =>
    isWithin(scope.routeDirAbs, absPath) || isWithin(scope.modulesAbs, absPath) || isWithin(scope.frameAbs, absPath);

  const visited = new Set<string>();
  const queue: VisitedFile[] = [];
  for (const entry of entries) {
    if (visited.has(canonical(entry.absPath))) continue;
    visited.add(canonical(entry.absPath));
    const facts = readFacts(entry.absPath, kindOf(entry.absPath), context);
    if (facts !== null) queue.push({ absPath: entry.absPath, dirAbs: entry.dirAbs, facts });
  }
  while (queue.length > 0) {
    const current = queue.shift();
    if (current === undefined) break;
    found.push(current);
    for (const specifier of current.facts.imports) {
      const target = resolveImport(specifier, current.dirAbs, scope.aliases);
      if (target === null || visited.has(canonical(target)) || !inScope(target)) continue;
      visited.add(canonical(target));
      const facts = readFacts(target, kindOf(target), context);
      if (facts !== null) queue.push({ absPath: target, dirAbs: parentDir(target), facts });
    }
  }
  return found;
}

// Every file reached from the layout entries: the layout files themselves and
// every file reached through the frame and the modules root, as far as calls
// are followed for routes: a layout reaches the frame helpers it uses and the
// use-case modules those helpers read. Route folders and shared packages are
// never followed from a layout. Files are visited once.
function walkLayoutFiles(entries: RouteEntry[], scope: LayoutScope, context: ReaderContext): VisitedFile[] {
  const found: VisitedFile[] = [];
  const inScope = (absPath: string): boolean =>
    isWithin(scope.frameAbs, absPath) || isWithin(scope.modulesAbs, absPath);
  const visited = new Set<string>();
  const queue: VisitedFile[] = [];
  for (const entry of entries) {
    if (visited.has(canonical(entry.absPath))) continue;
    visited.add(canonical(entry.absPath));
    const facts = readFacts(entry.absPath, kindOf(entry.absPath), context);
    if (facts !== null) queue.push({ absPath: entry.absPath, dirAbs: entry.dirAbs, facts });
  }
  while (queue.length > 0) {
    const current = queue.shift();
    if (current === undefined) break;
    found.push(current);
    for (const specifier of current.facts.imports) {
      const target = resolveImport(specifier, current.dirAbs, scope.aliases);
      if (target === null || visited.has(canonical(target)) || !inScope(target)) continue;
      visited.add(canonical(target));
      const facts = readFacts(target, kindOf(target), context);
      if (facts !== null) queue.push({ absPath: target, dirAbs: parentDir(target), facts });
    }
  }
  return found;
}

// Every operation call reached from the layout entries, in file and line
// order. A call names its operation through the generated client map and is
// marked as reached through a layout.
export function collectLayoutCalls(entries: RouteEntry[], scope: LayoutScope, context: ReaderContext): CallFacts[] {
  const calls: CallFacts[] = [];
  for (const current of walkLayoutFiles(entries, scope, context)) {
    const rel = relativeTo(context.root, current.absPath) ?? toPosix(current.absPath);
    for (const site of current.facts.calls) {
      const operation = scope.operations.get(operationKey(site.name));
      if (operation !== undefined && operation !== "") {
        calls.push({ operation, file: rel, line: site.line, fromLayout: true });
      }
    }
  }
  calls.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return calls;
}

// Every full-page navigation reached from the layout entries, in file and
// line order. The CLI matches each path against the discovered endpoint
// routes and counts a match as a call of that endpoint's operation. Each
// navigation is marked as reached through a layout.
export function collectLayoutNavigations(entries: RouteEntry[], scope: LayoutScope, context: ReaderContext): NavigationFacts[] {
  const navigations: NavigationFacts[] = [];
  for (const current of walkLayoutFiles(entries, scope, context)) {
    const rel = relativeTo(context.root, current.absPath) ?? toPosix(current.absPath);
    for (const site of current.facts.navigations) {
      navigations.push({ path: site.path, file: rel, line: site.line, fromLayout: true });
    }
  }
  navigations.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return navigations;
}

// Every operation call reached from the route entries, in file and line
// order. A call names its operation through the generated client map and is
// marked as reached through the route's own files.
export function collectRouteCalls(entries: RouteEntry[], scope: CallScope, context: ReaderContext): CallFacts[] {
  const calls: CallFacts[] = [];
  for (const current of walkRouteFiles(entries, scope, context)) {
    const rel = relativeTo(context.root, current.absPath) ?? toPosix(current.absPath);
    for (const site of current.facts.calls) {
      const operation = scope.operations.get(operationKey(site.name));
      if (operation !== undefined && operation !== "") {
        calls.push({ operation, file: rel, line: site.line, fromLayout: false });
      }
    }
  }
  calls.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return calls;
}

// Every full-page navigation reached from the route entries, in file and
// line order. The CLI matches each path against the discovered endpoint
// routes and counts a match as a call of that endpoint's operation. Each
// navigation is marked as reached through the route's own files.
export function collectRouteNavigations(entries: RouteEntry[], scope: CallScope, context: ReaderContext): NavigationFacts[] {
  const navigations: NavigationFacts[] = [];
  for (const current of walkRouteFiles(entries, scope, context)) {
    const rel = relativeTo(context.root, current.absPath) ?? toPosix(current.absPath);
    for (const site of current.facts.navigations) {
      navigations.push({ path: site.path, file: rel, line: site.line, fromLayout: false });
    }
  }
  navigations.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return navigations;
}
