import MiniSearch from "minisearch";
import type { SearchRow } from "./shard-types";

// Full-text search over the search shards: every record's title, type and excerpt.
// Words match by prefix and tolerate a small typo; a title hit outranks a text hit,
// and every word must match, so a longer query narrows the result.

export interface BookSearch {
  /** The matching record ids with their scores, best first. */
  query(text: string): Map<string, number>;
  size: number;
}

interface Doc { id: string; title: string; type: string; text: string }

export async function createSearch(rows: SearchRow[]): Promise<BookSearch> {
  const index = new MiniSearch<Doc>({
    fields: ["title", "type", "text"],
    storeFields: [],
    searchOptions: { boost: { title: 3, type: 1.5 }, prefix: true, fuzzy: 0.2, combineWith: "AND" },
  });
  const docs: Doc[] = [];
  const seen = new Set<string>();
  for (const r of rows) {
    if (!r?.[0] || seen.has(r[0])) continue;
    seen.add(r[0]);
    docs.push({ id: r[0], title: r[1] ?? "", type: r[2] ?? "", text: r[3] ?? "" });
  }
  // Index in chunks so a large book stays responsive while the index builds.
  await index.addAllAsync(docs, { chunkSize: 1000 });
  return {
    size: docs.length,
    query(text) {
      const out = new Map<string, number>();
      const q = text.trim();
      if (!q) return out;
      for (const hit of index.search(q)) out.set(String(hit.id), hit.score);
      return out;
    },
  };
}
