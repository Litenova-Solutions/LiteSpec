// The trace's local model: the goal-to-behavior trace, the words for
// implementation kinds, and the kept-by sentences. Every band word comes from
// the lexicon's layer labels; the store supplies records and relations.
import { capitalize, isFlowType, pluralize, stripStep } from "$lib/frame";
import type { Book, BookRecord, BookTest, ImplKind } from "$lib/frame";

/** A goal nothing serves reads as a gap in the trace and the Intent tree. */
export const GOAL_GAP_TEXT = "No feature serves this goal";

/** A feature nothing cites reads as a gap in the trace and the Intent tree. */
export const FEATURE_GAP_TEXT = "No requirement cites this feature";

/** A requirement nothing keeps reads as a gap in the trace and the Intent tree. */
export const GAP_TEXT = "Nothing keeps this requirement";

/** A goal or feature with nothing below it reads as a gap in the trace. */
const GAP: Record<string, string> = {
  goal: GOAL_GAP_TEXT,
  feature: FEATURE_GAP_TEXT,
};

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
} satisfies Record<ImplKind, string>;

/** The words for an implementation kind. Forward tolerance: a kind the lexicon does not name yet reads capitalized. */
export function kindWord(kind: string): string {
  return KIND_WORD[kind] ?? capitalize(kind);
}

/** The trace band a record reads in, from the lexicon's layer labels. */
export function bandOf(book: Book, id: string): string {
  const t = book.typeOf(id);
  if (t === "code" || t === "route") return book.layerByKey.implementation?.label ?? "Implementation";
  if (t === "test") return book.layerByKey.proof?.label ?? "Proof";
  const kind = book.kindOf(id);
  if (kind === "unlayered") return book.layerByKey.behavior?.label ?? "Behavior";
  return book.layerByKey[kind]?.label ?? capitalize(kind);
}

/** What keeps a requirement, or null when nothing keeps it. */
export function keptBySentence(book: Book, id: string): string | null {
  const keptBy = book.get(stripStep(id))?.keptBy ?? {};
  const parts = Object.keys(keptBy).map((t) => pluralize(keptBy[t] ?? 0, book.typeName(t).toLowerCase()));
  return parts.length ? "kept by " + parts.join(", ") : null;
}

/** Whether the Intent chain breaks at a record: a goal or feature with nothing below it, or a requirement nothing keeps. */
export function breaksIntentChain(book: Book, id: string, childCount: number): boolean {
  const t = book.typeOf(id);
  if (t === "goal" || t === "feature") return childCount === 0;
  if (t === "requirement") return keptBySentence(book, id) === null;
  return false;
}

/** The stable key for an implementation entry: its path, kind and line. */
export function implKey(entry: NonNullable<BookRecord["impl"]>[number]): string {
  return (entry.path ?? "") + "|" + (entry.kind ?? "") + "|" + (entry.line ?? "");
}

/** The stable key for a proof test: its name and path. */
export function proofTestKey(test: BookTest): string {
  return (test.name ?? "") + "|" + (test.path ?? test.file ?? "");
}

export interface DownEntry {
  id: string;
  phrase: string;
}

// Down the authority order: what each record type leads to in the layer below,
// with the words for that step.
export function downOf(book: Book, id: string): DownEntry[] {
  void book.rev;
  const t = book.typeOf(id);
  const out: DownEntry[] = [];
  const add = (list: readonly string[] | null | undefined, phrase: string) => {
    for (const raw of list ?? []) {
      const x = stripStep(raw) ?? "";
      if (book.has(x) && x !== (stripStep(id) ?? "")) out.push({ id: x, phrase });
    }
  };
  if (t === "product") add(book.order.filter((x) => book.typeOf(x) === "goal"), "a goal of the product");
  if (t === "product") add(book.order.filter((x) => book.typeOf(x) === "vision"), "a vision of the product");
  if (t === "vision") {
    add(book.ins(id, "basedOn"), "is based on this vision");
    add(book.ins(id, "cites"), "cites this vision");
  }
  if (t === "goal") add(book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature"), "serves this goal");
  if (t === "problem") add(book.kids(id).filter((child) => book.typeOf(child) === "feature"), "answers this problem");
  if (t === "feature") add(book.ins(id, "cites"), "cites this feature");
  if (t === "requirement") add(book.ins(id, "fulfills"), "fulfills this requirement");
  if (t === "journey") add(book.ins(id, "serves"), "serves a step");
  if (isFlowType(t)) {
    add(book.ins(id, "offers"), "offers it");
    add(book.resultsOf(id), "how it ends");
  }
  if (t === "result") add(book.shownByOf(id), "shows this result");
  if (t === "aggregate") add(book.ins(id, "changes"), "changes it");
  return out;
}

export type TraceRow =
  | { kind: "rec"; id: string; depth: number; phrase: string; here: boolean; showPh: boolean }
  | { kind: "note"; note: string; depth: number; band: string }
  | { kind: "gap"; gap: string; depth: number; band: string }
  | { kind: "more"; more: number; depth: number; t: string };

/** The stable key for a trace row: the record id, or the band text it shows. */
export function traceRowKey(row: TraceRow): string {
  if (row.kind === "rec") return "rec:" + row.id;
  if (row.kind === "note") return "note:" + row.note;
  if (row.kind === "gap") return "gap:" + row.gap;
  return "more:" + row.t + ":" + row.more;
}

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

export function buildTrace(book: Book, id: string): TraceModel {
  void book.rev;
  const intentBand = book.layerByKey.intent?.label ?? "Intent";
  const behaviorBand = book.layerByKey.behavior?.label ?? "Behavior";
  const up = book.traceOf(id).filter((x) => x !== id);
  const seen: Record<string, boolean> = {};
  const rows: TraceRow[] = [];
  // Above a use case, results and scenarios are walked through but not listed,
  // so the trace reaches code and tests without the detail.
  const top = book.typeOf(id);
  const quiet = !isFlowType(top) && top !== "result" && top !== "scenario" && top !== "rule";
  for (const x of up) {
    seen[x] = true;
    rows.push({ kind: "rec", id: x, depth: 0, phrase: "", here: false, showPh: false });
  }
  seen[id] = true;
  rows.push({ kind: "rec", id, depth: 0, phrase: "", here: true, showPh: false });
  const walk = (x: string, depth: number): void => {
    if (depth > 5) return;
    const kids = downOf(book, x);
    if (book.typeOf(x) === "requirement" && book.get(stripStep(x))) {
      const sentence = keptBySentence(book, x);
      rows.push(
        sentence
          ? { kind: "note", note: book.title(x) + ": " + sentence, depth: depth + 1, band: behaviorBand }
          : { kind: "gap", gap: GAP_TEXT, depth: depth + 1, band: intentBand },
      );
    }
    const gapText = GAP[book.typeOf(x)];
    if (!kids.length && gapText) {
      rows.push({ kind: "gap", gap: gapText, depth: depth + 1, band: bandOf(book, x) });
    }
    const by: Record<string, DownEntry[]> = {};
    for (const k of kids) {
      if (seen[k.id]) continue;
      const key = book.typeOf(k.id);
      const entries = by[key] ?? [];
      entries.push(k);
      by[key] = entries;
    }
    for (const t of Object.keys(by)) {
      const entries = by[t] ?? [];
      const hide = quiet && (t === "result" || t === "scenario");
      entries.forEach((k, i) => {
        seen[k.id] = true;
        if (hide) {
          walk(k.id, depth);
          return;
        }
        if (i < 3) {
          rows.push({ kind: "rec", id: k.id, depth: depth + 1, phrase: k.phrase, here: false, showPh: false });
          walk(k.id, depth + 1);
        } else if (i === 3) {
          rows.push({ kind: "more", more: entries.length - 3, depth: depth + 1, t });
        }
      });
    }
  };
  walk(id, 0);
  const bands: TraceBand[] = [intentBand, behaviorBand].map((b) => {
    const here = rows.filter((x) =>
      x.kind === "note" || x.kind === "gap" ? x.band === b : x.kind === "rec" ? bandOf(book, x.id) === b : false,
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
