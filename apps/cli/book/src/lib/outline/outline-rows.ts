// The outline's rows: layer headers with their descriptions, records grouped by
// record field, and the security groups. Rows carry everything a row renders,
// so the components never walk the store twice.

import { MODULE_MEMBER_TYPES, capitalize, formatLabel, stripStep } from "$lib/frame";
import type { Book, RecordFields, SecurityItem } from "$lib/frame";

/** One virtual row in the outline. */
export type OutlineRow =
  | { kind: "layer"; key: string; layer: string }
  | { kind: "desc"; key: string; layer: string }
  | { kind: "group"; key: string; gk: string; label: string; count: number; completion: string; open: boolean; depth: number }
  | { kind: "record"; key: string; id: string; depth: number; level: number; kids: number; open: boolean; dim: boolean }
  | { kind: "note"; key: string; text: string };

/** The estimated pixel height per row kind for the virtualizer. */
export const ROW_HEIGHT: Record<OutlineRow["kind"], number> = {
  layer: 34, desc: 56, note: 28, group: 26, record: 28,
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
  intent: "core", behavior: "folder", implementation: "code", proof: "flask", governance: "departure", glossary: "term",
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
  return ((stripStep(id) ?? "").split("/")[1] ?? "").split(".")[0] ?? "";
}

function parentTitleOf(book: Book, id: string): string {
  const p = book.parent(id);
  return p ? book.title(p) : "";
}

function goalTitleForFeatureOf(book: Book, id: string): string {
  const titles = book.outs(stripStep(id) ?? "", "advances").map((x) => book.title(x)).filter(Boolean);
  return titles[0] ?? "";
}

function goalTitleForProblemOf(book: Book, id: string): string {
  for (const f of book.ins(stripStep(id) ?? "", "solves")) {
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
    const floorplan = fields.floorplan;
    return [parentTitleOf(book, id) || "No entry point", floorplan ? capitalize(formatLabel(floorplan)) : ""];
  }
  if (t === "journey") {
    const actor = fields.actor;
    const serves = book.outs(stripStep(id) ?? "", "serves").map((x) => book.title(x));
    return [actor ? book.title(stripStep(actor) ?? "") : "No actor", serves[0] ?? ""];
  }
  if (t === "problem") {
    const affects = fields.affects;
    const list = Array.isArray(affects) ? affects : [];
    return [list.length ? book.title(stripStep(list[0]) ?? "") : "No affected actor", goalTitleForProblemOf(book, id)];
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

/** The concerns in display order, as the computed decision concern lists them. */
const DECISION_CONCERN_ORDER: readonly string[] = [
  "domain", "application", "infrastructure", "web-api",
  "frontend-ui", "frontend-data", "tests", "workspace", "operations",
];

/** The concerns of a decision's followers in display order, from the classification rules applies. */
function decisionConcerns(book: Book, id: string): string[] {
  const found = new Set<string>();
  for (const target of book.ins(id, "follows")) {
    const path = target.startsWith("code/") ? target.slice("code/".length) : target;
    const concern = concernOfPath(path);
    if (concern) found.add(concern);
  }
  return DECISION_CONCERN_ORDER.filter((concern) => found.has(concern));
}

/** Whether a C# file name carries a WebApi role suffix. */
function isWebApiName(name: string): boolean {
  return (
    name.endsWith("Endpoint.cs") ||
    name.endsWith("RequestModel.cs") ||
    name.endsWith("ResponseModel.cs") ||
    name.endsWith("ResponseItemModel.cs") ||
    name.endsWith("PaginationModel.cs") ||
    (name.endsWith("Model.cs") && !name.endsWith("ViewModel.cs"))
  );
}

/** The concern one follower path belongs to, following the classification rules applies, or nothing when no rule names it. */
function concernOfPath(path: string): string {
  const segments = path.split("/");
  const name = segments[segments.length - 1] ?? "";
  const dot = name.lastIndexOf(".");
  const extension = dot >= 0 ? name.slice(dot).toLowerCase() : "";
  if (
    segments.includes("tests") ||
    segments.includes("test") ||
    segments.includes("__tests__") ||
    name.includes(".Tests.") ||
    (extension !== "" && (name.endsWith("Test" + extension) || name.endsWith("Tests" + extension))) ||
    ((extension === ".ts" || extension === ".tsx" || extension === ".js" || extension === ".svelte") && name.includes(".spec."))
  ) {
    return "tests";
  }
  if (name === "Caddyfile" || name.startsWith("Dockerfile")) {
    return "operations";
  }
  if (segments.some((segment) => segment.toLowerCase() === "api-client")) {
    return "frontend-data";
  }
  if (extension === ".svelte" || extension === ".razor" || extension === ".kt") {
    return "frontend-ui";
  }
  if (
    extension === ".csproj" ||
    extension === ".props" ||
    extension === ".targets" ||
    extension === ".sln" ||
    extension === ".slnx"
  ) {
    return "workspace";
  }
  if (segments.includes("Migrations") || name.endsWith("Migration.cs") || name.endsWith("Snapshot.cs")) {
    return "infrastructure";
  }
  for (const segment of segments) {
    if (segment === "Domain") return "domain";
    if (segment === "Application" || segment === "Composition" || segment === "Contracts" || segment === "Jobs") {
      return "application";
    }
    if (segment === "Infrastructure") return "infrastructure";
    if (segment === "WebApi") return "web-api";
  }
  for (const segment of segments) {
    const lower = segment.toLowerCase();
    if (lower.endsWith(".domain")) return "domain";
    if (lower.endsWith(".application") || lower.endsWith(".worker")) return "application";
    if (lower.endsWith(".infrastructure")) return "infrastructure";
    if (lower.endsWith(".webapi")) return "web-api";
  }
  if (extension !== ".cs") return "";
  return isWebApiName(name) ? "web-api" : "application";
}

/** The comparator for the active sort; the table holds every sort key, so the last arm never runs. */
function compareFor(book: Book): (a: string, b: string) => number {
  return book.sorts[book.sortKey]?.cmp ?? book.sorts.outline?.cmp ?? (() => 0);
}

/** One record row and, when open, its children grouped by record field. */
function walkRecord(book: Book, id: string, depth: number, rows: OutlineRow[]): void {
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
function walkGroups(book: Book, kids: string[], parent: string, depth: number, rows: OutlineRow[]): void {
  const groups: { key: string; label: string; list: string[] }[] = [];
  const byKey: Record<string, { key: string; label: string; list: string[] }> = {};
  for (const id of kids) {
    const g = groupFieldOf(book, id);
    let key: string;
    let label: string;
    if (g && g[0]) {
      key = "field:" + g.join("/");
      label = g.filter(Boolean).join(": ");
    } else {
      const t = book.typeOf(id);
      key = "type:" + t;
      const q = book.lex.types?.[t];
      label = q ? q.plural || q.label || t : capitalize(t) + "s";
    }
    let entry = byKey[key];
    if (!entry) {
      entry = { key, label, list: [] };
      byKey[key] = entry;
      groups.push(entry);
    }
    entry.list.push(id);
  }
  const filtering = book.anyFilter();
  for (const g of groups) {
    const list = g.list.slice();
    if (book.sortKey !== "outline") list.sort(compareFor(book));
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
          text: "Calls " + book.title(stripStep(calls) ?? "")
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
