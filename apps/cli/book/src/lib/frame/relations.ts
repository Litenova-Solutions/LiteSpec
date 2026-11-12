// Pure relation readers: edges, trace, connections and breadcrumbs from records.

import type { BookRecord, LinkEdge, RecordFields } from "./shard-types";
import { MODULE_MEMBER_TYPES } from "./shard-types";
import { stripStep } from "./text";

/** One placed relation: its label, the linked record and how the link reads. */
export interface Connection {
  label: string;
  id: string;
  via: string;
}

/** The readers the relation judgements need from the store. */
export interface RelationReaders {
  get(id: string): BookRecord | undefined;
  has(id: string): boolean;
  title(id: string): string;
  typeOf(id: string): string;
  parent(id: string): string | null;
  fields(id: string): RecordFields;
  isReal(id: string): boolean;
  resolveField(field: string): string | null;
  phraseFor(field: string, carrierType: string | null, dir: "forward" | "inverse"): string;
  typeName(type: string): string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && Array.isArray(value) === false;
}

/** A record's link edges: the link field and the linked record id. */
export function edgesFor(record: BookRecord | undefined, key: "out" | "in"): LinkEdge[] {
  const raw = record?.[key];
  if (raw === undefined) return [];
  const out: LinkEdge[] = [];
  for (const edge of raw) {
    const field = edge.key;
    const id = stripStep(edge.id);
    if (field && id) out.push({ field, id });
  }
  return out;
}

/** Whether an edge field names a field, directly or through nesting. */
export function edgeMatches(edgeField: string, field: string): boolean {
  return edgeField === field || edgeField.endsWith("." + field);
}

/** The records a record links to through a field. */
export function outsFor(readers: Pick<RelationReaders, "get">, id: string, field: string): string[] {
  return edgesFor(readers.get(id), "out")
    .filter((edge) => edgeMatches(edge.field, field))
    .map((edge) => edge.id);
}

/** The records linking to a record through a field. */
export function insFor(readers: Pick<RelationReaders, "get">, id: string, field: string): string[] {
  return edgesFor(readers.get(id), "in")
    .filter((edge) => edgeMatches(edge.field, field))
    .map((edge) => edge.id);
}

/** The actors that may perform a record, through its access grant and roles. */
export function performersFor(
  readers: Pick<RelationReaders, "fields" | "get" | "title">,
  id: string,
): { id: string; via: string }[] {
  const access = readers.fields(id)["access"];
  const grant = isRecord(access) ? String(access["grant"] ?? "") : "";
  if (grant.startsWith("permission/") === false) return [];
  const out: { id: string; via: string }[] = [];
  for (const role of insFor(readers, grant, "allows")) {
    for (const actor of insFor(readers, role, "holds")) {
      out.push({ id: actor, via: "via " + readers.title(grant) + ", role " + readers.title(role) });
    }
  }
  return out;
}

/** The record above one in the goal-to-behavior trace, or nothing when it tops out. */
export function upOneFor(
  readers: Pick<RelationReaders, "get" | "has" | "isReal" | "parent" | "typeOf">,
  id: string,
): string | null {
  const type = readers.typeOf(id);
  const known = (ids: string[]): string | null => {
    for (const raw of ids) {
      const candidate = stripStep(raw);
      if (readers.has(candidate)) return candidate;
    }
    return null;
  };
  if (type === "scenario" || type === "result") {
    const parent = readers.parent(id);
    return parent && readers.get(parent) && readers.isReal(parent) ? parent : null;
  }
  if (type === "screen") return known(outsFor(readers, id, "serves")) ?? known(outsFor(readers, id, "offers"));
  if (MODULE_MEMBER_TYPES.includes(type)) {
    return known(outsFor(readers, id, "fulfills")) ?? known(outsFor(readers, id, "serves"));
  }
  if (type === "journey") return known(outsFor(readers, id, "cites")) ?? known(outsFor(readers, id, "serves"));
  if (type === "decision") return known(outsFor(readers, id, "decides"));
  if (type === "requirement") return known(outsFor(readers, id, "cites"));
  if (type === "feature") return known(outsFor(readers, id, "advances")) ?? known(outsFor(readers, id, "solves"));
  if (type === "event") {
    const first = insFor(readers, id, "raises")[0];
    return first ?? null;
  }
  return null;
}

/** A record's trace from its goal down to itself, or nothing when it stands alone. */
export function traceFor(
  readers: Pick<RelationReaders, "get" | "has" | "isReal" | "parent" | "typeOf">,
  id: string,
): string[] {
  const chain = [id];
  let current = id;
  for (let step = 0; step < 8; step++) {
    const up = upOneFor(readers, current);
    if (up === null || chain.includes(up)) break;
    chain.unshift(up);
    current = up;
  }
  return chain.length > 1 ? chain : [];
}

/** Every link of a record by its field; a record pointing here reads "Points at this: field". */
export function connectionsFor(
  readers: Pick<RelationReaders, "get" | "parent" | "resolveField">,
  id: string,
): Connection[] {
  const rows: Connection[] = [];
  const seen = new Set<string>();
  const parent = readers.parent(id);
  const push = (label: string, target: string, via = ""): void => {
    if (target === id || seen.has(label + "|" + target)) return;
    seen.add(label + "|" + target);
    rows.push({ label, id: target, via });
  };
  for (const edge of edgesFor(readers.get(id), "out")) {
    if (edge.id !== parent) push(readers.resolveField(edge.field) || edge.field, edge.id);
  }
  for (const edge of edgesFor(readers.get(id), "in")) {
    if (edge.id !== parent) push("Points at this: " + (readers.resolveField(edge.field) || edge.field), edge.id);
  }
  return rows;
}

/** One link field read as a sentence, in the Links table phrases. */
export function linkSentenceFor(
  readers: Pick<RelationReaders, "phraseFor" | "typeName" | "typeOf">,
  id: string,
  label: string,
  items: Connection[],
): string {
  const inbound = label.startsWith("Points at this: ");
  const field = inbound ? label.slice(16) : label;
  const me = readers.typeName(readers.typeOf(id)).toLowerCase();
  if (inbound === false) return "This " + me + " " + readers.phraseFor(field, readers.typeOf(id), "forward");
  const first = items.length > 0 ? items[0] : undefined;
  return "This " + me + " " + readers.phraseFor(field, first ? readers.typeOf(first.id) : null, "inverse");
}

/** The real records and record types in a layer, for the layer header and page. */
export function layerStats(
  readers: Pick<RelationReaders, "isReal" | "typeOf"> & { order: string[]; layerOf: Record<string, string> },
  layer: string,
): { ids: string[]; types: string[] } {
  const ids = readers.order.filter((id) => readers.layerOf[id] === layer && readers.isReal(id));
  const types = [...new Set(ids.map((id) => readers.typeOf(id)))];
  return { ids, types };
}

/** A record's breadcrumb: the ids from the root down to its parent, excluding itself. */
export function breadcrumbFor(readers: Pick<RelationReaders, "parent">, id: string): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  let current = readers.parent(id);
  while (current && seen.has(current) === false) {
    seen.add(current);
    out.unshift(current);
    current = readers.parent(current);
  }
  return out;
}
