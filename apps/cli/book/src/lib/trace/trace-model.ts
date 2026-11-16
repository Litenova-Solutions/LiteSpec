// The trace's local model: the goal-to-behavior trace, the words for
// implementation kinds, and the kept-by sentences. Every band word comes from
// the lexicon's layer labels; the store supplies records and relations.
import { capitalize, completionLines, isScenarioType, pluralize, stageLine, stripStep } from "$lib/frame";
import type { Book, BookRecord, BookTest, ImplKind } from "$lib/frame";

/** A goal nothing serves reads as a gap in the trace and the Intent tree. */
export const GOAL_GAP_TEXT = "No feature serves this goal";

/** A feature with no requirement in its folder reads as a gap in the trace and the Intent tree. */
export const FEATURE_GAP_TEXT = "This feature has no requirements";

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

/** Which way a trace edge points from the walked record: its out edge, its in edge, or containment with no edge. */
export type TraceEdgeDir = "out" | "in" | "contains";

export interface DownEntry {
  id: string;
  phrase: string;
  field: string | null;
  dir: TraceEdgeDir;
}

// Down the authority order: what each record type leads to in the layer below,
// with the words for that step.
export function downOf(book: Book, id: string): DownEntry[] {
  void book.rev;
  const t = book.typeOf(id);
  const self = stripStep(id) ?? "";
  const out: DownEntry[] = [];
  // A record that carries the link reads its phrase from the lexicon's Links table, by the field it carries.
  const linked = (list: readonly string[] | null | undefined, field: string) => {
    for (const raw of list ?? []) {
      const x = stripStep(raw) ?? "";
      if (book.has(x) && x !== self) {
        out.push({ id: x, phrase: book.phraseFor(field, book.typeOf(x), "inverse"), field, dir: "in" });
      }
    }
  };
  // Records reached by containment, not by a link field, read in the trace's own words.
  const contained = (list: readonly string[], phrase: string) => {
    for (const raw of list) {
      const x = stripStep(raw) ?? "";
      if (book.has(x) && x !== self) out.push({ id: x, phrase, field: null, dir: "contains" });
    }
  };
  if (t === "product") contained(book.order.filter((x) => book.typeOf(x) === "goal"), "a goal of the product");
  if (t === "product") contained(book.order.filter((x) => book.typeOf(x) === "direction"), "a direction of the product");
  if (t === "direction") {
    linked(book.ins(id, "basedOn"), "basedOn");
    linked(book.ins(id, "cites"), "cites");
  }
  if (t === "goal") linked(book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature"), "advances");
  if (t === "problem") linked(book.ins(id, "solves").filter((x) => book.typeOf(x) === "feature"), "solves");
  if (t === "feature") contained(book.kids(id).filter((x) => book.typeOf(x) === "requirement"), "a requirement of the feature");
  if (t === "requirement") linked(book.ins(id, "fulfills"), "fulfills");
  if (t === "flow") linked(book.ins(id, "serves"), "serves");
  if (isScenarioType(t)) {
    linked(book.ins(id, "offers"), "offers");
    contained(book.resultsOf(id), "how it ends");
  }
  if (t === "result") contained(book.shownByOf(id), "shows this result");
  if (t === "aggregate") linked(book.ins(id, "writes"), "writes");
  return out;
}

export type TraceRow =
  | { kind: "rec"; id: string; depth: number; phrase: string; here: boolean; showPh: boolean; field: string | null; dir: TraceEdgeDir; via: string | null }
  | { kind: "note"; note: string; depth: number; band: string }
  | { kind: "gap"; gap: string; depth: number; band: string }
  | { kind: "more"; more: number; depth: number; t: string; band: string; field: string | null; dir: TraceEdgeDir; phrase: string; via: string | null };

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

/** The walk edge from one trace record to the next: the carrier's out edge first, else its in edge, else containment. */
function walkEdge(book: Book, carrierId: string, nextId: string): { field: string | null; dir: TraceEdgeDir; phrase: string } {
  const next = stripStep(nextId);
  for (const edge of book.outEdges(carrierId)) {
    if (edge.id === next) {
      return { field: edge.field, dir: "out", phrase: book.phraseFor(edge.field, book.typeOf(carrierId), "forward") };
    }
  }
  for (const edge of book.inEdges(carrierId)) {
    if (edge.id === next) {
      return { field: edge.field, dir: "in", phrase: book.phraseFor(edge.field, book.typeOf(next), "inverse") };
    }
  }
  return { field: null, dir: "contains", phrase: "" };
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
  const quiet = !isScenarioType(top) && top !== "result" && top !== "scenario" && top !== "rule";
  // Each record above the open one is reached from the record below it: the
  // last one above from the open record itself.
  up.forEach((x, i) => {
    seen[x] = true;
    rows.push({ kind: "rec", id: x, depth: 0, here: false, showPh: false, via: up[i + 1] ?? id, ...walkEdge(book, up[i + 1] ?? id, x) });
  });
  seen[id] = true;
  rows.push({ kind: "rec", id, depth: 0, phrase: "", here: true, showPh: false, field: null, dir: "contains", via: null });
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
    // Records reached together read together: grouped per relationship and
    // type, in first-seen order. Every reached record stays in the model; the
    // section caps each opened list with a Show N more button instead.
    const by: Record<string, DownEntry[]> = {};
    for (const k of kids) {
      if (seen[k.id]) continue;
      const key = (k.field ?? "") + "|" + k.dir + "|" + book.typeOf(k.id);
      const entries = by[key] ?? [];
      entries.push(k);
      by[key] = entries;
    }
    for (const key of Object.keys(by)) {
      const entries = by[key] ?? [];
      const first = entries[0];
      const t = first ? book.typeOf(first.id) : "";
      const hide = quiet && (t === "result" || t === "scenario");
      for (const k of entries) {
        seen[k.id] = true;
        if (hide) {
          walk(k.id, depth);
          continue;
        }
        rows.push({ kind: "rec", id: k.id, depth: depth + 1, phrase: k.phrase, here: false, showPh: false, field: k.field, dir: k.dir, via: x });
        walk(k.id, depth + 1);
      }
    }
  };
  walk(id, 0);
  const bands: TraceBand[] = [intentBand, behaviorBand].map((b) => {
    const here = rows.filter((x) => (x.kind === "rec" ? bandOf(book, x.id) : x.band) === b);
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

/** One displayed trace record with the records reached through it nested below. A top direct node reads from the open record's side; a nested node reads from its carrier's side. */
export interface RailNode {
  row: TraceRow;
  phrase: string;
  tip: string;
  carrierId: string | null;
  children: RailNode[];
}

/** One Trace section relationship: the directly linked records under one relationship label, each with its nested records. The field and direction read the link from the open record's side; containment carries no field. */
export interface RailRelation {
  key: string;
  label: string;
  tip: string;
  field: string | null;
  dir: TraceEdgeDir;
  phrase: string;
  nodes: RailNode[];
}

/** One Trace section group: the relationship groups under one band label, then the carriers whose children sit in this band with their nested records, with the band's gaps after them. */
export interface RailGroup {
  label: string;
  relations: RailRelation[];
  orphans: RailNode[];
  gaps: Extract<TraceRow, { kind: "gap" }>[];
}

/** One trace relationship label: its phrase in sentence case, as the record page's Links table reads it. */
export function relationLabel(phrase: string): string {
  return capitalize(phrase.replace(/^is\s+/i, "").replace(/^are\s+/i, ""));
}

/** One trace relationship read as a sentence: "This use case fulfills each record below". */
export function relationSentence(book: Book, openId: string, phrase: string): string {
  return "This " + book.typeName(book.typeOf(openId)).toLowerCase() + " " + phrase + " each record below";
}

/** One trace item read as a sentence: "This use case fulfills the requirement Name". */
export function traceSentence(book: Book, openId: string, phrase: string, rowId: string): string {
  return (
    "This " +
    book.typeName(book.typeOf(openId)).toLowerCase() +
    " " +
    phrase +
    " the " +
    book.typeName(book.typeOf(rowId)).toLowerCase() +
    " " +
    book.title(rowId)
  );
}

/** One nested trace item read as a sentence naming its carrier: "The requirement Name cites the decision Other". */
export function carrierSentence(book: Book, carrierId: string, phrase: string, rowId: string): string {
  return (
    "The " +
    book.typeName(book.typeOf(carrierId)).toLowerCase() +
    " " +
    book.title(carrierId) +
    " " +
    phrase +
    " the " +
    book.typeName(book.typeOf(rowId)).toLowerCase() +
    " " +
    book.title(rowId)
  );
}

/** The open record's direct edge to a trace record: its out edge first, else the record's edge pointing at it, else null. */
function directEdge(book: Book, openId: string, rowId: string): { field: string; dir: "out" | "in" } | null {
  const row = stripStep(rowId);
  for (const edge of book.outEdges(openId)) {
    if (edge.id === row) return { field: edge.field, dir: "out" };
  }
  for (const edge of book.inEdges(openId)) {
    if (edge.id === row) return { field: edge.field, dir: "in" };
  }
  return null;
}

// Record bands read in layer order; the Implementation and Proof groups follow in the section.
const RAIL_GROUP_ORDER = ["intent", "behavior", "governance"];

/** The open record's side of a direct link: the field, direction and phrase it reads with. Containment reads as contains. */
function openSide(book: Book, openId: string, rowId: string, walk: { field: string | null; dir: TraceEdgeDir; phrase: string }): { field: string | null; dir: TraceEdgeDir; phrase: string } {
  const direct = directEdge(book, openId, rowId);
  if (direct) {
    const phrase =
      direct.dir === "out"
        ? book.phraseFor(direct.field, book.typeOf(openId), "forward")
        : book.phraseFor(direct.field, book.typeOf(rowId), "inverse");
    return { field: direct.field, dir: direct.dir, phrase };
  }
  if (walk.dir === "contains") return { field: walk.field, dir: walk.dir, phrase: "contains" };
  return { field: walk.field, dir: walk.dir, phrase: walk.phrase };
}

/** Whether a trace record links directly to the open record: its own edge, or the open record as its carrier. */
function isDirect(book: Book, openId: string, row: Extract<TraceRow, { kind: "rec" }>): boolean {
  if (row.via === openId) return true;
  return directEdge(book, openId, row.id) !== null;
}

/** A carrier shown as a nesting parent: its tooltip reads from the open record when direct, else names its own carrier. */
function carrierParentTip(book: Book, openId: string, carrierId: string, carrierRow: Extract<TraceRow, { kind: "rec" }> | undefined): string {
  const direct = directEdge(book, openId, carrierId);
  if (direct) {
    const phrase =
      direct.dir === "out"
        ? book.phraseFor(direct.field, book.typeOf(openId), "forward")
        : book.phraseFor(direct.field, book.typeOf(carrierId), "inverse");
    return traceSentence(book, openId, phrase, carrierId);
  }
  if (carrierRow && carrierRow.via === openId) {
    const phrase = carrierRow.dir === "contains" ? "contains" : carrierRow.phrase;
    return traceSentence(book, openId, phrase, carrierId);
  }
  if (carrierRow && carrierRow.via) {
    const phrase = carrierRow.dir === "contains" ? "contains" : carrierRow.phrase;
    return carrierSentence(book, carrierRow.via, phrase, carrierId);
  }
  return book.title(carrierId);
}

/** The Trace section's record groups: the trace's cross-layer rows under their band label, in layer order. A relationship label groups only the records linked directly to the open record; a record reached through other records nests under its chain of carriers down from the open record, each level with its carrier-side phrase. A carrier outside the band reads as a nesting parent. Gaps read along even when their band is the open record's. */
export function railGroups(book: Book, model: TraceModel, openId: string): RailGroup[] {
  void book.rev;
  const openBand = bandOf(book, openId);
  const rowById = new Map<string, Extract<TraceRow, { kind: "rec" }>>();
  const rowIndex = new Map<TraceRow, number>();
  model.rows.forEach((row, i) => {
    rowIndex.set(row, i);
    if (row.kind === "rec" && !row.here) rowById.set(row.id, row);
  });
  const gapsByLabel = new Map<string, Extract<TraceRow, { kind: "gap" }>[]>();
  const recsByLabel = new Map<string, Extract<TraceRow, { kind: "rec" }>[]>();
  const moresByLabel = new Map<string, Extract<TraceRow, { kind: "more" }>[]>();
  for (const row of model.rows) {
    if (row.kind === "note") continue;
    if (row.kind === "rec" && row.here) continue;
    const label = row.kind === "rec" ? bandOf(book, row.id) : row.band;
    if (label === null) continue;
    if (row.kind === "gap") {
      const list = gapsByLabel.get(label) ?? [];
      list.push(row);
      gapsByLabel.set(label, list);
      continue;
    }
    if (label === openBand) continue;
    if (row.kind === "rec") {
      const list = recsByLabel.get(label) ?? [];
      list.push(row);
      recsByLabel.set(label, list);
    } else {
      const list = moresByLabel.get(label) ?? [];
      list.push(row);
      moresByLabel.set(label, list);
    }
  }
  const labels = new Set<string>([...recsByLabel.keys(), ...moresByLabel.keys(), ...gapsByLabel.keys()]);
  const groups: RailGroup[] = [];
  for (const label of labels) {
    const recs = recsByLabel.get(label) ?? [];
    const mores = moresByLabel.get(label) ?? [];
    const nodes = new Map<string, RailNode & { index: number; direct: boolean; key: string; field: string | null; dir: TraceEdgeDir }>();
    for (const row of recs) {
      const direct = isDirect(book, openId, row);
      if (direct) {
        const side = openSide(book, openId, row.id, row);
        const key = (side.field ?? "") + "|" + side.dir + "|" + side.phrase;
        nodes.set(row.id, {
          row,
          phrase: side.phrase,
          tip: traceSentence(book, openId, side.phrase, row.id),
          carrierId: null,
          children: [],
          index: rowIndex.get(row) ?? 0,
          direct: true,
          key,
          field: side.field,
          dir: side.dir,
        });
      } else {
        const via = row.via ?? openId;
        const phrase = row.dir === "contains" ? "contains" : row.phrase;
        nodes.set(row.id, {
          row,
          phrase,
          tip: carrierSentence(book, via, phrase, row.id),
          carrierId: via,
          children: [],
          index: rowIndex.get(row) ?? 0,
          direct: false,
          key: "",
          field: row.field,
          dir: row.dir,
        });
      }
    }
    // Records in this band reached through another record in this band nest under their carrier.
    const attached = new Set<string>();
    for (const node of nodes.values()) {
      if (node.direct || !node.carrierId) continue;
      const parent = nodes.get(node.carrierId);
      if (parent) {
        parent.children.push(node);
        attached.add((node.row as Extract<TraceRow, { kind: "rec" }>).id);
      }
    }
    for (const node of nodes.values()) node.children.sort((a, b) => (rowIndex.get(a.row) ?? 0) - (rowIndex.get(b.row) ?? 0));
    const topDirect = [...nodes.values()].filter((n) => n.direct).sort((a, b) => a.index - b.index);
    const topOrphanRoots = [...nodes.values()].filter((n) => !n.direct && !attached.has((n.row as Extract<TraceRow, { kind: "rec" }>).id));
    // Direct collapse rows read in their relationship group; indirect collapse rows nest under their carrier.
    const directMores: (RailNode & { index: number; key: string; field: string | null; dir: TraceEdgeDir })[] = [];
    const moresByCarrier = new Map<string, Extract<TraceRow, { kind: "more" }>[]>();
    for (const row of mores) {
      const via = row.via ?? openId;
      if (via === openId) {
        const phrase = row.dir === "contains" ? "contains" : row.phrase;
        const key = (row.field ?? "") + "|" + row.dir + "|" + phrase;
        directMores.push({ row, phrase, tip: "", carrierId: null, children: [], index: rowIndex.get(row) ?? 0, key, field: row.field, dir: row.dir });
      } else {
        const list = moresByCarrier.get(via) ?? [];
        list.push(row);
        moresByCarrier.set(via, list);
      }
    }
    for (const [via, list] of moresByCarrier) {
      const parent = nodes.get(via);
      const leaves: RailNode[] = list
        .sort((a, b) => (rowIndex.get(a) ?? 0) - (rowIndex.get(b) ?? 0))
        .map((row) => ({ row, phrase: row.dir === "contains" ? "contains" : row.phrase, tip: "", carrierId: via, children: [] }));
      if (parent) {
        parent.children.push(...leaves);
        parent.children.sort((a, b) => (rowIndex.get(a.row) ?? 0) - (rowIndex.get(b.row) ?? 0));
      }
    }
    const relationsByKey = new Map<string, RailRelation & { order: number }>();
    for (const node of topDirect) {
      const hit = relationsByKey.get(node.key);
      if (hit) {
        hit.nodes.push(node);
      } else {
        relationsByKey.set(node.key, {
          key: node.key,
          label: relationLabel(node.phrase),
          tip: relationSentence(book, openId, node.phrase),
          field: node.field,
          dir: node.dir,
          phrase: node.phrase,
          nodes: [node],
          order: node.index,
        });
      }
    }
    for (const more of directMores) {
      const hit = relationsByKey.get(more.key);
      if (hit) {
        hit.nodes.push(more);
      } else {
        relationsByKey.set(more.key, {
          key: more.key,
          label: relationLabel(more.phrase),
          tip: relationSentence(book, openId, more.phrase),
          field: more.field,
          dir: more.dir,
          phrase: more.phrase,
          nodes: [more],
          order: more.index,
        });
      }
    }
    const relations = [...relationsByKey.values()]
      .sort((a, b) => a.order - b.order)
      .map((rel) => {
        rel.nodes.sort((a, b) => (rowIndex.get(a.row) ?? 0) - (rowIndex.get(b.row) ?? 0));
        return { key: rel.key, label: rel.label, tip: rel.tip, field: rel.field, dir: rel.dir, phrase: rel.phrase, nodes: rel.nodes };
      });
    // Indirect records whose carriers sit outside this band read nested under
    // their full chain of carriers down from the open record. Each chain level
    // reads with its connecting phrase from its own carrier's side, and chains
    // sharing a prefix share their parents, so every reached record appears once.
    const chainForLeaf = (startVia: string, excludeId: string | null = null): string[] => {
      const reversed: string[] = [];
      const seen = new Set<string>([openId]);
      if (excludeId) seen.add(excludeId);
      let cur: string | null = startVia;
      let guard = 0;
      while (cur && cur !== openId && !seen.has(cur) && guard < 12) {
        seen.add(cur);
        reversed.push(cur);
        const carrierRow = rowById.get(cur);
        if (!carrierRow || !carrierRow.via) break;
        cur = carrierRow.via;
        guard++;
      }
      reversed.reverse();
      return reversed;
    };
    const intermediateById = new Map<string, RailNode>();
    const getIntermediate = (id: string): RailNode => {
      const kept = intermediateById.get(id);
      if (kept) return kept;
      const carrierRow = rowById.get(id);
      const row: Extract<TraceRow, { kind: "rec" }> =
        carrierRow ?? { kind: "rec", id, depth: 0, phrase: "", here: false, showPh: false, field: null, dir: "contains", via: null };
      let phrase = "";
      if (carrierRow) {
        phrase = carrierRow.dir === "contains" ? "contains" : carrierRow.phrase;
      } else {
        const direct = directEdge(book, openId, id);
        if (direct) {
          phrase =
            direct.dir === "out"
              ? book.phraseFor(direct.field, book.typeOf(openId), "forward")
              : book.phraseFor(direct.field, book.typeOf(id), "inverse");
        }
      }
      const node: RailNode = { row, phrase, tip: carrierParentTip(book, openId, id, carrierRow), carrierId: carrierRow?.via ?? null, children: [] };
      intermediateById.set(id, node);
      return node;
    };
    const ensureChild = (parent: RailNode, child: RailNode): void => {
      if (!parent.children.includes(child)) parent.children.push(child);
    };
    const orphans: RailNode[] = [];
    const orphanTopIds = new Set<string>();
    const chains = new Map<string, string[]>();
    for (const node of topOrphanRoots) {
      const leafId = (node.row as Extract<TraceRow, { kind: "rec" }>).id;
      chains.set(leafId, chainForLeaf(node.carrierId ?? openId, leafId));
    }
    const orderedOrphans = [...topOrphanRoots].sort((a, b) => {
      const ai = (a.row as Extract<TraceRow, { kind: "rec" }>).id;
      const bi = (b.row as Extract<TraceRow, { kind: "rec" }>).id;
      const ac = chains.get(ai) ?? [];
      const bc = chains.get(bi) ?? [];
      return ac.length - bc.length || a.index - b.index;
    });
    for (const node of orderedOrphans) {
      const leafId = (node.row as Extract<TraceRow, { kind: "rec" }>).id;
      const chain = chains.get(leafId) ?? [];
      let bandAt = -1;
      for (let i = chain.length - 1; i >= 0; i--) {
        const id = chain[i];
        if (id && nodes.has(id)) {
          bandAt = i;
          break;
        }
      }
      if (bandAt >= 0) {
        const ancestorId = chain[bandAt];
        const ancestor = ancestorId ? nodes.get(ancestorId) : undefined;
        if (!ancestor) continue;
        let current: RailNode = ancestor;
        for (let i = bandAt + 1; i < chain.length; i++) {
          const id = chain[i];
          if (!id) continue;
          const mid = getIntermediate(id);
          ensureChild(current, mid);
          current = mid;
        }
        ensureChild(current, node);
      } else {
        let current: RailNode | null = null;
        for (const id of chain) {
          if (!id) continue;
          const mid = getIntermediate(id);
          if (current === null) {
            if (!orphanTopIds.has(id)) {
              orphanTopIds.add(id);
              orphans.push(mid);
            }
          } else {
            ensureChild(current, mid);
          }
          current = mid;
        }
        if (current) ensureChild(current, node);
        else orphans.push(node);
      }
    }
    for (const [via, list] of moresByCarrier) {
      if (nodes.has(via)) continue;
      const leaves: RailNode[] = [...list]
        .sort((a, b) => (rowIndex.get(a) ?? 0) - (rowIndex.get(b) ?? 0))
        .map((m) => ({ row: m, phrase: m.dir === "contains" ? "contains" : m.phrase, tip: "", carrierId: via, children: [] }));
      const kept = intermediateById.get(via);
      if (kept) {
        for (const leaf of leaves) ensureChild(kept, leaf);
        continue;
      }
      const chain = chainForLeaf(via);
      let bandAt = -1;
      for (let i = chain.length - 1; i >= 0; i--) {
        const id = chain[i];
        if (id && nodes.has(id)) {
          bandAt = i;
          break;
        }
      }
      if (bandAt >= 0) {
        const ancestorId = chain[bandAt];
        const ancestor = ancestorId ? nodes.get(ancestorId) : undefined;
        if (!ancestor) continue;
        let current: RailNode = ancestor;
        for (let i = bandAt + 1; i < chain.length; i++) {
          const id = chain[i];
          if (!id) continue;
          const mid = getIntermediate(id);
          ensureChild(current, mid);
          current = mid;
        }
        for (const leaf of leaves) ensureChild(current, leaf);
      } else {
        let current: RailNode | null = null;
        for (const id of chain) {
          if (!id) continue;
          const mid = getIntermediate(id);
          if (current === null) {
            if (!orphanTopIds.has(id)) {
              orphanTopIds.add(id);
              orphans.push(mid);
            }
          } else {
            ensureChild(current, mid);
          }
          current = mid;
        }
        if (current) {
          for (const leaf of leaves) ensureChild(current, leaf);
        }
      }
    }
    for (const top of orphans) top.carrierId = null;
    const minRowIndex = (node: RailNode): number => {
      let best = rowIndex.get(node.row) ?? Number.MAX_SAFE_INTEGER;
      for (const child of node.children) {
        const childBest = minRowIndex(child);
        if (childBest < best) best = childBest;
      }
      return best;
    };
    const sortTree = (node: RailNode): void => {
      for (const child of node.children) sortTree(child);
      node.children.sort((a, b) => minRowIndex(a) - minRowIndex(b));
    };
    for (const node of nodes.values()) sortTree(node);
    for (const top of orphans) sortTree(top);
    orphans.sort((a, b) => minRowIndex(a) - minRowIndex(b));
    const gaps = gapsByLabel.get(label) ?? [];
    if (relations.length || orphans.length || gaps.length) groups.push({ label, relations, orphans, gaps });
  }
  const rank = (label: string): number => {
    const at = RAIL_GROUP_ORDER.findIndex((key) => (book.layerByKey[key]?.label ?? "") === label);
    return at < 0 ? RAIL_GROUP_ORDER.length : at;
  };
  return groups.sort((a, b) => rank(a.label) - rank(b.label) || (a.label < b.label ? -1 : a.label > b.label ? 1 : 0));
}

/** Whether the Trace section shows anything for a record: cross-layer rows, implementation, or proof. */
export function traceSectionVisible(book: Book, id: string): boolean {
  void book.rev;
  const rec = book.get(id);
  if (!rec) return false;
  if (railGroups(book, buildTrace(book, id), id).length > 0) return true;
  if (rec.expect && (rec.expect.impl || (rec.impl ?? []).length > 0)) return true;
  return !!rec.expect && !!rec.expect.proof;
}

/** Whether the Completion section shows anything for a record: a stage, parked children, or completion lines. */
export function completionSectionVisible(book: Book, id: string): boolean {
  void book.rev;
  const r = book.get(id);
  if (!r) return false;
  if (stageLine(r.completion)) return true;
  if ((r.completion?.parkedBy ?? []).length > 0) return true;
  return completionLines(r.completion).some((line) => line.mark !== "none-expected");
}

/** The step suffixes a record carries for a flow, from its serves field. */
function servedSteps(book: Book, serverId: string, flowId: string): string[] {
  const raw = book.get(serverId)?.fields?.["serves"];
  const entries = typeof raw === "string" ? [raw] : Array.isArray(raw) ? raw : [];
  const out: string[] = [];
  for (const entry of entries) {
    const text = String(entry);
    const hash = text.indexOf("#");
    if (hash < 0 || stripStep(text) !== flowId) continue;
    const step = text.slice(hash + 1);
    if (step && !out.includes(step)) out.push(step);
  }
  return out;
}

/** The identifier a flow step carries, or null when the step names none. */
function stepIdOf(step: unknown): string | null {
  if (typeof step !== "object" || step === null || !("id" in step)) return null;
  const id: unknown = step.id;
  return typeof id === "string" || typeof id === "number" ? String(id) : null;
}

/** A step suffix read as a flow step number: its position in the flow's steps, else the number it carries. */
function flowStepPosition(book: Book, flowId: string, stepId: string): number | null {
  const steps = book.get(flowId)?.fields?.["steps"];
  if (Array.isArray(steps)) {
    const at = steps.findIndex((step) => stepIdOf(step) === stepId);
    if (at >= 0) return at + 1;
  }
  const match = /^s(\d+)$/i.exec(stepId) ?? /^(\d+)$/.exec(stepId);
  const digits = match?.[1];
  return digits === undefined ? null : Number(digits);
}

/** Step numbers read as words: "1 and 6", or "1, 2 and 6". */
function stepListText(positions: number[]): string {
  const words = positions.map((n) => String(n));
  if (words.length < 3) return words.join(" and ");
  const last = words[words.length - 1] ?? "";
  return words.slice(0, -1).join(", ") + " and " + last;
}

/** The step suffix for a trace row: which steps of the flow at one end the record at the other serves, or "" when neither end is a flow step. */
export function stepSuffixFor(book: Book, rowId: string, openId: string, traceIds: readonly string[]): string {
  void book.rev;
  const rowType = book.typeOf(rowId);
  const openType = book.typeOf(openId);
  if (rowType !== "flow" && openType !== "flow") return "";
  const flowId = rowType === "flow" ? rowId : openId;
  const servers = rowType === "flow" ? [openId, ...traceIds] : [rowId];
  const positions: number[] = [];
  for (const server of servers) {
    for (const stepId of servedSteps(book, server, flowId)) {
      const at = flowStepPosition(book, flowId, stepId);
      if (at !== null && !positions.includes(at)) positions.push(at);
    }
  }
  if (!positions.length) return "";
  positions.sort((a, b) => a - b);
  const [only] = positions;
  if (positions.length === 1 && only !== undefined) return "Step " + only;
  return "Steps " + stepListText(positions);
}

/** One record type counted in a trace summary line. */
export interface RailTypeCount {
  type: string;
  count: number;
}

/** A relationship's direct records counted by type, in first-seen order. The summary line reads these counts; nested records read under their own headings once their carriers open. */
export function relationTypeCounts(book: Book, rel: RailRelation): RailTypeCount[] {
  void book.rev;
  const counts: RailTypeCount[] = [];
  const byType = new Map<string, RailTypeCount>();
  for (const node of rel.nodes) {
    if (node.row.kind !== "rec") continue;
    const type = book.typeOf(node.row.id);
    const hit = byType.get(type);
    if (hit) hit.count++;
    else {
      const next = { type, count: 1 };
      byType.set(type, next);
      counts.push(next);
    }
  }
  return counts;
}

/** The orphan tops of a band counted by type, in first-seen order. */
export function orphanTypeCounts(book: Book, orphans: RailNode[]): RailTypeCount[] {
  void book.rev;
  const counts: RailTypeCount[] = [];
  const byType = new Map<string, RailTypeCount>();
  for (const node of orphans) {
    if (node.row.kind !== "rec") continue;
    const type = book.typeOf(node.row.id);
    const hit = byType.get(type);
    if (hit) hit.count++;
    else {
      const next = { type, count: 1 };
      byType.set(type, next);
      counts.push(next);
    }
  }
  return counts;
}

/** Every record a tree holds: its rec rows at every depth, plus collapsed counts. */
export function countTreeRecs(nodes: RailNode[]): number {
  let n = 0;
  const walk = (list: RailNode[]): void => {
    for (const node of list) {
      if (node.row.kind === "rec") n++;
      else if (node.row.kind === "more") n += node.row.more;
      if (node.children.length) walk(node.children);
    }
  };
  walk(nodes);
  return n;
}

/** Every record a band holds: its relationships and orphans at every depth. A band with more than 8 opens its summaries collapsed. */
export function bandRecCount(group: RailGroup): number {
  let n = countTreeRecs(group.orphans);
  for (const rel of group.relations) n += countTreeRecs(rel.nodes);
  return n;
}

/** One group of nested trace records under their shared carrier-side phrase. The field and direction read the link from the carrier's side; containment carries no field. */
export interface RailChildGroup {
  phrase: string;
  label: string;
  field: string | null;
  dir: TraceEdgeDir;
  nodes: RailNode[];
}

/** Nested trace records grouped by their carrier-side phrase, in first-seen order. The tree reads each phrase once as the heading above its group. */
export function groupChildrenByPhrase(children: RailNode[]): RailChildGroup[] {
  const groups: RailChildGroup[] = [];
  const byPhrase = new Map<string, RailChildGroup>();
  for (const child of children) {
    const phrase = child.phrase;
    let group = byPhrase.get(phrase);
    if (!group) {
      group = { phrase, label: relationLabel(phrase), field: null, dir: "contains", nodes: [] };
      byPhrase.set(phrase, group);
      groups.push(group);
    }
    group.nodes.push(child);
    if (group.field === null && (child.row.kind === "rec" || child.row.kind === "more")) {
      const field = child.row.field;
      const dir = child.row.dir;
      if (field && (dir === "out" || dir === "in")) {
        group.field = field;
        group.dir = dir;
      }
    }
  }
  return groups;
}

/** The records of one band that indirect carriers lead to, counted by type through every nesting level. */
export function orphanBandCounts(book: Book, orphans: RailNode[], band: string): RailTypeCount[] {
  void book.rev;
  const counts: RailTypeCount[] = [];
  const byType = new Map<string, RailTypeCount>();
  const seen = new Set<string>();
  const visit = (node: RailNode): void => {
    if (node.row.kind === "rec" && !seen.has(node.row.id)) {
      seen.add(node.row.id);
      if (bandOf(book, node.row.id) === band) {
        const type = book.typeOf(node.row.id);
        const hit = byType.get(type);
        if (hit) hit.count++;
        else {
          const next = { type, count: 1 };
          byType.set(type, next);
          counts.push(next);
        }
      }
    }
    for (const child of node.children) visit(child);
  };
  for (const node of orphans) visit(node);
  return counts;
}

/** The carriers of indirect records as a phrase: "2 features", "1 requirement and 3 flows". */
export function orphanCarrierPhrase(book: Book, orphans: RailNode[]): string {
  const parts = orphanTypeCounts(book, orphans).map((c) => pluralize(c.count, book.typeName(c.type).toLowerCase()));
  return parts.length <= 1 ? (parts[0] ?? "") : parts.slice(0, -1).join(", ") + " and " + parts[parts.length - 1];
}
