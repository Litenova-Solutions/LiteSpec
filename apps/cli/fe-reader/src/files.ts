// File system helpers: absolute path handling, directory walks, and text reads.
// All comparisons go through resolve so request paths and walked paths share
// one spelling. Reads never throw; an unreadable file or folder reads as absent.

import { existsSync, mkdirSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";

export function toPosix(path: string): string {
  return path.replace(/\\/g, "/");
}

// The comparison spelling of an absolute path: Windows compares paths without
// regard to case, so identity sets hold this form instead of the raw path.
export function canonical(absPath: string): string {
  return sep === "\\" ? absPath.toLowerCase() : absPath;
}

export function absolute(path: string): string {
  return resolve(path);
}

export function parentDir(path: string): string {
  return dirname(path);
}

export function joinPath(...parts: string[]): string {
  return join(...parts);
}

export function baseName(path: string): string {
  const posix = toPosix(path);
  const slash = posix.lastIndexOf("/");
  return slash < 0 ? posix : posix.slice(slash + 1);
}

export function isFile(path: string): boolean {
  try {
    return existsSync(path) && statSync(path).isFile();
  } catch {
    return false;
  }
}

export function isDirectory(path: string): boolean {
  try {
    return existsSync(path) && statSync(path).isDirectory();
  } catch {
    return false;
  }
}

// True when fileAbs is dirAbs itself or sits below it.
export function isWithin(dirAbs: string, fileAbs: string): boolean {
  if (sep === "\\") {
    const dir = dirAbs.toLowerCase();
    const file = fileAbs.toLowerCase();
    return file === dir || file.startsWith(dir + "\\");
  }
  return fileAbs === dirAbs || fileAbs.startsWith(dirAbs + "/");
}

// The path of abs below root with forward slashes, or null when abs sits
// outside root.
export function relativeTo(rootAbs: string, abs: string): string | null {
  const rel = relative(rootAbs, abs);
  if (rel === "" || rel === "." || rel === ".." || rel.startsWith(".." + sep)) return null;
  return toPosix(rel);
}

export function ensureDir(dirAbs: string): void {
  try {
    mkdirSync(dirAbs, { recursive: true });
  } catch {
    // The run continues without a cache when the folder cannot be created.
  }
}

export function readText(absPath: string): string | null {
  try {
    return readFileSync(absPath, "utf8");
  } catch {
    return null;
  }
}

const SKIPPED_DIRS = new Set(["node_modules", ".git", "dist", "bin", "obj", ".cache"]);

function isSkippedDir(name: string): boolean {
  return name.startsWith(".") || SKIPPED_DIRS.has(name);
}

// Every file below dirAbs whose name ends with one of extensions, as absolute
// paths in sorted order. A missing or unreadable folder yields nothing.
export function listFilesRecursive(dirAbs: string, extensions: string[]): string[] {
  const found: string[] = [];
  const visit = (dir: string): void => {
    let entries: string[];
    try {
      entries = readdirSync(dir).sort();
    } catch {
      return;
    }
    for (const entry of entries) {
      const full = join(dir, entry);
      let stats: { isDirectory(): boolean; isFile(): boolean };
      try {
        stats = statSync(full);
      } catch {
        continue;
      }
      if (stats.isDirectory()) {
        if (!isSkippedDir(entry)) visit(full);
      } else if (stats.isFile() && extensions.some(ext => entry.endsWith(ext))) {
        found.push(full);
      }
    }
  };
  if (isDirectory(dirAbs)) visit(dirAbs);
  return found.sort();
}

// Every directory below dirAbs including itself, as absolute paths. The pair
// reports for each directory whether it holds a direct child directory.
export function listDirsRecursive(dirAbs: string): { dir: string; hasChildDir: boolean }[] {
  const result: { dir: string; hasChildDir: boolean }[] = [];
  const visit = (dir: string): void => {
    let entries: string[];
    try {
      entries = readdirSync(dir).sort();
    } catch {
      result.push({ dir, hasChildDir: false });
      return;
    }
    const childDirs: string[] = [];
    for (const entry of entries) {
      const full = join(dir, entry);
      try {
        if (statSync(full).isDirectory() && !isSkippedDir(entry)) childDirs.push(full);
      } catch {
        // Skip entries that cannot be statted.
      }
    }
    result.push({ dir, hasChildDir: childDirs.length > 0 });
    for (const child of childDirs) visit(child);
  };
  if (isDirectory(dirAbs)) visit(dirAbs);
  return result;
}
