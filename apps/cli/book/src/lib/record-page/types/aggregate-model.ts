// The aggregate page model: the states with their reachability, the transitions
// grouped by origin, and the data-class, data and rule rows. The component
// resolves the linked rules; the model reads only values.

import { capitalize, dedupe, esc, formatLabel } from "$lib/frame";
import type { Book, BookRecord, RecordFields } from "$lib/frame";
import { codeCls, inlineRefHtml, tdHtml } from "../page-html";
import { aggregateFields, clsBadgeHtml, periodSuffixHtml, stringList } from "../page-values";

/** One rule of an aggregate, with its record and refusal resolved. */
export interface AggregateRuleInput {
  id: string;
  statement: string;
  kind: string;
  refusalCode: string | null;
  refusalId: string | null;
}

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
  classRows: string[][];
  dataRows: string[][];
  ruleRows: string[][];
}

export function aggregateModel(book: Book, f: RecordFields, r: BookRecord, rules: AggregateRuleInput[]): AggregateModel {
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
  const classRows = af.dataClasses.map((c) => [
    tdHtml(`<b>${esc(capitalize(formatLabel(c.name)))}</b><div class="mt-1">${clsBadgeHtml(c.classification)}</div>`),
    tdHtml(`<div class="flex flex-wrap gap-1">${stringList(c, "fields").map((x) => `<span class="${codeCls}">${esc(x)}</span>`).join("")}</div>`),
    tdHtml(esc(c.purpose ?? "")),
    tdHtml(
      `<div class="text-xs"><b>${esc(capitalize(String(c.onErasure ?? "")))}</b>${periodSuffixHtml(c.period)}</div>` +
        (c.basis || c.reason
          ? `<div class="mt-0.5 text-xs text-slate-600">${esc(String(c.basis ?? c.reason ?? ""))}</div>`
          : ""),
    ),
  ]);
  const dataRows = af.data.map((d) => [
    tdHtml(esc(String(d.name ?? "")), "nm"),
    tdHtml(esc(String(d.meaning ?? ""))),
    tdHtml(clsBadgeHtml(d.classification)),
  ]);
  const ruleRows = rules.map((x) => [
    tdHtml(inlineRefHtml(book, x.id) + `<div class="mt-0.5 text-xs text-slate-600">${esc(x.statement)}</div>`),
    tdHtml(`<span class="font-medium text-slate-900">${esc(capitalize(x.kind))}</span>`),
    tdHtml(
      x.refusalCode
        ? `<span class="${codeCls}">${esc(x.refusalCode)}</span>`
        : x.refusalId
          ? inlineRefHtml(book, x.refusalId)
          : '<span class="font-mono text-xs text-slate-600">none</span>',
    ),
  ]);
  return { states, transitions, classRows, dataRows, ruleRows };
}
