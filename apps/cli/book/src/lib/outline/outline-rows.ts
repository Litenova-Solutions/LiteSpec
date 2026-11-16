// The outline's rows: layer headers, records grouped by record type, and
// the security groups. Rows carry everything a row renders, so the components
// never walk the store twice. One build precomputes each record's group,
// completion, title and proof rank once and reuses them in grouping, sorting
// and counts; the last build is memoized per book while its inputs are
// unchanged. Counts and completion sit in tooltips and accessible names; the
// rows show none.

import { UNFULFILLED_AREA, areaGroupKey, areaKeysOf, capitalize, formatCount, formatLabel, proofRank, stripStep } from "$lib/frame";
import type { Book, RecordFields, SecurityItem } from "$lib/frame";

/** One virtual row in the outline. */
export type OutlineRow =
  | { kind: "layer"; key: string; layer: string; total: number; shown: number; done: number }
  | { kind: "group"; key: string; gk: string; label: string; count: number; completion: string; open: boolean; depth: number }
  | { kind: "record"; key: string; id: string; depth: number; level: number; kids: number; open: boolean; dim: boolean }
  | { kind: "note"; key: string; text: string }
  | { kind: "switch"; key: string };

/** The pixel height per row kind for the virtualizer: layer headers 32, every other row 28. */
export const ROW_HEIGHT: Record<OutlineRow["kind"], number> = {
  layer: 32, note: 28, group: 28, record: 28, switch: 28,
};

const INDENTS = [
  "pl-0", "pl-[14px]", "pl-[28px]", "pl-[42px]", "pl-[56px]", "pl-[70px]",
  "pl-[84px]", "pl-[98px]", "pl-[112px]", "pl-[126px]", "pl-[140px]", "pl-[154px]", "pl-[168px]",
];

/** The indentation class for an outline depth, clamped to the deepest step. */
export function indentClass(depth: number): string {
  return INDENTS[Math.min(Math.max(depth, 0), INDENTS.length - 1)] ?? "pl-0";
}

/** The navigation icon per record layer; an unknown layer reads as the core stack. */
const LAYER_ICONS: Record<string, string> = {
  intent: "core", behavior: "folder", implementation: "code", proof: "flask", governance: "waiver", glossary: "term",
};

/** The navigation icon name for a record layer. */
export function layerIconOf(layer: string): string {
  return LAYER_ICONS[layer] ?? "core";
}

/** Whether a field value reads by field name. */
function isFieldMap(value: unknown): value is RecordFields {
  return typeof value === "object" && value !== null;
}

/** Values computed once per row build and reused by grouping, sorting and counts. */
interface RowPrecompute {
  group(id: string): { key: string; label: string };
  complete(id: string): boolean;
  title(id: string): string;
  rank(id: string): number;
}

/** The per-build caches: each record's group, completion, title and proof rank. */
function precompute(book: Book): RowPrecompute {
  const groups = new Map<string, { key: string; label: string }>();
  const completes = new Map<string, boolean>();
  const titles = new Map<string, string>();
  const ranks = new Map<string, number>();
  return {
    group(id: string): { key: string; label: string } {
      let found = groups.get(id);
      if (!found) {
        found = book.groupOf(id);
        groups.set(id, found);
      }
      return found;
    },
    complete(id: string): boolean {
      let found = completes.get(id);
      if (found === undefined) {
        found = book.completeOf(id);
        completes.set(id, found);
      }
      return found;
    },
    title(id: string): string {
      let found = titles.get(id);
      if (found === undefined) {
        found = book.title(id);
        titles.set(id, found);
      }
      return found;
    },
    rank(id: string): number {
      let found = ranks.get(id);
      if (found === undefined) {
        found = proofRank(book.proofState(id));
        ranks.set(id, found);
      }
      return found;
    },
  };
}

/** The group's completion words from the precomputed completion of each record. */
function completeCounts(pre: RowPrecompute, ids: string[]): string {
  if (!ids.length) return "";
  const done = ids.filter((id) => pre.complete(id)).length;
  return done + " of " + ids.length + " complete";
}

/** A record count in words: 1 reads "1 record", 1595 reads "1,595 records". */
export function recordCount(n: number): string {
  return formatCount(n) + " " + (n === 1 ? "record" : "records");
}

/** Every real record below a record, each once. Code, tests and findings below it are leaves, never records. */
export function recordsBelow(book: Book, id: string): string[] {
  const out: string[] = [];
  const seen = new Set<string>([id]);
  const stack = [...book.kids(id)];
  while (stack.length > 0) {
    const current = stack.pop();
    if (current === undefined || seen.has(current)) continue;
    seen.add(current);
    if (book.isReal(current)) out.push(current);
    stack.push(...book.kids(current));
  }
  return out;
}

/** The records-below summary for a record: how many records sit below it and their completion. */
export function belowSummary(book: Book, id: string): string {
  const below = recordsBelow(book, id);
  if (!below.length) return "no records below";
  return recordCount(below.length) + " below, " + book.completeCounts(below);
}

/** The navigation tooltip for a record: its type with the records-below summary. */
export function recordTip(book: Book, id: string): string {
  return book.typeName(book.typeOf(id)) + ", " + belowSummary(book, id);
}

/** The navigation tooltip for a type group: its records and their completion. */
export function groupTip(label: string, count: number, completion: string): string {
  return completion
    ? label + ", " + recordCount(count) + ", " + completion
    : label + ", " + recordCount(count);
}

/** The navigation tooltip for a layer header: its records and their completion, with the shown records while filtering. */
export function layerTip(label: string, total: number, shown: number, done: number, filtering: boolean): string {
  if (filtering) return label + ", " + formatCount(shown) + " of " + formatCount(total) + " records shown";
  return label + ", " + recordCount(total) + ", " + formatCount(done) + " of " + formatCount(total) + " complete";
}

/** The group's records in the active sort, with each title and rank read once. */
function sortKids(book: Book, pre: RowPrecompute, list: string[]): string[] {
  if (book.sortKey === "outline") return list;
  const order = book.orderIdx;
  if (book.sortKey === "title") {
    return list
      .map((id) => ({ id, title: pre.title(id), at: order[id] ?? 0 }))
      .sort((a, b) => a.title.localeCompare(b.title) || a.at - b.at)
      .map((entry) => entry.id);
  }
  return list
    .map((id) => ({ id, rank: pre.rank(id), at: order[id] ?? 0 }))
    .sort((a, b) => a.rank - b.rank || a.at - b.at)
    .map((entry) => entry.id);
}

/** One layer's counts: its records, its direct matches and its complete records. */
export interface LayerCounts {
  total: number;
  shown: number;
  done: number;
}

/** Every layer's counts from one pass over the record order. */
function layerCounts(book: Book, pre: RowPrecompute, filtering: boolean): Map<string, LayerCounts> {
  const counts = new Map<string, LayerCounts>();
  for (const id of book.order) {
    if (!book.isReal(id)) continue;
    const layer = book.layerOf[id];
    if (!layer) continue;
    let entry = counts.get(layer);
    if (!entry) {
      entry = { total: 0, shown: 0, done: 0 };
      counts.set(layer, entry);
    }
    entry.total += 1;
    if (filtering && book.isDirectMatch(id)) entry.shown += 1;
    if (pre.complete(id)) entry.done += 1;
  }
  return counts;
}

/** One record row and, when open, its children grouped by record type. The path is positional, so every row key is unique and a record shown under two parents keeps two keys. */
function walkRecord(book: Book, pre: RowPrecompute, id: string, depth: number, rows: OutlineRow[], path: string): void {
  if (!book.isVisible(id)) return;
  const kids = book.kids(id).filter((k) => book.isVisible(k));
  const open = kids.length > 0 && (book.anyFilter() ? true : !!book.expanded[id]);
  rows.push({
    kind: "record", key: "row/" + path, id, depth,
    level: book.depthOf[id] ?? depth, kids: kids.length, open, dim: book.anyFilter() && !book.isDirectMatch(id),
  });
  if (open) walkGroups(book, pre, kids, id, depth + 1, rows, path);
}

/** Children grouped by record type, each with its completion for its tooltip. Children of one record type list directly, without a group header. */
function walkGroups(book: Book, pre: RowPrecompute, kids: string[], parent: string, depth: number, rows: OutlineRow[], path: string): void {
  const groups: { key: string; label: string; list: string[] }[] = [];
  const byKey: Record<string, { key: string; label: string; list: string[] }> = {};
  for (const id of kids) {
    const g = pre.group(id);
    let entry = byKey[g.key];
    if (!entry) {
      entry = { key: g.key, label: g.label, list: [] };
      byKey[g.key] = entry;
      groups.push(entry);
    }
    entry.list.push(id);
  }
  const filtering = book.anyFilter();
  const shaped = groups
    .map((g, gi) => {
      const list = sortKids(book, pre, g.list);
      return { g, gi, list, visible: list.filter((id) => book.isVisible(id)) };
    })
    .filter((s) => s.visible.length);
  const only = shaped.length === 1 ? shaped[0] : undefined;
  if (only) {
    const gpath = path + "/g" + only.gi;
    for (const [i, id] of only.list.entries()) walkRecord(book, pre, id, depth, rows, gpath + "/r" + i);
    return;
  }
  for (const s of shaped) {
    const gk = parent + "|" + s.g.key;
    const gpath = path + "/g" + s.gi;
    const closed = !filtering && !!book.closedGroups[gk];
    rows.push({
      kind: "group", key: "group/" + gpath, gk, label: s.g.label,
      count: s.visible.length, completion: completeCounts(pre, s.visible), open: !closed, depth,
    });
    if (!closed) {
      for (const [i, id] of s.list.entries()) walkRecord(book, pre, id, depth, rows, gpath + "/r" + i);
    }
  }
}

/** The Intent layer grouped by product area: one group per Behavior module with its features and flows, then the features and flows no module reaches. */
function walkAreas(book: Book, pre: RowPrecompute, rows: OutlineRow[]): void {
  const areas = book.productAreas;
  if (!areas) {
    rows.push({ kind: "note", key: "layer/intent/areas-loading", text: "Loading the product areas." });
    return;
  }
  const filtering = book.anyFilter();
  let gi = 0;
  const walkArea = (id: string, label: string, list: string[]): void => {
    const sorted = sortKids(book, pre, list);
    const visible = sorted.filter((entry) => book.isVisible(entry));
    if (filtering && !visible.length) return;
    if (id === UNFULFILLED_AREA && !visible.length) return;
    const gk = areaGroupKey(id);
    const gpath = "layer/intent-area/g" + gi++;
    const closed = !filtering && !!book.closedGroups[gk];
    rows.push({
      kind: "group", key: "group/" + gpath, gk, label,
      count: visible.length, completion: completeCounts(pre, visible), open: !closed, depth: 1,
    });
    if (!closed) {
      for (const [i, entry] of sorted.entries()) walkRecord(book, pre, entry, 1, rows, gpath + "/r" + i);
    }
  };
  for (const mod of areas.modules) walkArea(mod.id, mod.title, [...mod.features, ...mod.flows]);
  walkArea(UNFULFILLED_AREA, "Not yet fulfilled", [...areas.unfulfilled.features, ...areas.unfulfilled.flows]);
}

/** The outline rows: the product root, then layer headers, records and field groups. */
function buildRows(book: Book): OutlineRow[] {
  const rows: OutlineRow[] = [];
  const filtering = book.anyFilter();
  const pre = precompute(book);
  const counts = layerCounts(book, pre, filtering);
  // The product root above the first layer group: one record row, so
  // selection, reveal and keyboard moves treat it like any record.
  const pid = book.productId();
  if (pid && book.has(pid) && book.isVisible(pid)) {
    rows.push({
      kind: "record", key: "root", id: pid, depth: 0,
      level: 1, kids: 0, open: false, dim: filtering && !book.isDirectMatch(pid),
    });
  }
  for (const layer of book.man?.layers ?? []) {
    if (!book.isLayerLoaded(layer.key)) {
      rows.push({ kind: "layer", key: "layer/" + layer.key, layer: layer.key, total: 0, shown: 0, done: 0 });
      if (book.openLayers[layer.key]) rows.push({ kind: "note", key: "layer/" + layer.key + "/loading", text: "Loading." });
      continue;
    }
    const entry = counts.get(layer.key) ?? { total: 0, shown: 0, done: 0 };
    if (filtering && !entry.shown) continue;
    rows.push({ kind: "layer", key: "layer/" + layer.key, layer: layer.key, total: entry.total, shown: entry.shown, done: entry.done });
    if (!(filtering || book.openLayers[layer.key])) continue;
    if (layer.key === "intent") {
      rows.push({ kind: "switch", key: "layer/intent/switch" });
      if (book.intentGrouping === "product-area") {
        walkAreas(book, pre, rows);
        continue;
      }
    }
    const roots = book.rootsOf[layer.key] ?? [];
    const base = "layer/" + layer.key;
    if (roots.length > 1) walkGroups(book, pre, roots, layer.key, 1, rows, base);
    else for (const [i, id] of roots.entries()) walkRecord(book, pre, id, 1, rows, base + "/r" + i);
  }
  return rows;
}

/** The last row build per book, reused while its inputs are unchanged. */
const rowCache = new WeakMap<Book, { sig: string; rows: OutlineRow[] }>();

/** The inputs a row build reads, as one string: the outline, records, search and product-area revisions, the sort, the Intent grouping, the filters and the expansion. */
function rowSignature(book: Book): string {
  return JSON.stringify([
    book.outlineRev,
    book.recRev,
    book.searchRev,
    book.areasRev,
    book.sortKey,
    book.intentGrouping,
    book.query,
    book.active,
    book.expanded,
    book.closedGroups,
    book.openLayers,
    book.order.length,
  ]);
}

/** The outline rows, memoized per book while the revision, sort, filters and expansion are unchanged. */
export function buildOutlineRows(book: Book): OutlineRow[] {
  const sig = rowSignature(book);
  const cached = rowCache.get(book);
  if (cached && cached.sig === sig) return cached.rows;
  const rows = buildRows(book);
  rowCache.set(book, { sig, rows });
  return rows;
}

/** Opens the chain above a record without closing anything else: the record, its ancestors, the group each sits in and its layer. */
export function expandChain(book: Book, raw: string): void {
  const id = stripStep(raw);
  if (!book.has(id)) return;
  const chain: string[] = [id];
  const seen = new Set<string>([id]);
  let parent = book.parent(id);
  while (parent && !seen.has(parent)) {
    chain.push(parent);
    seen.add(parent);
    parent = book.parent(parent);
  }
  for (const current of chain) {
    if (book.kids(current).length) book.expanded[current] = true;
    const holder = book.parent(current) ?? book.layerOf[current] ?? "";
    if (holder) delete book.closedGroups[holder + "|" + book.groupOf(current).key];
  }
  if (book.intentGrouping === "product-area" && book.productAreas) {
    for (const key of areaKeysOf(book.productAreas, chain)) delete book.closedGroups[key];
  }
  const layer = book.layerOf[id];
  if (layer) book.openLayers[layer] = true;
}

/** The records in the book: the manifest totals, else the loaded real records. The tabs and the shown line read this one count. */
const totalsCache = new WeakMap<Book, { rev: number; total: number }>();
export function totalRecords(book: Book): number {
  const cached = totalsCache.get(book);
  if (cached && cached.rev === book.rev) return cached.total;
  let total = Object.values(book.man?.totals ?? {}).reduce((a, b) => a + b, 0);
  if (!total) total = book.orderWithProduct().filter((id) => book.isReal(id)).length;
  totalsCache.set(book, { rev: book.rev, total });
  return total;
}

/** The security items: the shard once loaded, else read from the loaded records. */
export function securityItems(book: Book): SecurityItem[] {
  if (book.security !== null) return book.securityItems();
  const items: SecurityItem[] = [];
  for (const id of book.order) {
    if (!book.isReal(id)) continue;
    const f = book.fields(id);
    const access = f.access;
    if (isFieldMap(access) && access.tenant === "cross") {
      const grant = access.grant;
      items.push({ kind: "cross", id, text: "Acts across tenants" + (typeof grant === "string" && grant ? ", through " + book.title(grant) : "") });
    }
    const effects = f.effects;
    if (Array.isArray(effects)) {
      for (const effect of effects) {
        if (!isFieldMap(effect)) continue;
        const calls = effect.calls;
        if (typeof calls !== "string" || !calls) continue;
        items.push({
          kind: "calls", id,
          text: "Calls " + book.title(stripStep(calls))
            + (calls.indexOf("#") > 0 ? ", " + formatLabel(calls.split("#")[1]) : ""),
        });
      }
    }
    const dataClasses = f.dataClasses;
    if (Array.isArray(dataClasses)) {
      for (const entry of dataClasses) {
        if (!isFieldMap(entry)) continue;
        const classification = entry.classification;
        if (classification !== "personal" && classification !== "sensitive") continue;
        const erasure = entry.onErasure;
        items.push({
          kind: "data", id,
          text: capitalize(classification) + " data: " + formatLabel(entry.name)
            + (typeof erasure === "string" && erasure ? ", on erasure " + erasure : ""),
        });
      }
    }
  }
  return items;
}

/** The security items memoized per book revision, so the outline and the shown line share one computation. */
const securityCache = new WeakMap<Book, { rev: number; items: SecurityItem[] }>();
export function cachedSecurityItems(book: Book): SecurityItem[] {
  const cached = securityCache.get(book);
  if (cached && cached.rev === book.rev) return cached.items;
  const items = securityItems(book);
  securityCache.set(book, { rev: book.rev, items });
  return items;
}

/** One group of security items by kind, with its heading words. */
export interface SecurityGroup {
  kind: "cross" | "calls" | "data";
  label: string;
  icon: string;
  items: SecurityItem[];
}

/** The security items passing the filters, grouped by kind. */
export function securityGroups(book: Book): SecurityGroup[] {
  const items = cachedSecurityItems(book).filter((x) => book.passesFilters(x.id));
  const groups: SecurityGroup[] = [
    { kind: "cross", label: "Access", icon: "shield", items: items.filter((x) => x.kind === "cross") },
    { kind: "calls", label: "Calls", icon: "ext", items: items.filter((x) => x.kind === "calls") },
    { kind: "data", label: "Data classes", icon: "lock", items: items.filter((x) => x.kind === "data") },
  ];
  return groups.filter((g) => g.items.length);
}
