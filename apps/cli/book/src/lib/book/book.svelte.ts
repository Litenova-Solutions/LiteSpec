// The book's data and state: shards, the lexicon, relations, review state, filters
// and navigation. Every word the book prints about a record, a link or a status
// comes from the lexicon shard or the record itself; nothing here invents one.
//
// Records and the outline are large, so they live in plain objects and `rev`
// counts their changes: every accessor reads `rev`, so a component that calls one
// re-renders when data arrives. Small UI state is ordinary $state.

import { createSearch, type BookSearch } from "./search";
import type { ShardLoader } from "$lib/data/shards";
import type {
  Ballot, Batch, BookRecord, Edge, Fields, Lexicon, Manifest, OutlineNode, Queue, SearchRow, SecurityItem,
} from "./types";

export type View = "outline" | "list" | "queue" | "security" | "ballots";
export type Human = "approved" | "reviewed" | "mine" | "mineR" | "draft" | "inherited" | "question" | "changes" | "retired";
export type Verdict = "mine" | "mineR" | "changes" | "question";
export type NoteKind = Verdict | "answer";
export type ProofState = "passing" | "failing" | "awaiting" | "unconfirmed" | "unproved" | "stale" | "indirect" | "norun";
export interface Explanation { title: string; text: string; source: string }
export interface Connection { label: string; id: string; via: string }

export const HLABEL: Record<Human, string> = {
  approved: "approved", reviewed: "reviewed", mine: "approved", mineR: "reviewed", draft: "draft",
  inherited: "effective status: draft", question: "question", changes: "draft", retired: "retired",
};
export const PLABEL: Record<ProofState, string> = {
  passing: "passing", failing: "failing", awaiting: "awaiting", unconfirmed: "unconfirmed",
  unproved: "unproved", stale: "stale", indirect: "indirect", norun: "norun",
};
export const STATUS_KEYS: Human[] = ["draft", "inherited", "approved", "reviewed", "retired", "mine", "changes", "question"];
export const GROUPS = {
  none: "No grouping", field: "Record field", type: "Type", layer: "Record layer",
  status: "Review status", decider: "Decider", risk: "Risk class",
} as const;
export type GroupKey = keyof typeof GROUPS;

export const base = (s: unknown) => String(s ?? "").split("#")[0];
export const cap = (s: unknown) => { const t = String(s ?? ""); return t.charAt(0).toUpperCase() + t.slice(1); };
export const plural = (n: number, one: string, many?: string) => n + " " + (n === 1 ? one : many ?? one + "s");
export const uniq = <T>(a: T[]) => a.filter((x, i) => a.indexOf(x) === i);
export const n0 = (x: unknown) => Number(x ?? 0).toLocaleString("en-GB");

interface LayerInfo { key: string; label: string; outline: string[]; writes: string; reviews: string; question: string }
interface NodeInfo { label: string; type: string; status: string | null }

export class Book {
  // ---------- data ----------
  rev = $state(0);
  ready = $state(false);
  error = $state<string | null>(null);
  man: Manifest | null = null;
  lex: Lexicon = { types: {}, layers: {}, links: [], fields: {}, glossary: {}, sections: {}, findings: {} };
  queue: Queue | null = null;
  rec: Record<string, BookRecord> = {};
  node: Record<string, NodeInfo> = {};
  layerOf: Record<string, string> = {};
  parentOf: Record<string, string | null> = {};
  depthOf: Record<string, number> = {};
  kidsOf: Record<string, string[]> = {};
  rootsOf: Record<string, string[]> = {};
  order: string[] = [];
  orderIdx: Record<string, number> = {};
  layerByKey: Record<string, LayerInfo> = {};
  today = new Date();
  vetoDays = 7;
  search: BookSearch | null = null;
  private outlineLoaded: Record<string, boolean> = {};
  private shardLoaded: Record<string, boolean> = {};
  private searchPromise: Promise<void> | null = null;
  private queuePromise: Promise<void> | null = null;

  // ---------- state ----------
  view = $state<View>("outline");
  sortKey = $state("outline");
  groupKey = $state<GroupKey>("field");
  query = $state("");
  selected = $state<string | null>(null);
  loading = $state(false);
  expanded = $state<Record<string, boolean>>({});
  closedGroups = $state<Record<string, boolean>>({});
  openLayers = $state<Record<string, boolean>>({ intent: true });
  active = $state<Record<string, string[]>>({});
  verdict = $state<Record<string, Verdict>>({});
  notes = $state<Record<string, string>>({});
  answered = $state<Record<string, string>>({});
  recent = $state<string[]>([]);
  focusQ = $state<number | null>(null);
  noteFor = $state<string | null>(null);
  noteKind = $state<NoteKind | null>(null);
  facetsOpen = $state(false);
  listShowAll = $state(false);
  wide = $state(false);
  /** Per-key "show all" switches for long lists on the page and in the rail. */
  more = $state<Record<string, boolean>>({});
  /** The navigation's rows in display order: record ids, or "id|questionIndex" in the ballots view. */
  navIds = $state.raw<string[]>([]);
  /** Bumped to ask the navigation to scroll the selected row into view and focus it. */
  focusNavTick = $state(0);
  /** A record the rail points at; the navigation highlights its row. */
  hover = $state<string | null>(null);
  private hits: Map<string, number> | null = null;

  constructor(private readonly shards: ShardLoader) {}

  private touch() { this.rev++; }

  // ---------- loading ----------
  async boot(hash: string) {
    try {
      this.man = await this.shards.load<Manifest>("manifest");
      const day = (this.man.generatedOn ?? "").slice(0, 10) || new Date().toISOString().slice(0, 10);
      const today = new Date(day + "T00:00:00Z");
      this.today = isNaN(+today) ? new Date() : today;
      this.lex = await this.shards.load<Lexicon>("lexicon");
      for (const l of this.man.layers ?? []) {
        const meta = this.lex.layers?.[l.key] ?? {};
        this.layerByKey[l.key] = {
          key: l.key, label: l.label || meta.label || l.key, outline: l.outline ?? [],
          writes: meta.writes ?? "", reviews: meta.reviews ?? "", question: meta.question ?? "",
        };
      }
      const keys = ["intent"];
      const start = base(hash);
      const startLayer = this.layerFor(start);
      if (startLayer && !keys.includes(startLayer) && this.layerByKey[startLayer]) keys.push(startLayer);
      await this.loadLayers(keys);
      if (this.man.unplaced?.length) console.info("Unplaced records: " + this.man.unplaced.join(", "));
      const total = Object.values(this.man.totals ?? {}).reduce((a, b) => a + b, 0);
      if (total && total <= 500) await this.preloadAll();
      const first = this.node[start] || this.rec[start] ? start : this.productId() ?? this.order[0];
      this.ready = true;
      this.touch();
      if (first) {
        history.replaceState({ id: first }, "", "#" + encodeURIComponent(first));
        await this.select(first, { fromHistory: true });
      }
      // The queue counts the Security and Ballots tabs; it loads after the first page.
      setTimeout(() => void this.ensureQueue(), 0);
    } catch (err) {
      this.error = err instanceof Error ? err.message : "missing data";
    }
  }

  private walkOutline(layer: string, n: OutlineNode, parent: string | null, depth: number): string[] {
    if (!n?.id) return [];
    if (n.kind === "group") return (n.children ?? []).flatMap((c) => this.walkOutline(layer, c, parent, depth));
    const id = n.id;
    this.layerOf[id] = layer;
    this.parentOf[id] = parent;
    this.depthOf[id] = depth;
    this.order.push(id);
    this.node[id] = { label: n.label || id, type: n.type || id.split("/")[0], status: n.status ?? null };
    this.kidsOf[id] = (n.children ?? []).flatMap((c) => this.walkOutline(layer, c, id, depth + 1));
    return [id];
  }

  private addLayerOutline(layer: string, arrays: OutlineNode[][]) {
    const roots = (arrays ?? []).flatMap((arr) => (arr ?? []).flatMap((n) => this.walkOutline(layer, n, null, 1)));
    this.rootsOf[layer] = (this.rootsOf[layer] ?? []).concat(roots);
    this.orderIdx = {};
    this.order.forEach((id, i) => (this.orderIdx[id] = i));
    this.kidsOf["layer/" + layer] = this.rootsOf[layer];
    const meta = this.lex.layers?.[layer] ?? {};
    const info = this.layerByKey[layer];
    this.rec["layer/" + layer] = {
      type: "layer", title: info?.label || meta.label || layer,
      fields: { question: meta.question ?? "", writes: meta.writes ?? "", reviews: meta.reviews ?? "" },
      bodyHtml: "", sections: null, src: null,
    };
  }

  async ensureLayer(key: string) {
    if (this.outlineLoaded[key]) return;
    const layer = this.layerByKey[key];
    if (!layer) return;
    if (!layer.outline.length) {
      this.outlineLoaded[key] = true;
      this.rootsOf[key] ??= [];
      this.kidsOf["layer/" + key] = this.rootsOf[key];
      return;
    }
    try {
      const values = await Promise.all(layer.outline.map((k) => this.shards.load<OutlineNode[]>(k)));
      this.addLayerOutline(key, values);
    } finally {
      this.outlineLoaded[key] = true;
      this.touch();
    }
  }

  loadLayers(keys: string[]) { return Promise.all(keys.map((k) => this.ensureLayer(k))); }
  allOutlinesLoaded() { return (this.man?.layers ?? []).every((l) => this.outlineLoaded[l.key]); }
  isLayerLoaded(key: string) { this.rev; return !!this.outlineLoaded[key]; }
  ensureAllOutlines() { return this.loadLayers((this.man?.layers ?? []).map((l) => l.key)); }

  async ensureRecords(ids: string[]) {
    const keys = new Set<string>();
    for (const raw of ids ?? []) {
      const id = base(raw);
      const shard = this.man?.shardOf?.[id];
      if (!this.rec[id] && shard && !this.shardLoaded[shard]) keys.add(shard);
    }
    if (!keys.size) return;
    const list = [...keys];
    try {
      const values = await Promise.all(list.map((k) => this.shards.load<Record<string, BookRecord>>(k)));
      for (const pack of values) if (pack) Object.assign(this.rec, pack);
    } finally {
      for (const k of list) this.shardLoaded[k] = true;
      this.touch();
    }
  }

  ensureSearch() {
    this.searchPromise ??= (async () => {
      const keys = this.man?.search ?? [];
      const values = await Promise.all(keys.map((k) => this.shards.load<SearchRow[]>(k))).catch(() => [] as SearchRow[][]);
      this.search = await createSearch(values.flat());
      this.touch();
    })();
    return this.searchPromise;
  }

  ensureQueue() {
    this.queuePromise ??= (async () => {
      try {
        this.queue = (await this.shards.load<Queue>("queue")) ?? { batches: {}, ballots: [], security: [] };
      } catch {
        this.queue = { batches: {}, ballots: [], security: [] };
      }
      if (this.queue.review?.vetoWindowDays) this.vetoDays = this.queue.review.vetoWindowDays;
      this.touch();
    })();
    return this.queuePromise;
  }

  async preloadAll() {
    await this.ensureAllOutlines();
    await this.ensureRecords(Object.keys(this.man?.shardOf ?? {}));
  }

  // ---------- lexicon ----------
  section(title: string) { return this.lex.sections?.[title]?.text ?? ""; }
  sectionSource(title: string, fallback: string) { return this.lex.sections?.[title]?.source || fallback; }
  gloss(term: string) { return this.lex.glossary?.[String(term).toLowerCase()]?.definition ?? ""; }
  findingMeaning(code: string) { return this.lex.findings?.[code]?.meaning ?? ""; }
  typeName(t: string) { return this.lex.types?.[t]?.label || t; }
  pluralName(t: string) { const q = this.lex.types?.[t]; return q?.plural || q?.label || t; }
  tag(t: string) { return this.typeName(t).toLowerCase(); }
  typeQuestion(t: string) { return this.lex.types?.[t]?.question ?? ""; }
  /** A field reads with its LiteSpec label; an unknown field renders raw, never invented. */
  fieldLabel(s: string) { return this.lex.fields?.[s] || s; }

  private carriersOf(c: string[] | string) {
    if (Array.isArray(c)) return c;
    return typeof c === "string" ? c.split(",").map((s) => s.trim()).filter(Boolean) : [];
  }

  resolveField(field: string): string | null {
    let candidate = field;
    const dot = candidate.indexOf(".");
    if (dot > 0 && this.lex.types?.[candidate.slice(0, dot)]) candidate = candidate.slice(dot + 1);
    const links = this.lex.links ?? [];
    for (;;) {
      if (links.some((l) => l.field === candidate)) return candidate;
      const nested = candidate.lastIndexOf(".");
      if (nested < 0) break;
      candidate = candidate.slice(0, nested);
    }
    // A leaf link field reads from its own row: journey steps name their actor.
    const leaf = field.slice(field.lastIndexOf(".") + 1);
    return leaf !== candidate && links.some((l) => l.field === leaf) ? leaf : null;
  }

  private linkRow(field: string, carrierType: string | null) {
    const candidate = this.resolveField(field);
    if (candidate === null) return null;
    const rows = (this.lex.links ?? []).filter((l) => l.field === candidate);
    if (!rows.length) return null;
    if (carrierType) {
      for (const row of rows) {
        const carriers = this.carriersOf(row.carriers);
        if (carriers.includes(carrierType)) return { ...row, carriers };
      }
    }
    return { ...rows[0], carriers: this.carriersOf(rows[0].carriers) };
  }

  phraseFor(field: string, carrierType: string | null, dir: "forward" | "inverse") {
    const row = this.linkRow(field, carrierType);
    const text = row?.[dir];
    if (!row || !text) return field;
    if (text.includes(" / ")) {
      const parts = text.split(" / ");
      const idx = carrierType ? row.carriers.indexOf(carrierType) : -1;
      return (idx >= 0 && parts[idx] ? parts[idx] : parts[0]).trim();
    }
    return text;
  }

  linkVaries(field: string) {
    const candidate = this.resolveField(field);
    if (candidate === null) return false;
    const rows = (this.lex.links ?? []).filter((l) => l.field === candidate);
    if (rows.some((l) => (l.forward ?? "").includes(" / ") || (l.inverse ?? "").includes(" / "))) return true;
    return uniq(rows.map((l) => l.inverse ?? "")).length > 1;
  }

  /** The LiteSpec text that explains a key: a status, a glossary term ("g:term") or a record type ("type:feature"). */
  explain(key: string): Explanation | null {
    const statuses = (): Explanation => ({ title: "Statuses", text: this.section("Statuses"), source: this.sectionSource("Statuses", "model.md, Statuses") });
    const status = (title: string): Explanation => ({ ...statuses(), title });
    const glossary = (title: string, term: string): Explanation => ({ title, text: this.gloss(term), source: "Glossary" });
    const risk = this.section("Risk Classes and the Approval Table") || this.section("Risk Classes and Approval") || this.section("Risk and Approval") || this.section("Risk Classes");
    const levels = (): Explanation => ({ title: "Check level", text: this.section("Check Levels"), source: this.sectionSource("Check Levels", "model.md, Check Levels") });
    const table: Record<string, () => Explanation> = {
      draft: () => status("Draft"), approved: () => status("Approved"), reviewed: () => status("Reviewed"), retired: () => status("Retired"),
      inherited: () => glossary("Effective status", "effective status"),
      risk: () => ({ title: "Risk", text: risk, source: this.sectionSource("Risk Classes and Approval", "model.md, Risk Classes and Approval") }),
      transcribed: () => glossary("Transcription", "transcription"),
      awaiting: statuses, unconfirmed: statuses, unproved: statuses, passing: statuses, failing: statuses, indirect: statuses, norun: statuses, settled: statuses,
      stale: () => glossary("Stale", "stale"), implemented: () => glossary("Implemented", "implemented"), reachable: () => glossary("Reachable", "reachable"),
      G: levels, S: levels, A: levels,
      known: () => glossary("Exception", "exception"),
      batch: () => ({ title: "Batch review", text: this.section("Approval as Data") || this.section("Questions and Ballots"), source: "release.md, Batch Review" }),
      finding: () => glossary("Finding", "finding"), ballot: () => glossary("Ballot", "ballot"),
    };
    let e: Explanation | null = table[key]?.() ?? null;
    if (!e && key.startsWith("type:")) {
      const t = key.slice(5);
      const q = this.lex.types?.[t];
      const g = this.gloss(this.typeName(t));
      if (q) {
        const lr = this.lex.layers?.[String(q.layer ?? "").split(",")[0].trim()] ?? {};
        e = {
          title: this.typeName(t),
          text: (q.question ?? "") + (g ? " " + g : "") + (lr.writes ? " Written by " + lr.writes + "; reviewed by " + lr.reviews + "." : ""),
          source: "model.md, Record Types",
        };
      } else if (g) e = { title: this.typeName(t), text: g, source: "Glossary" };
    }
    if (!e && key.startsWith("g:")) e = glossary(cap(key.slice(2)), key.slice(2));
    return e && e.text ? { ...e, text: cap(e.text) } : null;
  }

  // ---------- records ----------
  get(id: string | null | undefined): BookRecord | undefined { this.rev; return id ? this.rec[base(id)] : undefined; }
  has(id: string) { this.rev; const b = base(id); return !!(this.rec[b] || this.node[b]); }
  title(id: string) { this.rev; const b = base(id); return this.rec[b]?.title ?? this.node[b]?.label ?? b; }
  typeOf(id: string) { this.rev; const b = base(id); return this.rec[b]?.type ?? this.node[b]?.type ?? b.split("/")[0]; }
  fields(id: string): Fields { return this.get(id)?.fields ?? {}; }
  kids(id: string) { this.rev; return this.kidsOf[base(id)] ?? []; }
  parent(id: string) { this.rev; return this.parentOf[base(id)] ?? null; }
  siblingsOf(id: string) { const p = this.parent(id); return p ? this.kids(p) : this.rootsOf[this.layerOf[id]] ?? []; }
  productId() { this.rev; return this.man?.project?.id || this.order.find((x) => this.typeOf(x) === "product") || null; }

  /** The record layer a type belongs to, from LiteSpec's model page. */
  kindOf(id: string): "intent" | "behavior" | "governance" | "entry" {
    const layer = String(this.lex.types?.[this.typeOf(id)]?.layer ?? "").split(",")[0].trim();
    if (!layer) return "entry";
    return layer.startsWith("intent") ? "intent" : layer.startsWith("behavior") ? "behavior" : layer.startsWith("governance") ? "governance" : "entry";
  }
  isBeh(id: string) { return this.kindOf(id) === "behavior"; }
  /** A LiteSpec record has a type in the model page's record type table; everything else is code, a test, a finding or a grouping. */
  isReal(id: string) { return !!this.lex.types?.[this.typeOf(id)]; }

  layerFor(id: string): string | null {
    if (!id) return null;
    if (this.layerOf[id]) return this.layerOf[id];
    if (id.startsWith("layer/")) return id.slice(6);
    const q = this.lex.types?.[id.split("/")[0]];
    return q?.layer ? String(q.layer).split(",")[0].trim() : null;
  }

  // ---------- relations ----------
  private edges(r: BookRecord | undefined, key: "out" | "in", target: string): Edge[] {
    const raw = r?.[key];
    if (!raw) return [];
    if (Array.isArray(raw)) {
      return raw
        .map((e) => ({ field: String(e.key || e.field || ""), id: base(e[target] || e.id || e.to || e.from || "") }))
        .filter((e) => e.field && e.id);
    }
    return Object.entries(raw).flatMap(([field, ids]) => (ids ?? []).map((t) => ({ field, id: base(t) })));
  }
  outEdges(id: string) { return this.edges(this.get(id), "out", "to"); }
  inEdges(id: string) { return this.edges(this.get(id), "in", "from"); }
  private static fieldMatches(edgeField: string, field: string) { return edgeField === field || edgeField.endsWith("." + field); }
  outs(id: string, field: string) { return this.outEdges(id).filter((e) => Book.fieldMatches(e.field, field)).map((e) => e.id); }
  ins(id: string, field: string) { return this.inEdges(id).filter((e) => Book.fieldMatches(e.field, field)).map((e) => e.id); }

  performers(id: string) {
    const grant = String(this.fields(id).access?.grant ?? "");
    if (!grant.startsWith("permission/")) return [];
    return this.ins(grant, "allows").flatMap((role) =>
      this.ins(role, "holds").map((a) => ({ id: a, via: "via " + this.title(grant) + ", role " + this.title(role) })));
  }

  upOne(id: string): string | null {
    const t = this.typeOf(id);
    const known = (xs: string[]) => xs.map(base).find((x) => this.has(x)) ?? null;
    if (t === "scenario" || t === "result") { const p = this.parent(id); return p && this.get(p) && this.isReal(p) ? p : null; }
    if (t === "screen") return known(this.outs(id, "serves")) ?? known(this.outs(id, "offers"));
    if (["use-case", "rule", "reaction", "job", "notification", "setting"].includes(t)) return known(this.outs(id, "fulfills")) ?? known(this.outs(id, "serves"));
    if (t === "journey") return known(this.outs(id, "cites")) ?? known(this.outs(id, "serves"));
    if (t === "decision") return known(this.outs(id, "decides"));
    if (t === "requirement") return known(this.outs(id, "cites"));
    if (t === "feature") return known(this.outs(id, "advances")) ?? known(this.outs(id, "solves"));
    if (t === "event") return this.ins(id, "raises")[0] ?? null;
    return null;
  }

  traceOf(id: string) {
    const chain = [id];
    let cur = id;
    for (let g = 0; g < 8; g++) {
      const u = this.upOne(cur);
      if (!u || chain.includes(u)) break;
      chain.unshift(u);
      cur = u;
    }
    return chain.length > 1 ? chain : [];
  }

  /** Every link of a record by its field; a record pointing here reads "Points at this: field". */
  connectionsOf(id: string): Connection[] {
    const rows: Connection[] = [];
    const seen = new Set<string>();
    const parent = this.parent(id);
    const push = (label: string, tid: string, via = "") => {
      if (tid === id || seen.has(label + "|" + tid)) return;
      seen.add(label + "|" + tid);
      rows.push({ label, id: tid, via });
    };
    for (const e of this.outEdges(id)) if (e.id !== parent) push(this.resolveField(e.field) || e.field, e.id);
    for (const e of this.inEdges(id)) if (e.id !== parent) push("Points at this: " + (this.resolveField(e.field) || e.field), e.id);
    return rows;
  }

  /** Each link field read as a sentence, in the Links table phrases. */
  linkSentence(id: string, label: string, items: Connection[]) {
    const inbound = label.startsWith("Points at this: ");
    const field = inbound ? label.slice(16) : label;
    const me = this.typeName(this.typeOf(id)).toLowerCase();
    if (!inbound) return "This " + me + " " + this.phraseFor(field, this.typeOf(id), "forward");
    return "This " + me + " " + this.phraseFor(field, items.length ? this.typeOf(items[0].id) : null, "inverse");
  }

  /** Records related to the selected one: its links and its trace, marked in the outline. */
  related = $derived.by(() => {
    this.rev;
    const sel = this.selected;
    const out = new Set<string>();
    if (!sel || !this.rec[base(sel)]) return out;
    for (const c of this.connectionsOf(sel)) if (this.has(c.id)) out.add(c.id);
    for (const x of this.traceOf(sel)) if (x !== sel) out.add(x);
    return out;
  });

  // ---------- children by type ----------
  kidsByType(id: string, t: string) { return this.kids(id).filter((x) => this.typeOf(x) === t); }
  descByType(id: string, t: string, maxDepth: number) {
    const out: string[] = [];
    const seen = new Set<string>();
    const walk = (x: string, d: number) => {
      if (d > maxDepth) return;
      for (const k of this.kids(x)) {
        if (seen.has(k)) continue;
        seen.add(k);
        if (this.typeOf(k) === t) out.push(k);
        walk(k, d + 1);
      }
    };
    walk(base(id), 1);
    return out;
  }
  private listField(id: string, key: string) { const v = this.get(id)?.[key]; return Array.isArray(v) ? (v as string[]) : null; }
  resultsOf(id: string) { return this.listField(id, "results") ?? this.kidsByType(id, "result"); }
  scenariosOf(id: string) {
    const own = this.listField(id, "scenarios");
    if (own) return own;
    const out = this.descByType(id, "scenario", 2);
    return out.length ? out : this.kidsByType(id, "scenario");
  }
  rulesOf(id: string) { return this.listField(id, "rules") ?? this.kidsByType(id, "rule"); }
  eventsOf(id: string) { return this.listField(id, "events") ?? this.kidsByType(id, "event"); }
  screensOf(id: string) { return this.listField(id, "screens") ?? this.ins(base(id), "offers"); }
  shownByOf(id: string) { return this.listField(id, "shownBy") ?? this.kidsByType(id, "scenario"); }

  // ---------- review state ----------
  layerWord(id: string) { return this.isBeh(id) ? "reviewed" : "approved"; }
  human(id: string): Human {
    const v = this.verdict[id];
    if (v) return v;
    const r = this.get(id);
    if (r) {
      if (r.status === "retired") return "retired";
      if (r.status === "draft") return "draft";
      if (r.effective === "draft") return "inherited";
      return r.status === "reviewed" ? "reviewed" : "approved";
    }
    const n = this.node[base(id)];
    if (n?.status) return n.status === "draft" ? "draft" : n.status === "retired" ? "retired" : n.status === "reviewed" ? "reviewed" : "approved";
    return "approved";
  }
  needs(id: string) {
    if (!this.isReal(id)) return false;
    const h = this.human(id);
    return !(h === "approved" || h === "reviewed" || h === "mine" || h === "mineR" || h === "retired");
  }
  riskClassesOf(id: string): string[] {
    const r = this.get(id);
    if (!r) return [];
    const risk = r.fields?.risk ?? r.risk ?? [];
    return Array.isArray(risk) ? risk.filter(Boolean).map(String) : [];
  }
  riskWords(id: string) { return this.riskClassesOf(id).map((x) => cap(x.replace(/-/g, " "))); }
  hasRisk(id: string) { return this.riskClassesOf(id).length > 0; }
  approverFor(riskClass: string | undefined, id: string) {
    const own = this.get(id)?.approval;
    if (own?.by) return own.by;
    const table = this.man?.approvals;
    const read = (v: string | { by?: string; approver?: string; rule?: string }) => (typeof v === "string" ? v : v.by || v.approver || v.rule || "");
    if (table && riskClass && table[riskClass]) return read(table[riskClass]);
    if (table?.default) return read(table.default);
    return "";
  }
  openQs(id: string): Fields[] {
    const r = this.get(id);
    if (!r) return [];
    return ((r.fields?.questions as Fields[]) ?? []).filter((_, i) => !this.answered[base(id) + "#" + i]);
  }
  proofState(id: string): ProofState | null {
    const r = this.get(id);
    if (!r) return null;
    if (r.proof) return r.proof.state as ProofState;
    if (r.type === "use-case" || r.type === "reaction" || r.type === "job") {
      const states = this.scenariosOf(base(id)).map((s) => this.get(s)?.proof?.state).filter(Boolean);
      if (states.includes("failing")) return "failing";
      if (r.machine?.verified) return "passing";
      if (r.machine && !r.machine.implemented) return "unproved";
      return states.length ? "awaiting" : "unproved";
    }
    if (r.type === "requirement" && r.machine) return r.machine.met ? "passing" : "awaiting";
    return null;
  }
  /** What a record is still missing: implementation, or tests on itself or its scenarios. */
  lacks(id: string) {
    const r = this.get(id);
    if (!r) return "";
    if (typeof r.lacks === "string") return r.lacks;
    if (!r.expect) return "";
    const miss: string[] = [];
    if (r.expect.impl && !(r.impl ?? []).length) miss.push("not implemented");
    if (r.expect.proof) {
      if (r.type === "use-case" || r.type === "reaction" || r.type === "job") {
        const sc = this.scenariosOf(id).filter((x) => this.get(x));
        if (!sc.length || sc.some((x) => !(this.get(x)?.tests ?? []).length)) miss.push("scenarios not tested");
      } else if (!(r.tests ?? []).length) miss.push("not tested");
    }
    return miss.join(", ");
  }
  completionStageOf(id: string) { return String(this.get(id)?.completion?.stage ?? ""); }
  completionCounts(ids: string[]) {
    const done = ids.filter((x) => ["implemented", "proven"].includes(this.completionStageOf(x))).length;
    return ids.length && ids.some((x) => this.completionStageOf(x)) ? done + " of " + ids.length + " implemented" : "";
  }

  // ---------- dates ----------
  days(d: string) { const x = new Date(d + "T00:00:00Z"); return isNaN(+x) ? null : Math.round((+x - +this.today) / 86400000); }
  addDays(d: string, n: number) { const x = new Date(d + "T00:00:00Z"); x.setUTCDate(x.getUTCDate() + n); return x.toISOString().slice(0, 10); }
  fmtDate(s: string | null | undefined) {
    if (!s) return "";
    const d = new Date(s + "T00:00:00Z");
    return isNaN(+d) ? s : d.toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "UTC" });
  }
  dateHint(s: string) {
    const n = this.days(s);
    if (n === null) return "";
    if (n === 0) return "today";
    return n > 0 ? "in " + plural(n, "day") : plural(-n, "day") + " ago";
  }
  dueOf(id: string) {
    const r = this.get(id);
    if (!r) return null;
    const ds: string[] = [];
    if (r.status === "draft" && r.decideBy) ds.push(r.decideBy);
    for (const q of this.openQs(id)) if (q.decideBy) ds.push(q.decideBy);
    return ds.sort()[0] ?? null;
  }

  // ---------- queue ----------
  batchOf(id: string): { key: string; batch: Batch } | null {
    this.rev;
    const batches = this.queue?.batches ?? {};
    const r = this.get(id);
    if (r?.batch && batches[r.batch]) return { key: r.batch, batch: batches[r.batch] };
    for (const [key, b] of Object.entries(batches)) if ((b.records ?? b.ids ?? []).includes(base(id))) return { key, batch: b };
    return null;
  }
  ballots(): Ballot[] { this.rev; return (this.queue?.ballots ?? []).filter((x) => x.id && x.q?.question); }
  security(): SecurityItem[] { this.rev; return this.queue?.security ?? []; }
  securityReviewer() { this.rev; return this.queue?.review?.securityReviewer || "named in the project file"; }

  // ---------- facets and search ----------
  readonly facets: { key: string; label: string; get: (id: string) => string[]; name: (v: string) => string }[] = [
    { key: "layer", label: "Record layer", get: (id) => (this.layerOf[id] ? [this.layerOf[id]] : []), name: (v) => this.layerByKey[v]?.label || v },
    { key: "type", label: "Type", get: (id) => [this.typeOf(id)], name: (v) => this.typeName(v) },
    { key: "status", label: "Status", get: (id) => { const h = this.human(id); return [h === "mineR" ? "mine" : h]; }, name: (v) => HLABEL[v as Human] },
    {
      key: "risk", label: "Risk", get: (id) => { const r = this.riskClassesOf(id); return r.length ? r.map((x) => "risk:" + x) : ["risk:none"]; },
      name: (v) => (v === "risk:none" ? "No risk" : cap(v.slice(5).replace(/-/g, " "))),
    },
    { key: "questions", label: "Questions", get: (id) => (this.openQs(id).length ? ["open"] : []), name: () => "Question" },
    {
      key: "findings", label: "Findings", get: (id) => { const f = this.get(id)?.findings ?? []; return f.length ? (f.some((x) => !x.known) ? ["finding"] : ["known"]) : []; },
      name: (v) => (v === "finding" ? "Finding" : "Excepted"),
    },
    { key: "proof", label: "Proof", get: (id) => { const p = this.proofState(id); return p ? [p] : []; }, name: (v) => PLABEL[v as ProofState] },
    {
      key: "due", label: "Decide by", get: (id) => { const d = this.dueOf(id); if (!d) return []; const n = this.days(d) ?? 0; return [n < 0 ? "over" : n <= 7 ? "week" : n <= 31 ? "month" : "later"]; },
      name: (v) => ({ over: "Overdue", week: "Within 7 days", month: "Within a month", later: "Later" })[v] ?? v,
    },
    { key: "decider", label: "Decider", get: (id) => { const d = this.get(id)?.decider; return d ? [d] : []; }, name: (v) => v },
    { key: "provenance", label: "Provenance", get: (id) => (this.fields(id).provenance === "transcribed" ? ["transcribed"] : []), name: () => "Transcription" },
  ];

  anyFilter() { return !!this.query || Object.values(this.active).some((v) => v?.length); }

  toggleFacet(key: string, value: string) {
    const list = this.active[key] ?? [];
    this.active[key] = list.includes(value) ? list.filter((x) => x !== value) : [...list, value];
  }
  clearFilters() { this.active = {}; this.query = ""; }

  /** The search hits for the current query, best first, from the MiniSearch index. */
  private queryHits() {
    if (!this.query || !this.search) return null;
    this.hits ??= this.search.query(this.query);
    return this.hits;
  }
  setQuery(q: string) { this.query = q.trim(); this.hits = null; }

  passes(id: string, skip?: string) {
    for (const f of this.facets) {
      const sel = this.active[f.key];
      if (!sel?.length || f.key === skip) continue;
      const vals = f.get(id);
      if (vals.length && !sel.some((v) => vals.includes(v))) return false;
    }
    const hits = this.queryHits();
    if (this.query && !(hits?.has(base(id)) ?? this.title(id).toLowerCase().includes(this.query.toLowerCase()))) return false;
    return true;
  }

  /** 2 for a record that matches, 1 for an ancestor kept to show where a match sits. */
  visible = $derived.by(() => {
    this.rev;
    const map = new Map<string, 1 | 2>();
    if (!this.anyFilter()) return map;
    for (const id of this.order) {
      if (!this.passes(id)) continue;
      map.set(id, 2);
      let p = this.parentOf[id];
      while (p && !map.has(p)) { map.set(p, 1); p = this.parentOf[p]; }
    }
    return map;
  });
  isVisible(id: string) { return !this.anyFilter() || this.visible.has(id); }
  matches(id: string) { return !this.anyFilter() || this.visible.get(id) === 2; }

  /** The record Enter opens from the search box: the best title hit, else the best hit. */
  bestHit() {
    const hits = this.queryHits();
    const all = this.order.filter((id) => this.passes(id));
    if (hits) all.sort((a, b) => (hits.get(base(b)) ?? 0) - (hits.get(base(a)) ?? 0));
    const words = this.query.toLowerCase().split(/\s+/);
    return all.find((id) => words.every((w) => this.title(id).toLowerCase().includes(w))) ?? all[0] ?? null;
  }

  // ---------- sorting and grouping ----------
  readonly sorts: Record<string, { label: string; cmp: (a: string, b: string) => number }> = {
    outline: { label: "Outline order", cmp: (a, b) => this.idx(a) - this.idx(b) },
    title: { label: "Title", cmp: (a, b) => this.title(a).localeCompare(this.title(b)) },
    due: { label: "Decide by, soonest", cmp: (a, b) => { const x = this.dueOf(a) || "9999", y = this.dueOf(b) || "9999"; return x < y ? -1 : x > y ? 1 : this.idx(a) - this.idx(b); } },
    risk: { label: "Risk first", cmp: (a, b) => +this.hasRisk(b) - +this.hasRisk(a) || this.idx(a) - this.idx(b) },
    questions: { label: "Most open questions", cmp: (a, b) => this.openQs(b).length - this.openQs(a).length || this.idx(a) - this.idx(b) },
    proof: {
      label: "Proof, failing first",
      cmp: (a, b) => {
        const o: Record<string, number> = { failing: 0, unconfirmed: 1, unproved: 2, awaiting: 3, passing: 4, indirect: 4 };
        return (o[this.proofState(a) ?? ""] ?? 5) - (o[this.proofState(b) ?? ""] ?? 5) || this.idx(a) - this.idx(b);
      },
    },
    status: { label: "Review status", cmp: (a, b) => STATUS_KEYS.indexOf(this.human(a)) - STATUS_KEYS.indexOf(this.human(b)) || this.idx(a) - this.idx(b) },
  };
  private idx(id: string) { return this.orderIdx[id] ?? 0; }

  private homeOf(id: string) { return (base(id).split("/")[1] ?? "").split(".")[0] ?? ""; }
  private parentTitle(id: string) { const p = this.parent(id); return p ? this.title(p) : ""; }
  private goalTitleForFeature(id: string) { return this.outs(base(id), "advances").map((x) => this.title(x)).filter(Boolean)[0] ?? ""; }
  private goalTitleForProblem(id: string) {
    for (const f of this.ins(base(id), "solves")) { const g = this.goalTitleForFeature(f); if (g) return g; }
    return "";
  }
  /** The record field a list groups by, per record type: a topic, an aggregate, an actor. */
  fieldGroup(id: string): [string, string] | null {
    const f = this.fields(id), t = this.typeOf(id);
    if (t === "requirement") { const topic = this.parentTitle(id); return topic ? [topic, ""] : null; }
    if (["use-case", "rule", "reaction", "job", "notification", "setting"].includes(t)) {
      const agg = this.parentTitle(id), mod = this.homeOf(id);
      return [agg || mod, agg && mod ? mod : ""];
    }
    if (t === "screen") return [f.entryPoint ? this.title(base(f.entryPoint)) : "No entry point", f.floorplan ? cap(String(f.floorplan).replace(/-/g, " ")) : ""];
    if (t === "journey") return [f.actor ? this.title(base(f.actor)) : "No actor", this.outs(base(id), "serves").map((x) => this.title(x))[0] ?? ""];
    if (t === "problem") { const a = f.affects ?? []; return [a.length ? this.title(base(a[0])) : "No affected actor", this.goalTitleForProblem(id)]; }
    if (t === "feature") return [f.lifecycle ? cap(f.lifecycle) : "No lifecycle", this.completionStageOf(id) ? cap(this.completionStageOf(id)) : ""];
    if (t === "actor") return [f.kind ? cap(f.kind) : "No kind", ""];
    return null;
  }
  groupVal(id: string): [string, string] {
    const t = this.typeOf(id);
    switch (this.groupKey) {
      case "field": { const g = this.fieldGroup(id); return g?.[0] ? [g.join(" / "), g.filter(Boolean).join(": ")] : [t, this.pluralName(t)]; }
      case "type": return [t, this.pluralName(t)];
      case "layer": return [this.layerOf[id], this.layerByKey[this.layerOf[id]]?.label || this.layerOf[id]];
      case "status": return [this.human(id), HLABEL[this.human(id)]];
      case "decider": { const d = this.get(id)?.decider; return [d || "~", d || "No decider"]; }
      case "risk": { const r = this.riskWords(id); return r.length ? [r.join(","), r.join(", ")] : ["none", "No risk"]; }
      default: return ["", ""];
    }
  }

  // ---------- navigation ----------
  expandTo(id: string) {
    const next: Record<string, boolean> = {};
    if (this.kids(id).length) next[id] = true;
    let p = this.parentOf[id];
    delete this.closedGroups[(p || this.layerOf[id]) + "|" + this.typeOf(id)];
    while (p) {
      next[p] = true;
      delete this.closedGroups[(this.parentOf[p] || this.layerOf[p]) + "|" + this.typeOf(p)];
      p = this.parentOf[p];
    }
    this.expanded = next;
    if (this.layerOf[id]) this.openLayers[this.layerOf[id]] = true;
  }
  expandAll() {
    const next: Record<string, boolean> = {};
    for (const x of this.order) if (this.kids(x).length && this.typeOf(x) !== "use-case") next[x] = true;
    this.expanded = next;
  }
  collapseAll() { this.expanded = {}; }

  private async preloadUpChain(id: string) {
    let cur = base(id);
    await this.ensureRecords([cur]);
    for (let g = 0; g < 8; g++) {
      const u = this.upOne(cur);
      if (!u || u === cur) return;
      cur = base(u);
      await this.ensureRecords([cur]);
    }
  }

  /** Loads what a record's page reads: its layer, the chain above it, its children and its links. */
  async preloadFor(raw: string) {
    const id = base(raw);
    const layer = this.layerFor(id);
    if (layer && this.layerByKey[layer] && !this.outlineLoaded[layer]) await this.loadLayers([layer]);
    await this.preloadUpChain(id);
    const r = this.rec[id];
    if (!r) return;
    const t = r.type, ids: string[] = [];
    const depth = t === "use-case" ? 2 : ["reaction", "job", "aggregate", "result"].includes(t) ? 1 : 0;
    if (depth) {
      const seen = new Set<string>();
      const walk = (x: string, d: number) => {
        if (d > depth) return;
        for (const k of this.kidsOf[x] ?? []) { if (seen.has(k)) continue; seen.add(k); ids.push(k); walk(k, d + 1); }
      };
      walk(id, 1);
    }
    for (const e of [...this.outEdges(id), ...this.inEdges(id)].slice(0, 200)) ids.push(e.id);
    for (const key of ["stepServers", "stepScreens"]) {
      const map = (r[key] ?? {}) as Record<string, string[]>;
      for (const list of Object.values(map)) ids.push(...(list ?? []));
    }
    await this.ensureRecords(ids.slice(0, 500));
  }

  async select(raw: string, opts: { q?: number; fromHistory?: boolean } = {}) {
    const id = base(raw);
    this.loading = true;
    await this.preloadFor(id);
    this.selected = id;
    this.noteFor = null;
    this.focusQ = opts.q ?? null;
    this.expandTo(id);
    if (!["outline", "queue", "security", "ballots", "list"].includes(this.view)) this.view = "outline";
    this.recent = [id, ...this.recent.filter((x) => x !== id)].slice(0, 12);
    if (!opts.fromHistory) history.pushState({ id }, "", "#" + encodeURIComponent(id));
    this.loading = false;
  }

  async setView(v: View) {
    this.view = v;
    if (v === "queue" || v === "security" || v === "ballots") await this.ensureQueue();
  }

  /** The navigation's records in reading order, for j, k and Next draft. */
  navOrder(): string[] {
    if (this.view === "list") return this.order.filter((x) => this.passes(x)).sort(this.sorts[this.sortKey].cmp);
    return this.order.filter((x) => !this.anyFilter() || this.passes(x));
  }

  nextNeeding(from: string | null) {
    const list = this.navOrder();
    const i = from ? list.indexOf(from) : -1;
    for (let k = 1; k <= list.length; k++) {
      const id = list[(i + k) % list.length];
      if (this.needs(id) && this.typeOf(id) !== "scenario") return id;
    }
    return null;
  }

  setVerdict(v: Verdict) {
    const id = this.selected;
    if (!id || !this.get(id)) return;
    this.verdict[id] = v;
    if (v === "mine" || v === "mineR") {
      this.noteFor = null;
      const n = this.nextNeeding(id);
      if (n) void this.select(n);
      return;
    }
    this.noteFor = id;
    this.noteKind = v;
  }

  answer(i: number, text: string | "accept") {
    if (!this.selected) return;
    this.answered[this.selected + "#" + i] = text;
  }

  startAnswer(i: number) {
    this.noteFor = this.selected;
    this.noteKind = "answer";
    this.focusQ = i;
  }

  saveNote(text: string) {
    const id = this.selected;
    if (!id) return;
    if (this.noteKind === "answer" && this.focusQ !== null) this.answered[id + "#" + this.focusQ] = text || "answered";
    else this.notes[id] = text;
    this.noteFor = null;
  }

  cancelNote() {
    if (this.selected && this.noteKind !== "answer") delete this.verdict[this.selected];
    this.noteFor = null;
  }

  /** The verdicts and answers given in this book, as the review file `litespec model review` reads. */
  reviewFile() {
    const verdicts = Object.entries(this.verdict).map(([record, v]) => ({
      record,
      verdict: v === "mine" ? "approve" : v === "mineR" ? "review" : v === "changes" ? "request-changes" : "question",
      note: this.notes[record] ?? null,
    }));
    const answers = Object.entries(this.answered).map(([k, a]) => {
      const [record, i] = k.split("#");
      const q = ((this.rec[record]?.fields?.questions as Fields[]) ?? [])[+i] ?? {};
      return { record, question: q.question ?? "", ballot: q.ballot ?? null, answer: a === "accept" ? "accept recommendation" : a };
    });
    return { project: this.man?.project, generatedOn: this.man?.generatedOn, verdicts, answers };
  }
}
