// The rail's local model: the goal-to-behavior trace, the words for
// implementation kinds, and the connections grouped by layer and relation.
// Every string comes from book.js; the store supplies records and relations.
import { book, capitalize, pluralize, stripStep } from "$lib/frame";
import type { Connection } from "$lib/frame";

export const GAP: Record<string, string> = {
  goal: "No feature serves this goal",
  feature: "No requirement cites this feature",
};

const BAND: Record<string, string> = {
  intent: "Intent",
  behavior: "Behavior",
  entry: "",
  governance: "Governance",
};

export function bandOf(id: string): string {
  const t = book.typeOf(id);
  if (t === "code" || t === "route") return "Implementation";
  if (t === "test") return "Proof";
  return BAND[book.kindOf(id)] || "Behavior";
}

const KIND_WORD: Record<string, string> = {
  handler: "Handler",
  endpoint: "Endpoint",
  "aggregate-root": "Aggregate root",
  "aggregate-method": "Aggregate method",
  automation: "Automation",
  reaction: "Reaction",
  job: "Job",
  port: "Port",
  adapter: "Adapter",
  event: "Event class",
  "rule-exception": "Refusal exception",
  result: "Result code",
  "domain-service": "Domain service",
  route: "Route",
};

export const kindWord = (kind: string) => KIND_WORD[kind] || capitalize(kind);

export function keptByOf(id: string): Record<string, number> {
  return (book.get(stripStep(id))?.keptBy ?? {}) as Record<string, number>;
}

export interface DownEntry {
  id: string;
  phrase: string;
}

// Down the authority order: what each record type leads to in the layer below,
// with the words for that step.
export function downOf(id: string): DownEntry[] {
  void book.rev;
  const r = book.get(id);
  const t = book.typeOf(id);
  const out: DownEntry[] = [];
  const add = (list: readonly string[] | null | undefined, phrase: string) => {
    for (const raw of list ?? []) {
      const x = stripStep(raw);
      if (book.has(x) && x !== stripStep(id)) out.push({ id: x, phrase });
    }
  };
  if (t === "product") add(book.order.filter((x) => book.typeOf(x) === "goal"), "a goal of the product");
  if (t === "goal") add(book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature"), "serves this goal");
  if (t === "problem") add(book.ins(id, "solves"), "solves this problem");
  if (t === "feature") add(book.ins(id, "cites"), "cites this feature");
  if (t === "requirement") add(book.ins(id, "fulfills"), "fulfills this requirement");
  if (t === "journey") add(book.ins(id, "serves"), "serves a step");
  if (t === "use-case" || t === "reaction" || t === "job") {
    add(book.ins(id, "offers"), "offers it");
    add(book.resultsOf(id), "how it ends");
  }
  if (t === "result") add(book.shownByOf(id), "shows this result");
  if (t === "scenario" || t === "rule") add((r?.provedBy ?? []) as string[], "proves a scenario of it");
  if (t === "screen") add((r?.implementedBy ?? []) as string[], "implements it");
  if (t === "aggregate") add(book.ins(id, "changes"), "changes it");
  return out;
}

export type TraceRow =
  | { kind: "rec"; id: string; depth: number; phrase: string; here: boolean; showPh: boolean }
  | { kind: "note"; note: string; depth: number; band: string }
  | { kind: "gap"; gap: string; depth: number; band: string }
  | { kind: "more"; more: number; depth: number; t: string };

export interface TraceBand {
  name: string;
  rows: TraceRow[];
}

export interface TraceModel {
  rows: TraceRow[];
  show: boolean;
  shown: Set<string>;
  bands: TraceBand[];
}

export function buildTrace(id: string): TraceModel {
  void book.rev;
  const up = book.traceOf(id).filter((x) => x !== id);
  const seen: Record<string, boolean> = {};
  const rows: TraceRow[] = [];
  // Above a use case, results and scenarios are walked through but not listed,
  // so the trace reaches code and tests without the detail.
  const quiet = !["use-case", "reaction", "job", "result", "scenario", "rule"].includes(book.typeOf(id));
  for (const x of up) {
    seen[x] = true;
    rows.push({ kind: "rec", id: x, depth: 0, phrase: "", here: false, showPh: false });
  }
  seen[id] = true;
  rows.push({ kind: "rec", id, depth: 0, phrase: "", here: true, showPh: false });
  const walk = (x: string, depth: number): void => {
    if (depth > 5) return;
    const kids = downOf(x);
    if (book.typeOf(x) === "requirement" && book.get(stripStep(x))) {
      const kb = keptByOf(x);
      const parts = Object.keys(kb).map((t) => pluralize(kb[t] ?? 0, book.typeName(t).toLowerCase()));
      rows.push(
        parts.length
          ? { kind: "note", note: book.title(x) + ": kept by " + parts.join(", "), depth: depth + 1, band: "Behavior" }
          : { kind: "gap", gap: "Nothing keeps this requirement", depth: depth + 1, band: "Intent" },
      );
    }
    if (!kids.length && GAP[book.typeOf(x)]) {
      rows.push({ kind: "gap", gap: GAP[book.typeOf(x)], depth: depth + 1, band: bandOf(x) });
    }
    const by: Record<string, DownEntry[]> = {};
    for (const k of kids) {
      if (seen[k.id]) continue;
      (by[book.typeOf(k.id)] ??= []).push(k);
    }
    for (const t of Object.keys(by)) {
      const hide = quiet && (t === "result" || t === "scenario");
      by[t].forEach((k, i) => {
        seen[k.id] = true;
        if (hide) {
          walk(k.id, depth);
          return;
        }
        if (i < 3) {
          rows.push({ kind: "rec", id: k.id, depth: depth + 1, phrase: k.phrase, here: false, showPh: false });
          walk(k.id, depth + 1);
        } else if (i === 3) {
          rows.push({ kind: "more", more: by[t].length - 3, depth: depth + 1, t });
        }
      });
    }
  };
  walk(id, 0);
  const bands: TraceBand[] = ["Intent", "Behavior"].map((b) => {
    const here = rows.filter((x) =>
      x.kind === "note" || x.kind === "gap" ? x.band === b : x.kind === "rec" ? bandOf(x.id) === b : false,
    );
    let lastPh = "";
    for (const x of here) {
      if (x.kind !== "rec") continue;
      x.showPh = !!x.phrase && x.phrase + x.depth !== lastPh;
      lastPh = (x.phrase || "") + x.depth;
    }
    return { name: b, rows: here };
  });
  const shown = new Set<string>();
  for (const x of rows) if (x.kind === "rec") shown.add(x.id);
  return { rows, show: rows.length >= 2, shown, bands };
}
