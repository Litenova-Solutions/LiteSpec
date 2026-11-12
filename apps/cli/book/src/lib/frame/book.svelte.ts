// The book's data and state: shards, the lexicon, relations, completion, filters
// and navigation. Every word the book prints about a record or a link comes
// from the lexicon shard or the record itself; nothing here invents one.
//
// Records and the outline are large, so they live in plain objects and `rev`
// counts their changes: every accessor reads `rev`, so a component that calls one
// re-renders when data arrives. Small UI state is ordinary $state.

import { createSearch, type BookSearch } from "./search";
import type { ShardLoader } from "./shard-loader";
import type {
  BookRecord,
  IntentCoverage,
  LayerClaim,
  Lexicon,
  LinkEdge,
  LoadState,
  Manifest,
  OutlineNode,
  ProofState,
  RecordFields,
  SearchRow,
  SecurityItem,
  SortKey,
  View,
} from "./shard-types";
import { isFlowType } from "./shard-types";
import {
  parseLexicon,
  parseManifest,
  parseOutlineNodes,
  parseRecordPack,
  parseSearchRows,
  parseSecurityItems,
} from "./shard-schema";
import {
  lexiconExplain,
  lexiconFieldLabel,
  lexiconFindingMeaning,
  lexiconGlossaryDefinition,
  lexiconLinkVaries,
  lexiconPhraseFor,
  lexiconPluralName,
  lexiconResolveField,
  lexiconSection,
  lexiconSectionSource,
  lexiconTypeName,
  lexiconTypeQuestion,
  lexiconTypeTag,
  type Explanation,
} from "./lexicon";
import {
  breadcrumbFor,
  connectionsFor,
  edgesFor,
  insFor,
  linkSentenceFor,
  outsFor,
  performersFor,
  traceFor,
  upOneFor,
  type Connection,
} from "./relations";
import {
  completionSummaryFor,
  hasFailingProofFor,
  isComplete,
  lacksFor,
  proofStateFor,
} from "./completion";
import {
  bestSearchHitFor,
  createFacets,
  createSorts,
  hasAnyFilter,
  passesFiltersFor,
  visibleMapFor,
  type FacetDef,
} from "./filters";
import { pluralize, stripStep } from "./text";

export type { Connection, Explanation };

interface LayerInfo {
  key: string;
  label: string;
  outline: string[];
  writes: string;
  question: string;
}

interface NodeInfo {
  label: string;
  type: string;
}

export class Book {
  // ---------- data ----------
  rev = $state(0);
  load = $state<LoadState>({ kind: "loading" });
  man: Manifest | null = null;
  lex: Lexicon = { types: {}, layers: {}, links: [], fields: {}, glossary: {}, sections: {}, findings: {} };
  security: SecurityItem[] | null = null;
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
  search: BookSearch | null = null;
  private outlineLoaded: Record<string, boolean> = {};
  private shardLoaded: Record<string, boolean> = {};
  private searchPromise: Promise<void> | null = null;
  private securityPromise: Promise<void> | null = null;

  // ---------- state ----------
  view = $state<View>("outline");
  sortKey = $state<SortKey>("outline");
  query = $state("");
  selected = $state<string | null>(null);
  expanded = $state<Record<string, boolean>>({});
  closedGroups = $state<Record<string, boolean>>({});
  openLayers = $state<Record<string, boolean>>({ intent: true });
  active = $state<Record<string, string[]>>({});
  recent = $state<string[]>([]);

  hits = $derived.by(() => {
    this.rev;
    if (!this.query || !this.search) return null;
    return this.search.query(this.query);
  });

  readonly facets: FacetDef[] = createFacets({
    get: (id) => this.get(id),
    typeOf: (id) => this.typeOf(id),
    typeName: (type) => this.typeName(type),
    proofState: (id) => this.proofState(id),
    layerOf: this.layerOf,
    layerLabel: (key) => this.layerByKey[key]?.label || key,
  });

  readonly sorts: Record<SortKey, { label: string; cmp: (a: string, b: string) => number }> = createSorts({
    title: (id) => this.title(id),
    indexOf: (id) => this.idx(id),
    proofState: (id) => this.proofState(id),
  });

  constructor(private readonly shards: ShardLoader) {}

  private touch(): void {
    this.rev++;
  }

  // ---------- loading ----------
  async boot(hash: string): Promise<void> {
    try {
      this.man = parseManifest(await this.shards.load("manifest"));
      const day = this.man.generatedOn.slice(0, 10) || new Date().toISOString().slice(0, 10);
      const today = new Date(day + "T00:00:00Z");
      this.today = isNaN(+today) ? new Date() : today;
      this.lex = parseLexicon(await this.shards.load("lexicon"));
      for (const layer of this.man.layers) {
        const meta = this.lex.layers[layer.key] ?? { label: "", writes: "" };
        this.layerByKey[layer.key] = {
          key: layer.key,
          label: layer.label || meta.label || layer.key,
          outline: layer.outline,
          writes: meta.writes,
          question: meta.question ?? "",
        };
      }
      const keys = ["intent"];
      const start = stripStep(hash);
      const startLayer = this.layerFor(start);
      if (startLayer && !keys.includes(startLayer) && this.layerByKey[startLayer]) keys.push(startLayer);
      await this.loadLayers(keys);
      if (this.man.unplaced.length) console.info("Unplaced records: " + this.man.unplaced.join(", "));
      const total = Object.values(this.man.totals).reduce((a, b) => a + b, 0);
      if (total && total <= 500) await this.preloadAll();
      const first = this.node[start] || this.rec[start] ? start : (this.productId() ?? this.order[0]);
      this.load = { kind: "ready" };
      this.touch();
      if (first) {
        history.replaceState({ id: first }, "", "#" + encodeURIComponent(first));
        await this.select(first, { fromHistory: true });
      }
      // The security items count the Security tab; they load after the first page.
      setTimeout(() => void this.ensureSecurity(), 0);
    } catch (err) {
      this.load = { kind: "failed", error: err instanceof Error ? err.message : "missing data" };
    }
  }

  private walkOutline(layer: string, n: OutlineNode, parent: string | null, depth: number): string[] {
    if (!n.id) return [];
    if (n.kind === "group") return n.children.flatMap((c) => this.walkOutline(layer, c, parent, depth));
    const id = n.id;
    this.layerOf[id] = layer;
    this.parentOf[id] = parent;
    this.depthOf[id] = depth;
    this.order.push(id);
    const fallbackType = id.split("/")[0] ?? id;
    this.node[id] = { label: n.label || id, type: n.type || fallbackType };
    this.kidsOf[id] = n.children.flatMap((c) => this.walkOutline(layer, c, id, depth + 1));
    return [id];
  }

  private addLayerOutline(layer: string, arrays: OutlineNode[][]): void {
    const roots = arrays.flatMap((arr) => arr.flatMap((n) => this.walkOutline(layer, n, null, 1)));
    this.rootsOf[layer] = (this.rootsOf[layer] ?? []).concat(roots);
    this.orderIdx = {};
    this.order.forEach((id, i) => (this.orderIdx[id] = i));
    this.kidsOf["layer/" + layer] = this.rootsOf[layer] ?? [];
    const meta = this.lex.layers[layer] ?? { label: "", writes: "" };
    const info = this.layerByKey[layer];
    this.rec["layer/" + layer] = {
      type: "layer",
      title: info?.label || meta.label || layer,
      fields: { question: meta.question ?? "", writes: meta.writes },
      bodyHtml: "",
      sections: null,
      src: null,
    };
  }

  async ensureLayer(key: string): Promise<void> {
    if (this.outlineLoaded[key]) return;
    const layer = this.layerByKey[key];
    if (!layer) return;
    if (!layer.outline.length) {
      this.outlineLoaded[key] = true;
      this.rootsOf[key] ??= [];
      this.kidsOf["layer/" + key] = this.rootsOf[key] ?? [];
      return;
    }
    try {
      const values = await Promise.all(
        layer.outline.map(async (shard) => parseOutlineNodes(await this.shards.load(shard))),
      );
      this.addLayerOutline(key, values);
    } finally {
      this.outlineLoaded[key] = true;
      this.touch();
    }
  }

  loadLayers(keys: string[]): Promise<void[]> {
    return Promise.all(keys.map((key) => this.ensureLayer(key)));
  }

  allOutlinesLoaded(): boolean {
    return (this.man?.layers ?? []).every((layer) => this.outlineLoaded[layer.key]);
  }

  isLayerLoaded(key: string): boolean {
    this.rev;
    return !!this.outlineLoaded[key];
  }

  ensureAllOutlines(): Promise<void[]> {
    return this.loadLayers((this.man?.layers ?? []).map((layer) => layer.key));
  }

  async ensureRecords(ids: string[]): Promise<void> {
    const keys = new Set<string>();
    for (const raw of ids) {
      const id = stripStep(raw);
      const shard = this.man?.shardOf[id];
      if (!this.rec[id] && shard && !this.shardLoaded[shard]) keys.add(shard);
    }
    if (!keys.size) return;
    const list = [...keys];
    try {
      const values = await Promise.all(
        list.map(async (shard) => parseRecordPack(await this.shards.load(shard))),
      );
      for (const pack of values) Object.assign(this.rec, pack);
    } finally {
      for (const key of list) this.shardLoaded[key] = true;
      this.touch();
    }
  }

  ensureSearch(): Promise<void> {
    this.searchPromise ??= (async () => {
      const keys = this.man?.search ?? [];
      const values = await Promise.all(
        keys.map(async (shard) => parseSearchRows(await this.shards.load(shard))),
      ).catch((): SearchRow[][] => []);
      this.search = await createSearch(values.flat());
      this.touch();
    })();
    return this.searchPromise;
  }

  ensureSecurity(): Promise<void> {
    this.securityPromise ??= (async () => {
      try {
        this.security = parseSecurityItems(await this.shards.load("security"));
      } catch {
        this.security = [];
      }
      this.touch();
    })();
    return this.securityPromise;
  }

  async preloadAll(): Promise<void> {
    await this.ensureAllOutlines();
    await this.ensureRecords(Object.keys(this.man?.shardOf ?? {}));
  }

  // ---------- lexicon ----------
  section(title: string): string {
    return lexiconSection(this.lex, title);
  }

  sectionSource(title: string, fallback: string): string {
    return lexiconSectionSource(this.lex, title, fallback);
  }

  glossaryDefinition(term: string): string {
    return lexiconGlossaryDefinition(this.lex, term);
  }

  findingMeaning(code: string): string {
    return lexiconFindingMeaning(this.lex, code);
  }

  typeName(type: string): string {
    return lexiconTypeName(this.lex, type);
  }

  pluralName(type: string): string {
    return lexiconPluralName(this.lex, type);
  }

  typeTag(type: string): string {
    return lexiconTypeTag(this.lex, type);
  }

  typeQuestion(type: string): string {
    return lexiconTypeQuestion(this.lex, type);
  }

  /** A field reads with its LiteSpec label; an unknown field renders raw, never invented. */
  fieldLabel(field: string): string {
    return lexiconFieldLabel(this.lex, field);
  }

  resolveField(field: string): string | null {
    return lexiconResolveField(this.lex, field);
  }

  phraseFor(field: string, carrierType: string | null, dir: "forward" | "inverse"): string {
    return lexiconPhraseFor(this.lex, field, carrierType, dir);
  }

  linkVaries(field: string): boolean {
    return lexiconLinkVaries(this.lex, field);
  }

  /** The LiteSpec text that explains a key: a proof state, a check level, a glossary term or a record type. */
  explain(key: string): Explanation | null {
    return lexiconExplain(this.lex, key);
  }

  // ---------- records ----------
  get(id: string | null | undefined): BookRecord | undefined {
    this.rev;
    return id ? this.rec[stripStep(id)] : undefined;
  }

  has(id: string): boolean {
    this.rev;
    const recordId = stripStep(id);
    return !!(this.rec[recordId] || this.node[recordId]);
  }

  title(id: string): string {
    this.rev;
    const recordId = stripStep(id);
    return this.rec[recordId]?.title ?? this.node[recordId]?.label ?? recordId;
  }

  typeOf(id: string): string {
    this.rev;
    const recordId = stripStep(id);
    return this.rec[recordId]?.type ?? this.node[recordId]?.type ?? recordId.split("/")[0] ?? recordId;
  }

  /** The folder and module a record file sits in, as the model computes them. */
  placement(id: string): { folder: string; module: string | null } {
    this.rev;
    const recordId = stripStep(id);
    return { folder: this.rec[recordId]?.folder ?? "", module: this.rec[recordId]?.module ?? null };
  }

  fields(id: string): RecordFields {
    return this.get(id)?.fields ?? {};
  }

  kids(id: string): string[] {
    this.rev;
    return this.kidsOf[stripStep(id)] ?? [];
  }

  parent(id: string): string | null {
    this.rev;
    return this.parentOf[stripStep(id)] ?? null;
  }

  siblingsOf(id: string): string[] {
    const parent = this.parent(id);
    if (parent) return this.kids(parent);
    const layer = this.layerOf[id];
    return layer ? (this.rootsOf[layer] ?? []) : [];
  }

  productId(): string | null {
    this.rev;
    return this.man?.project.id || this.order.find((id) => this.typeOf(id) === "product") || null;
  }

  /** The record layer a type belongs to, from LiteSpec's model page. */
  kindOf(id: string): "intent" | "behavior" | "governance" | "unlayered" {
    const layerText = this.lex.types[this.typeOf(id)]?.layer ?? "";
    const layer = (layerText.split(",")[0] ?? "").trim();
    if (!layer) return "unlayered";
    if (layer.startsWith("intent")) return "intent";
    if (layer.startsWith("behavior")) return "behavior";
    if (layer.startsWith("governance")) return "governance";
    return "unlayered";
  }

  isBeh(id: string): boolean {
    return this.kindOf(id) === "behavior";
  }

  /** A LiteSpec record has a type in the model page's record type table; everything else is code, a test, a finding or a grouping. */
  isReal(id: string): boolean {
    return !!this.lex.types[this.typeOf(id)];
  }

  layerFor(id: string): string | null {
    if (!id) return null;
    const known = this.layerOf[id];
    if (known) return known;
    if (id.startsWith("layer/")) return id.slice(6);
    const typeKey = id.split("/")[0] ?? "";
    const entry = this.lex.types[typeKey];
    if (!entry?.layer) return null;
    return (String(entry.layer).split(",")[0] ?? "").trim();
  }

  // ---------- relations ----------
  outEdges(id: string): LinkEdge[] {
    return edgesFor(this.get(id), "out");
  }

  inEdges(id: string): LinkEdge[] {
    return edgesFor(this.get(id), "in");
  }

  outs(id: string, field: string): string[] {
    return outsFor(this, id, field);
  }

  ins(id: string, field: string): string[] {
    return insFor(this, id, field);
  }

  performers(id: string): { id: string; via: string }[] {
    return performersFor(this, id);
  }

  upOne(id: string): string | null {
    return upOneFor(this, id);
  }

  traceOf(id: string): string[] {
    return traceFor(this, id);
  }

  /** Every link of a record by its field; a record pointing here reads "Points at this: field". */
  connectionsOf(id: string): Connection[] {
    return connectionsFor(this, id);
  }

  /** Each link field read as a sentence, in the Links table phrases. */
  linkSentence(id: string, label: string, items: Connection[]): string {
    return linkSentenceFor(this, id, label, items);
  }

  /** A record's breadcrumb: the ids from the root down to its parent, excluding itself. */
  breadcrumbOf(id: string): string[] {
    return breadcrumbFor(this, id);
  }

  /** Records related to the selected one: its links and its trace, marked in the outline. */
  related = $derived.by(() => {
    this.rev;
    const selected = this.selected;
    const out = new Set<string>();
    if (!selected || !this.rec[stripStep(selected)]) return out;
    for (const connection of this.connectionsOf(selected)) if (this.has(connection.id)) out.add(connection.id);
    for (const id of this.traceOf(selected)) if (id !== selected) out.add(id);
    return out;
  });

  // ---------- children by type ----------
  kidsByType(id: string, type: string): string[] {
    return this.kids(id).filter((kid) => this.typeOf(kid) === type);
  }

  descByType(id: string, type: string, maxDepth: number): string[] {
    const out: string[] = [];
    const seen = new Set<string>();
    const walk = (current: string, depth: number): void => {
      if (depth > maxDepth) return;
      for (const kid of this.kids(current)) {
        if (seen.has(kid)) continue;
        seen.add(kid);
        if (this.typeOf(kid) === type) out.push(kid);
        walk(kid, depth + 1);
      }
    };
    walk(stripStep(id), 1);
    return out;
  }

  private listField(id: string, key: "results" | "scenarios" | "rules" | "events" | "screens" | "shownBy"): string[] | null {
    const value = this.get(id)?.[key];
    return Array.isArray(value) ? value : null;
  }

  resultsOf(id: string): string[] {
    return this.listField(id, "results") ?? this.kidsByType(id, "result");
  }

  scenariosOf(id: string): string[] {
    const own = this.listField(id, "scenarios");
    if (own) return own;
    const out = this.descByType(id, "scenario", 2);
    return out.length ? out : this.kidsByType(id, "scenario");
  }

  rulesOf(id: string): string[] {
    return this.listField(id, "rules") ?? this.kidsByType(id, "rule");
  }

  eventsOf(id: string): string[] {
    return this.listField(id, "events") ?? this.kidsByType(id, "event");
  }

  screensOf(id: string): string[] {
    return this.listField(id, "screens") ?? this.ins(stripStep(id), "offers");
  }

  shownByOf(id: string): string[] {
    return this.listField(id, "shownBy") ?? this.kidsByType(id, "scenario");
  }

  // ---------- layer claims and Intent coverage ----------
  /** The record layers the project claims, as the project file writes them. */
  claims(): LayerClaim[] {
    return this.man?.claims ?? [];
  }

  /** Whether the project claims a record layer; a layer it does not claim reads as not claimed. */
  claimed(layer: string): boolean {
    return this.claims().some((claim) => claim.layer === layer);
  }

  intentCoverage(): IntentCoverage | null {
    return this.man?.intentCoverage ?? null;
  }

  // ---------- completion and proof ----------
  proofState(id: string): ProofState | null {
    return proofStateFor(this, id);
  }

  /** What a record is still missing in the layers it claims: implementation, or tests on itself or its scenarios. */
  lacks(id: string): string {
    return lacksFor(this, id);
  }

  /** Whether a record is complete: it has the code and tests it expects, and its scenarios pass. A record expecting nothing is complete. */
  completeOf(id: string): boolean {
    return isComplete(this.get(id)?.completion);
  }

  completeCounts(ids: string[]): string {
    if (!ids.length) return "";
    const { done, total } = this.completionSummary(ids);
    return done + " of " + total + " complete";
  }

  /** How many of the records are complete, with the percent complete. */
  completionSummary(ids: string[]): { done: number; total: number; percent: number } {
    return completionSummaryFor((id) => this.completeOf(id), ids);
  }

  /** Whether a record's proof state is failing. */
  hasFailingProof(id: string): boolean {
    return hasFailingProofFor(this, id);
  }

  // ---------- dates ----------
  days(date: string): number | null {
    const parsed = new Date(date + "T00:00:00Z");
    return isNaN(+parsed) ? null : Math.round((+parsed - +this.today) / 86400000);
  }

  fmtDate(value: string | null | undefined): string {
    if (!value) return "";
    const parsed = new Date(value + "T00:00:00Z");
    return isNaN(+parsed)
      ? value
      : parsed.toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "UTC" });
  }

  dateHint(value: string): string {
    const count = this.days(value);
    if (count === null) return "";
    if (count === 0) return "today";
    return count > 0 ? "in " + pluralize(count, "day") : pluralize(-count, "day") + " ago";
  }

  // ---------- security ----------
  securityItems(): SecurityItem[] {
    this.rev;
    return this.security ?? [];
  }

  // ---------- facets and search ----------
  anyFilter(): boolean {
    return hasAnyFilter(this.query, this.active);
  }

  toggleFacet(key: string, value: string): void {
    const list = this.active[key] ?? [];
    this.active[key] = list.includes(value) ? list.filter((item) => item !== value) : [...list, value];
  }

  clearFilters(): void {
    this.active = {};
    this.query = "";
  }

  setQuery(query: string): void {
    this.query = query.trim();
  }

  passesFilters(id: string, skip?: string): boolean {
    return passesFiltersFor(this.facets, this.active, this.query, this.hits, (entry) => this.title(entry), id, skip);
  }

  /** 2 for a record that is a direct match, 1 for an ancestor kept to show where a match sits. */
  visible = $derived.by(() => {
    this.rev;
    return visibleMapFor(this.order, this.parentOf, (id) => this.passesFilters(id), this.anyFilter());
  });

  isVisible(id: string): boolean {
    return !this.anyFilter() || this.visible.has(id);
  }

  isDirectMatch(id: string): boolean {
    return !this.anyFilter() || this.visible.get(id) === 2;
  }

  /** The record Enter opens from the search box: the best title hit, else the best hit. */
  bestSearchHit(): string | null {
    return bestSearchHitFor(this.order, (id) => this.passesFilters(id), this.hits, this.query, (id) => this.title(id));
  }

  // ---------- sorting ----------
  private idx(id: string): number {
    return this.orderIdx[id] ?? 0;
  }

  // ---------- navigation ----------
  expandTo(id: string): void {
    const next: Record<string, boolean> = {};
    if (this.kids(id).length) next[id] = true;
    let parent = this.parentOf[id] ?? null;
    const layer = this.layerOf[id] ?? "";
    delete this.closedGroups[(parent ?? layer) + "|" + this.typeOf(id)];
    while (parent) {
      next[parent] = true;
      const grandparent = this.parentOf[parent] ?? null;
      const parentLayer = this.layerOf[parent] ?? "";
      delete this.closedGroups[(grandparent ?? parentLayer) + "|" + this.typeOf(parent)];
      parent = this.parentOf[parent] ?? null;
    }
    this.expanded = next;
    if (layer) this.openLayers[layer] = true;
  }

  expandAll(): void {
    const next: Record<string, boolean> = {};
    for (const id of this.order) if (this.kids(id).length && this.typeOf(id) !== "use-case") next[id] = true;
    this.expanded = next;
  }

  collapseAll(): void {
    this.expanded = {};
  }

  private async preloadUpChain(id: string): Promise<void> {
    let current = stripStep(id);
    await this.ensureRecords([current]);
    for (let step = 0; step < 8; step++) {
      const up = this.upOne(current);
      if (!up || up === current) return;
      current = stripStep(up);
      await this.ensureRecords([current]);
    }
  }

  /** Loads what a record's page reads: its layer, the chain above it, its children and its links. */
  async preloadFor(raw: string): Promise<void> {
    const id = stripStep(raw);
    const layer = this.layerFor(id);
    if (layer && this.layerByKey[layer] && !this.outlineLoaded[layer]) await this.loadLayers([layer]);
    await this.preloadUpChain(id);
    const record = this.rec[id];
    if (!record) return;
    const type = record.type;
    const ids: string[] = [];
    const depth = type === "use-case" ? 2 : isFlowType(type) || type === "aggregate" || type === "result" ? 1 : 0;
    if (depth) {
      const seen = new Set<string>();
      const walk = (current: string, level: number): void => {
        if (level > depth) return;
        for (const kid of this.kidsOf[current] ?? []) {
          if (seen.has(kid)) continue;
          seen.add(kid);
          ids.push(kid);
          walk(kid, level + 1);
        }
      };
      walk(id, 1);
    }
    for (const edge of [...this.outEdges(id), ...this.inEdges(id)].slice(0, 200)) ids.push(edge.id);
    for (const map of [record.stepServers, record.stepScreens]) {
      if (!map) continue;
      for (const list of Object.values(map)) ids.push(...list);
    }
    await this.ensureRecords(ids.slice(0, 500));
  }

  async select(raw: string, opts: { fromHistory?: boolean } = {}): Promise<void> {
    const id = stripStep(raw);
    await this.preloadFor(id);
    this.selected = id;
    this.expandTo(id);
    if (this.view !== "outline" && this.view !== "security") this.view = "outline";
    this.recent = [id, ...this.recent.filter((entry) => entry !== id)].slice(0, 12);
    if (!opts.fromHistory) history.pushState({ id }, "", "#" + encodeURIComponent(id));
  }

  async setView(view: View): Promise<void> {
    this.view = view;
    if (view === "security") await this.ensureSecurity();
  }

  /** The navigation's records in reading order, for j and k. */
  navOrder(): string[] {
    return this.order.filter((id) => !this.anyFilter() || this.passesFilters(id));
  }
}
