// The outline's rows: layer headers with their counts, records grouped by
// record field, and the security groups. Rows carry everything a row renders,
// so the components never walk the store twice. One build precomputes each
// record's group, completion, title and proof rank once and reuses them in
// grouping, sorting and counts; the last build is memoized per book while its
// inputs are unchanged.

import { MODULE_MEMBER_TYPES, UNFULFILLED_AREA, areaGroupKey, areaKeysOf, capitalize, formatLabel, proofRank, stripStep } from "$lib/frame";
import type { Book, RecordFields, SecurityItem } from "$lib/frame";

/** One virtual row in the outline. */
export type OutlineRow =
  | { kind: "layer"; key: string; layer: string; total: number; shown: number; done: number }
  | { kind: "group"; key: string; gk: string; label: string; count: number; completion: string; open: boolean; depth: number }
  | { kind: "record"; key: string; id: string; depth: number; level: number; kids: number; open: boolean; dim: boolean }
  | { kind: "note"; key: string; text: string }
  | { kind: "switch"; key: string };

/** The pixel height per row kind for the virtualizer: headers 36, rows 32, the grouping switch 40. */
export const ROW_HEIGHT: Record<OutlineRow["kind"], number> = {
  layer: 36, note: 32, group: 32, record: 32, switch: 40,
};

const INDENTS = [
  "pl-0", "pl-[22px]", "pl-[44px]", "pl-[66px]", "pl-[88px]", "pl-[110px]",
  "pl-[132px]", "pl-[154px]", "pl-[176px]", "pl-[198px]", "pl-[220px]", "pl-[242px]", "pl-[264px]",
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

function homeOf(id: string): string {
  return (stripStep(id).split("/")[1] ?? "").split(".")[0] ?? "";
}

function parentTitleOf(book: Book, id: string): string {
  const p = book.parent(id);
  return p ? book.title(p) : "";
}

function goalTitleForFeatureOf(book: Book, id: string): string {
  const titles = book.outs(stripStep(id), "advances").map((x) => book.title(x)).filter(Boolean);
  return titles[0] ?? "";
}

function goalTitleForProblemOf(book: Book, id: string): string {
  for (const f of book.kids(stripStep(id)).filter((child) => book.typeOf(child) === "feature")) {
    const g = goalTitleForFeatureOf(book, f);
    if (g) return g;
  }
  return "";
}

/** The record field children group by, per record type: a feature, an aggregate, an actor. */
function groupFieldOf(book: Book, id: string): [string, string] | null {
  const fields = book.fields(id);
  const t = book.typeOf(id);
  if (t === "requirement") {
    const feature = parentTitleOf(book, id);
    return feature ? [feature, ""] : null;
  }
  if (MODULE_MEMBER_TYPES.includes(t)) {
    const agg = parentTitleOf(book, id);
    const mod = homeOf(id);
    return [agg || mod, agg && mod ? mod : ""];
  }
  if (t === "screen") {
    return [parentTitleOf(book, id) || "No entry point", ""];
  }
  if (t === "flow") {
    const actor = fields.actor;
    const serves = book.outs(stripStep(id), "serves").map((x) => book.title(x));
    return [actor ? book.title(stripStep(actor)) : "No actor", serves[0] ?? ""];
  }
  if (t === "problem") {
    const affects = fields.affects;
    const list = Array.isArray(affects) ? affects : [];
    return [list.length ? book.title(stripStep(list[0])) : "No affected actor", goalTitleForProblemOf(book, id)];
  }
  if (t === "feature") return [parentTitleOf(book, id) || "No module", goalTitleForFeatureOf(book, id)];
  if (t === "actor") {
    const kind = fields.kind;
    return [kind ? capitalize(kind) : "No kind", ""];
  }
  if (t === "decision") {
    const kind = fields.kind;
    const concerns = decisionConcerns(book, id)
      .map((concern) => capitalize(formatLabel(concern)))
      .join(", ");
    return [kind ? capitalize(kind) : "No kind", concerns];
  }
  return null;
}

/** The decision concerns in display order. */
const DECISION_CONCERN_ORDER: readonly string[] = [
  "domain", "application", "infrastructure", "web-api",
  "frontend-ui", "frontend-data", "tests", "workspace", "operations",
];

/** The concerns of a decision's followers in display order, as the book command computed them. */
function decisionConcerns(book: Book, id: string): string[] {
  const found = new Set<string>();
  const concerns = book.get(id)?.concerns;
  if (Array.isArray(concerns)) {
    for (const concern of concerns) found.add(concern);
  }
  return DECISION_CONCERN_ORDER.filter((concern) => found.has(concern));
}

/** The group a record sits in under its parent: by record field, else by type. */
function groupOf(book: Book, id: string): { key: string; label: string } {
  const g = groupFieldOf(book, id);
  if (g && g[0]) return { key: "field:" + g.join("/"), label: g.filter(Boolean).join(": ") };
  const t = book.typeOf(id);
  const q = book.lex.types?.[t];
  return { key: "type:" + t, label: q ? q.plural || q.label || t : capitalize(t) + "s" };
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
        found = groupOf(book, id);
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

/** One record row and, when open, its children grouped by record field. The path is positional, so every row key is unique and a record shown under two parents keeps two keys. */
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

/** Children grouped by record field, else by type, each with its completion. */
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
  for (const [gi, g] of groups.entries()) {
    const list = sortKids(book, pre, g.list);
    const visible = list.filter((id) => book.isVisible(id));
    if (!visible.length) continue;
    const gk = parent + "|" + g.key;
    const gpath = path + "/g" + gi;
    const closed = !filtering && !!book.closedGroups[gk];
    rows.push({
      kind: "group", key: "group/" + gpath, gk, label: g.label,
      count: visible.length, completion: completeCounts(pre, visible), open: !closed, depth,
    });
    if (!closed) {
      for (const [i, id] of list.entries()) walkRecord(book, pre, id, depth, rows, gpath + "/r" + i);
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

/** The outline rows: the product root, then layer headers with their counts, records and field groups. */
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
    if (holder) delete book.closedGroups[holder + "|" + groupOf(book, current).key];
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
