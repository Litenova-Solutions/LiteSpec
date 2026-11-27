// The aggregate page model: the states with their reachability, the transitions
// grouped by origin. The component draws the data-class, data and rule tables
// itself; the model reads only values.

import { dedupe } from "$lib/frame";
import type { BookRecord, RecordFields } from "$lib/frame";
import { aggregateFields } from "../page-values";

export interface AggregateStateRow {
  name: string;
  meaning: string;
  created: boolean;
  unreached: boolean;
}

export interface AggregateTransitionTarget {
  to: string;
  movedBy: (string | null)[];
}

export interface AggregateTransition {
  from: string;
  created: boolean;
  targets: AggregateTransitionTarget[];
}

export interface AggregateModel {
  states: AggregateStateRow[];
  transitions: AggregateTransition[];
}

export function aggregateModel(f: RecordFields, r: BookRecord): AggregateModel {
  const af = aggregateFields(f);
  const reach = r.reachable ?? [];
  const starts = new Set(af.transitions.filter((t) => t.from === "-").map((t) => String(t.to)));
  const states = af.states.map((s) => ({
    name: String(s.name ?? ""),
    meaning: String(s.meaning ?? ""),
    created: starts.has(String(s.name)),
    unreached: reach.length > 0 && !reach.includes(String(s.name)),
  }));
  const froms = dedupe(af.transitions.map((t) => String(t.from)));
  const transitions = froms.map((fr) => {
    const tos = dedupe(af.transitions.filter((t) => String(t.from) === fr).map((t) => String(t.to)));
    return {
      from: fr,
      created: fr === "-",
      targets: tos.map((to) => ({
        to,
        movedBy: af.transitions
          .filter((t) => String(t.from) === fr && String(t.to) === to)
          .map((t) => (t.movedBy ? String(t.movedBy) : null)),
      })),
    };
  });
  return { states, transitions };
}
