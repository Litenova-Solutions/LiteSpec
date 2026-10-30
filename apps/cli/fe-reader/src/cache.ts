// Per-file cache under the request's cache folder. Each entry is keyed by the
// file content hash, the reader version, and a namespace naming the fact kind,
// so identical content reuses one entry whatever file it came from. Payloads
// hold lines only, never file paths. Cache failures are silent: the run
// recomputes and continues without the cache.

import { createHash } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { ensureDir } from "./files.js";
import { READER_VERSION } from "./protocol.js";

interface CacheEntry {
  reader: string;
  data: unknown;
}

function entryPath(cacheDir: string, namespace: string, content: string): string {
  const hash = createHash("sha256").update(`${READER_VERSION}\n${namespace}\n${content}`).digest("hex");
  return join(cacheDir, `${namespace}-${hash}.json`);
}

export function withCache<T>(cacheDir: string | null, namespace: string, content: string, compute: () => T): T {
  if (cacheDir === null || cacheDir === "") return compute();
  const path = entryPath(cacheDir, namespace, content);
  try {
    const stored: unknown = JSON.parse(readFileSync(path, "utf8"));
    if (typeof stored === "object" && stored !== null && (stored as CacheEntry).reader === READER_VERSION) {
      return (stored as CacheEntry).data as T;
    }
  } catch {
    // A missing or broken entry recomputes below.
  }
  const data = compute();
  try {
    ensureDir(cacheDir);
    const entry: CacheEntry = { reader: READER_VERSION, data };
    writeFileSync(path, JSON.stringify(entry), "utf8");
  } catch {
    // The run keeps its computed facts when the cache cannot be written.
  }
  return data;
}
