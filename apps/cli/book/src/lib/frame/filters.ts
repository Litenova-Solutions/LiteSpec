// Facet, search and sort readers: the navigation filters, the visible map and the sorts.

import type { BookRecord, ProofState, SortKey } from "./shard-types";
import { proofLabel, proofRank } from "./proof-order";
import { stripStep } from "./text";

/** One navigation facet: its key, label, values per record and words per value. */
export interface FacetDef {
  key: string;
  label: string;
  get(id: string): string[];
  name(value: string): string;
}

/** The readers the facets need from the store. */
export interface FacetReaders {
  get(id: string): BookRecord | undefined;
  typeOf(id: string): string;
  typeName(type: string): string;
  proofState(id: string): ProofState | null;
  layerOf: Record<string, string>;
  layerLabel(key: string): string;
}

function isProofState(value: string): value is ProofState {
  return (
    value === "passing" ||
    value === "failing" ||
    value === "awaiting" ||
    value === "unconfirmed" ||
    value === "unproved" ||
    value === "indirect" ||
    value === "norun"
  );
}

/** The navigation facets: record layer, type, findings and proof. */
export function createFacets(readers: FacetReaders): FacetDef[] {
  return [
    {
      key: "layer",
      label: "Record layer",
      get: (id) => {
        const layer = readers.layerOf[id];
        return layer ? [layer] : [];
      },
      name: (value) => readers.layerLabel(value),
    },
    {
      key: "type",
      label: "Type",
      get: (id) => [readers.typeOf(id)],
      name: (value) => readers.typeName(value),
    },
    {
      key: "findings",
      label: "Findings",
      get: (id) => {
        const findings = readers.get(id)?.findings ?? [];
        if (findings.length === 0) return [];
        return findings.some((finding) => finding.covered !== true) ? ["finding"] : ["covered"];
      },
      name: (value) => (value === "finding" ? "Finding" : "Covered"),
    },
    {
      key: "proof",
      label: "Proof",
      get: (id) => {
        const state = readers.proofState(id);
        return state ? [state] : [];
      },
      name: (value) => (isProofState(value) ? proofLabel(value) : value),
    },
  ];
}

/** Whether any filter applies: a query or a chosen facet value. */
export function hasAnyFilter(query: string, active: Record<string, string[]>): boolean {
  if (query) return true;
  return Object.values(active).some((values) => values !== undefined && values.length > 0);
}

/** Whether a record passes the active facets and the search query. */
export function passesFiltersFor(
  facets: FacetDef[],
  active: Record<string, string[]>,
  query: string,
  hits: Map<string, number> | null,
  titleOf: (id: string) => string,
  id: string,
  skip?: string,
): boolean {
  for (const facet of facets) {
    const selected = active[facet.key];
    if (selected === undefined || selected.length === 0 || facet.key === skip) continue;
    const values = facet.get(id);
    if (values.length > 0 && selected.some((value) => values.includes(value)) === false) return false;
  }
  if (query) {
    const hit = hits?.has(stripStep(id)) ?? titleOf(id).toLowerCase().includes(query.toLowerCase());
    if (hit === false) return false;
  }
  return true;
}

/** The visible records: 2 for a direct match, 1 for an ancestor kept to show where a match sits. */
export function visibleMapFor(
  order: string[],
  parentOf: Record<string, string | null>,
  passes: (id: string) => boolean,
  anyFilter: boolean,
): Map<string, 1 | 2> {
  const map = new Map<string, 1 | 2>();
  if (anyFilter === false) return map;
  for (const id of order) {
    if (passes(id) === false) continue;
    map.set(id, 2);
    let parent = parentOf[id];
    while (parent !== undefined && parent !== null && map.has(parent) === false) {
      map.set(parent, 1);
      parent = parentOf[parent];
    }
  }
  return map;
}

/** The record Enter opens from the search box: the best title hit, else the best hit. */
export function bestSearchHitFor(
  order: string[],
  passes: (id: string) => boolean,
  hits: Map<string, number> | null,
  query: string,
  titleOf: (id: string) => string,
): string | null {
  const all = order.filter((id) => passes(id));
  if (hits) {
    all.sort((a, b) => (hits.get(stripStep(b)) ?? 0) - (hits.get(stripStep(a)) ?? 0));
  }
  const words = query.toLowerCase().split(/\s+/);
  return all.find((id) => words.every((word) => titleOf(id).toLowerCase().includes(word))) ?? all[0] ?? null;
}

/** The readers the sorts need from the store. */
export interface SortReaders {
  title(id: string): string;
  indexOf(id: string): number;
  proofState(id: string): ProofState | null;
}

/** The navigation sorts: outline order, title, and proof with failing first. */
export function createSorts(readers: SortReaders): Record<SortKey, { label: string; cmp: (a: string, b: string) => number }> {
  return {
    outline: {
      label: "Outline order",
      cmp: (a, b) => readers.indexOf(a) - readers.indexOf(b),
    },
    title: {
      label: "Title",
      cmp: (a, b) => readers.title(a).localeCompare(readers.title(b)),
    },
    proof: {
      label: "Proof, failing first",
      cmp: (a, b) => proofRank(readers.proofState(a)) - proofRank(readers.proofState(b)) || readers.indexOf(a) - readers.indexOf(b),
    },
  };
}
