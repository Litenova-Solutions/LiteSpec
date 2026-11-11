// The navigation's rows: the outline with its field groups, the record list
// with its groups, and the security groups. Rows carry everything a row
// renders, so the components never walk the store twice.

import { capitalize, formatLabel, stripStep, type Book } from "$lib/frame";
import type { SecurityItem } from "$lib/frame";

/** One virtual row in the outline or list view. */
export type OutlineRow =
  | { kind: "layer"; key: string; layer: string }
  | { kind: "desc"; key: string; layer: string }
  | { kind: "group"; key: string; gk: string; label: string; count: number; completion: string; open: boolean; depth: number }
  | { kind: "record"; key: string; id: string; depth: number; level: number; kids: number; open: boolean; dim: boolean }
  | { kind: "listgroup"; key: string; label: string; icon: string | null; count: number; completion: string }
  | { kind: "listrecord"; key: string; id: string }
  | { kind: "note"; key: string; text: string }
  | { kind: "showall"; key: string; total: number };

/** The estimated pixel height per row kind for the virtualizer. */
export const ROW_HEIGHT: Record<OutlineRow["kind"], number> = {
  layer: 34, desc: 56, note: 28, group: 26, record: 28, listgroup: 30, listrecord: 46, showall: 50,
};

const INDENTS = [
  "pl-0", "pl-[22px]", "pl-[44px]", "pl-[66px]", "pl-[88px]", "pl-[110px]",
  "pl-[132px]", "pl-[154px]", "pl-[176px]", "pl-[198px]", "pl-[220px]", "pl-[242px]", "pl-[264px]",
];

/** The indentation class for an outline depth, clamped to the deepest step. */
export function indentClass(depth: number) {
  return INDENTS[Math.min(Math.max(depth, 0), INDENTS.length - 1)];
}

/** One record row and, when open, its children grouped by record field. */
function walkRecord(book: Book, id: string, depth: number, rows: OutlineRow[]) {
  if (!book.isVisible(id)) return;
  const kids = book.kids(id).filter((k) => book.isVisible(k));
  const open = kids.length > 0 && (book.anyFilter() ? true : !!book.expanded[id]);
  rows.push({
    kind: "record", key: "row/" + id, id, depth,
    level: book.depthOf[id] ?? depth, kids: kids.length, open, dim: book.anyFilter() && !book.isDirectMatch(id),
  });
  if (open) walkGroups(book, kids, id, depth + 1, rows);
}

/** Children grouped by record field, else by type, each with its completion. */
function walkGroups(book: Book, kids: string[], parent: string, depth: number, rows: OutlineRow[]) {
  const groups: { key: string; label: string; list: string[] }[] = [];
  const byKey: Record<string, { key: string; label: string; list: string[] }> = {};
  for (const id of kids) {
    const g = book.fieldGroup(id);
    let key: string, label: string;
    if (g && g[0]) {
      key = "field:" + g.join("/");
      label = g.filter(Boolean).join(": ");
    } else {
      const t = book.typeOf(id);
      key = "type:" + t;
      const q = book.lex.types?.[t];
      label = q ? q.plural || q.label || t : capitalize(t) + "s";
    }
    byKey[key] ??= { key, label, list: [] };
    if (!groups.includes(byKey[key])) groups.push(byKey[key]);
    byKey[key].list.push(id);
  }
  const filtering = book.anyFilter();
  for (const g of groups) {
    const list = g.list.slice();
    if (book.sortKey !== "outline") list.sort(book.sorts[book.sortKey]?.cmp ?? book.sorts.outline.cmp);
    const visible = list.filter((id) => book.isVisible(id));
    if (!visible.length) continue;
    const gk = parent + "|" + g.key;
    const closed = !filtering && !!book.closedGroups[gk];
    rows.push({
      kind: "group", key: "group/" + gk, gk, label: g.label,
      count: visible.length, completion: book.completeCounts(visible), open: !closed, depth,
    });
    if (!closed) for (const id of list) walkRecord(book, id, depth, rows);
  }
}

/** The outline rows: layer headers with their descriptions, records and field groups. */
export function buildOutlineRows(book: Book): OutlineRow[] {
  const rows: OutlineRow[] = [];
  const filtering = book.anyFilter();
  for (const layer of book.man?.layers ?? []) {
    if (!book.isLayerLoaded(layer.key)) {
      rows.push({ kind: "layer", key: "layer/" + layer.key, layer: layer.key });
      if (book.openLayers[layer.key]) rows.push({ kind: "note", key: "layer/" + layer.key + "/loading", text: "Loading." });
      continue;
    }
    const inLayer = book.order.filter((id) => book.layerOf[id] === layer.key && book.isReal(id));
    const matched = filtering ? inLayer.filter((id) => book.isDirectMatch(id)).length : 0;
    if (filtering && !matched) continue;
    rows.push({ kind: "layer", key: "layer/" + layer.key, layer: layer.key });
    if (!(filtering || book.openLayers[layer.key])) continue;
    if (!filtering) rows.push({ kind: "desc", key: "layer/" + layer.key + "/desc", layer: layer.key });
    const roots = book.rootsOf[layer.key] ?? [];
    if (roots.length > 1) walkGroups(book, roots, layer.key, 1, rows);
    else for (const id of roots) walkRecord(book, id, 1, rows);
  }
  return rows;
}

/** The list rows: group headers with their completion, then records, capped at 400. */
export function buildListRows(book: Book): OutlineRow[] {
  const rows: OutlineRow[] = [];
  const cmp = book.sorts[book.sortKey]?.cmp ?? book.sorts.outline.cmp;
  const ids = book.order.filter((id) => book.passesFilters(id)).sort(cmp);
  const counts: Record<string, number> = {};
  const members: Record<string, string[]> = {};
  if (book.groupKey !== "none") {
    for (const id of ids) {
      const v = book.groupVal(id)[0];
      counts[v] = (counts[v] ?? 0) + 1;
      (members[v] ??= []).push(id);
    }
    ids.sort((a, b) => {
      const x = book.groupVal(a)[0], y = book.groupVal(b)[0];
      return x < y ? -1 : +(x > y);
    });
    if (book.groupKey === "type") {
      const order = Object.keys(book.lex.types ?? {});
      ids.sort((a, b) => order.indexOf(book.typeOf(a)) - order.indexOf(book.typeOf(b)));
    }
  }
  const shown = book.listShowAll ? ids.length : 400;
  const slice = ids.slice(0, shown);
  let last: string | null = null;
  for (const id of slice) {
    const g = book.groupVal(id);
    if (book.groupKey !== "none" && g[0] !== last) {
      rows.push({
        kind: "listgroup", key: "listgroup/" + g[0], label: g[1],
        icon: book.groupKey === "type" ? g[0] : null,
        count: counts[g[0]] ?? 0, completion: book.completeCounts(members[g[0]] ?? []),
      });
      last = g[0];
    }
    rows.push({ kind: "listrecord", key: "list/" + id, id });
  }
  if (ids.length > shown) rows.push({ kind: "showall", key: "list/showall", total: ids.length });
  else if (book.listShowAll && ids.length > 400) rows.push({ kind: "note", key: "list/shown", text: "Showing all " + ids.length + " records." });
  if (!ids.length) rows.push({ kind: "note", key: "list/empty", text: "No record matches these filters." });
  return rows;
}

/** The security items: the shard once loaded, else read from the loaded records. */
export function securityItems(book: Book): SecurityItem[] {
  if (book.security !== null) return book.securityItems();
  const items: SecurityItem[] = [];
  for (const id of book.order) {
    if (!book.isReal(id)) continue;
    const f = book.fields(id);
    if (f.access && f.access.tenant === "cross") {
      items.push({ kind: "cross", id, text: "Acts across tenants" + (f.access.grant ? ", through " + book.title(f.access.grant) : "") });
    }
    for (const e of f.effects ?? []) {
      if (e.calls) {
        items.push({
          kind: "calls", id,
          text: "Calls " + book.title(stripStep(String(e.calls)))
            + (String(e.calls).indexOf("#") > 0 ? ", " + formatLabel(String(e.calls).split("#")[1]) : ""),
        });
      }
    }
    for (const d of f.dataClasses ?? []) {
      if (d.classification === "personal" || d.classification === "sensitive") {
        items.push({
          kind: "data", id,
          text: capitalize(String(d.classification)) + " data: " + formatLabel(d.name)
            + (d.onErasure ? ", on erasure " + d.onErasure : ""),
        });
      }
    }
  }
  return items;
}

/** One group of security items by kind, with its heading words. */
export interface SecurityGroup {
  kind: "cross" | "calls" | "data";
  label: string;
  text: string;
  icon: string;
  items: SecurityItem[];
}

/** The security items passing the filters, grouped by kind. */
export function securityGroups(book: Book): SecurityGroup[] {
  const items = securityItems(book).filter((x) => book.passesFilters(x.id));
  const groups: SecurityGroup[] = [
    { kind: "cross", label: "Access", text: "Use cases that act outside the caller's own tenant.", icon: "shield", items: items.filter((x) => x.kind === "cross") },
    { kind: "calls", label: "Calls", text: "Every call that leaves the system for an external system.", icon: "ext", items: items.filter((x) => x.kind === "calls") },
    { kind: "data", label: "Data classes", text: "Data classes that hold personal or sensitive fields, with their erasure position.", icon: "lock", items: items.filter((x) => x.kind === "data") },
  ];
  return groups.filter((g) => g.items.length);
}
