// Generated-client operation discovery and per-route call collection. An
// operation is a named function export of the generated client folder,
// reported as the client exports it. Calls are collected from the route entry files and
// from every file reached through relative and alias imports within the
// route's own folder and the use-case module folders the entry imports
// directly. The frame and shared packages are never followed.

import { absolute, baseName, canonical, isFile, isWithin, joinPath, listFilesRecursive, parentDir, relativeTo, toPosix } from "./files.js";
import { FileFacts, FileKind, readFacts } from "./parse.js";
import { CallFacts, ReaderContext } from "./protocol.js";

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

// Every operation the generated client folder exports, keyed by its
// PascalCase form and reported as the client exports it. A client
// file that fails to parse is diagnosed and contributes no operations.
export function collectOperations(apiClientAbs: string, context: ReaderContext): Map<string, string> {
  const operations = new Map<string, string>();
  for (const file of listFilesRecursive(apiClientAbs, [".ts", ".tsx"])) {
    const facts = readFacts(file, "ts", context);
    if (facts === null) continue;
    for (const name of facts.functions) {
      const key = operationKey(name);
      if (key !== "" && !operations.has(key)) operations.set(key, name);
    }
  }
  return operations;
}

function probeCandidates(resolved: string): string | null {
  const candidates = [
    resolved,
    `${resolved}.ts`,
    `${resolved}.tsx`,
    `${resolved}.svelte`,
    joinPath(resolved, "index.ts"),
    joinPath(resolved, "index.tsx"),
  ];
  if (resolved.endsWith(".js")) candidates.push(`${resolved.slice(0, -3)}.ts`);
  if (resolved.endsWith(".jsx")) candidates.push(`${resolved.slice(0, -4)}.tsx`);
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

// The use-case module folders the route entry files import directly: for each
// import resolving below the modules root, the nearest ancestor folder known
// to be a use-case folder.
export function directModuleFolders(
  entryFacts: { facts: FileFacts; dirAbs: string }[],
  modulesAbs: string,
  useCaseFolders: Set<string>,
  aliases: Record<string, string>,
): Set<string> {
  const folders = new Set<string>();
  for (const entry of entryFacts) {
    for (const specifier of entry.facts.imports) {
      const target = resolveImport(specifier, entry.dirAbs, aliases);
      if (target === null || !isWithin(modulesAbs, target)) continue;
      let dir = parentDir(target);
      while (true) {
        if (useCaseFolders.has(canonical(dir))) {
          folders.add(canonical(dir));
          break;
        }
        if (dir === modulesAbs) break;
        const parent = parentDir(dir);
        if (parent === dir || !isWithin(modulesAbs, dir)) break;
        dir = parent;
      }
    }
  }
  return folders;
}

export interface RouteEntry {
  absPath: string;
  dirAbs: string;
}

export interface CallScope {
  routeDirAbs: string;
  modulesAbs: string;
  useCaseFolders: Set<string>;
  operations: Map<string, string>;
  aliases: Record<string, string>;
}

// Every operation call reached from the route entries, following imports only
// within the route folder and the directly imported module folders. Files are
// visited once; entries come out sorted by file and line.
export function collectRouteCalls(entries: RouteEntry[], scope: CallScope, context: ReaderContext): CallFacts[] {
  const entryFacts: { absPath: string; facts: FileFacts; dirAbs: string }[] = [];
  for (const entry of entries) {
    const facts = readFacts(entry.absPath, kindOf(entry.absPath), context);
    if (facts !== null) entryFacts.push({ absPath: entry.absPath, facts, dirAbs: entry.dirAbs });
  }
  const moduleFolders = directModuleFolders(entryFacts, scope.modulesAbs, scope.useCaseFolders, scope.aliases);

  const inScope = (absPath: string): boolean => {
    if (isWithin(scope.routeDirAbs, absPath)) return true;
    for (const folder of moduleFolders) {
      if (isWithin(folder, absPath)) return true;
    }
    return false;
  };

  const calls: CallFacts[] = [];
  const visited = new Set<string>();
  const queue: { absPath: string; dirAbs: string; facts: FileFacts }[] = [];
  for (const entry of entryFacts) {
    if (visited.has(canonical(entry.absPath))) continue;
    visited.add(canonical(entry.absPath));
    queue.push({ absPath: entry.absPath, dirAbs: entry.dirAbs, facts: entry.facts });
  }
  for (const entry of entries) visited.add(canonical(entry.absPath));
  while (queue.length > 0) {
    const current = queue.shift();
    if (current === undefined) break;
    const rel = relativeTo(context.root, current.absPath) ?? toPosix(current.absPath);
    for (const site of current.facts.calls) {
      const operation = scope.operations.get(operationKey(site.name));
      if (operation !== undefined && operation !== "") {
        calls.push({ operation, file: rel, line: site.line });
      }
    }
    for (const specifier of current.facts.imports) {
      const target = resolveImport(specifier, current.dirAbs, scope.aliases);
      if (target === null || visited.has(canonical(target)) || !inScope(target)) continue;
      visited.add(canonical(target));
      const facts = readFacts(target, kindOf(target), context);
      if (facts !== null) queue.push({ absPath: target, dirAbs: parentDir(target), facts });
    }
  }
  calls.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return calls;
}
