// The shapes the book client reads, shared by the store, the views and the tests.

/** A test step: the unit tests that prove it and its screenshots, if any. */
export interface TestStep {
  unit?: { path?: string; cases?: { name?: string; browser?: boolean }[] }[];
  screenshots?: string[];
}

/** A record's fields, read by name. */
export type RecordFields = Record<string, unknown>;

/** A scenario or rule proof state from its proves links and its proving test. */
export type ProofState =
  | "passing"
  | "failing"
  | "awaiting"
  | "unconfirmed"
  | "unproved"
  | "indirect"
  | "norun";

/** A screen capture state from the model: captured, route-not-captured or no-route. */
export type CaptureState = "captured" | "route-not-captured" | "no-route";

/** A finding check level: blocking, should-fix or advisory. */
export type CheckLevel = "G" | "S" | "A";

/** An implementation entry kind the book writes in impl[].kind. */
export type ImplKind =
  | "handler"
  | "endpoint"
  | "aggregate-root"
  | "aggregate-method"
  | "automation"
  | "reaction"
  | "job"
  | "port"
  | "adapter"
  | "event"
  | "rule-exception"
  | "result"
  | "domain-service"
  | "route";

/** An outline node kind: a record, a group, or the code, test or finding under its subject. */
export type OutlineKind = "record" | "group" | "code" | "test" | "finding";

/** A record layer the project claims. */
export type LayerKey = "intent" | "behavior" | "implementation" | "proof";

/** A navigation sort: outline order, title, or proof with failing first. */
export type SortKey = "outline" | "title" | "proof";

/** A navigation view: the outline or the security list. */
export type View = "outline" | "security";

/** The Intent layer grouping in the outline: the outline order or one group per product area. */
export type IntentGrouping = "outline" | "product-area";

/** The book load state: loading, ready, or failed with the reason. */
export type LoadState = { kind: "loading" } | { kind: "ready" } | { kind: "failed"; error: string };

/** The record types with scenarios that prove them: use cases, reactions and jobs. */
export const SCENARIO_TYPES: readonly ["use-case", "reaction", "job"] = ["use-case", "reaction", "job"];

/** Whether a record type has scenarios that prove it. */
export function isScenarioType(type: string): boolean {
  return SCENARIO_TYPES.some((scenarioType) => scenarioType === type);
}

/** The record types that live under a module member: use cases, rules, reactions, jobs, notifications, settings and read models. */
export const MODULE_MEMBER_TYPES: readonly string[] = ["use-case", "rule", "reaction", "job", "notification", "setting", "read-model"];

/** One parked child: a record a live waiver holds out of its parent's stage, with that waiver and its expiry. */
export interface ParkedChild {
  id: string;
  waiver: string;
  expiresOn?: string | null;
}

/** A record the Records packs carry, with every computed detail the book adds. */
export interface BookRecord {
  type: string;
  title: string;
  /** The folder the record file sits in, relative to the consumer root. */
  folder?: string;
  module?: string | null;
  fields: RecordFields;
  /** The record layer, or root for the product above every layer. */
  layer?: LayerKey | "root" | null;
  src?: SourceRef | null;
  impl?: { path?: string; line?: number | null; kind?: string }[];
  tests?: BookTest[];
  expect?: { impl?: boolean; proof?: boolean };
  /** What the record still lacks in the layers the project claims, always an array. */
  lacks?: string[];
  /** Computed completion from proof: code, tests and passing scenarios, each null when the record expects nothing there. */
  completion?: {
    hasCode?: boolean | null;
    hasTests?: boolean | null;
    scenarios?: { pass?: number; of?: number } | null;
    /** The model completion stage: unfulfilled, specified, coded or verified, with its scope counts. */
    stage?: string;
    proof?: string;
    heldBy?: string[];
    parkedBy?: ParkedChild[];
    inScope?: number;
    parked?: number;
    total?: number;
    counts?: Record<string, { done?: number; of?: number }>;
  } | null;
  findings?: BookFinding[];
  proof?: {
    state?: ProofState | null;
    tests?: number;
    test?: { name?: string; path?: string | null; line?: number | null } | null;
    /** The recorded run commit, empty when the run recorded none. */
    commit?: string;
    /** The short run commit the book shows after "last passed at", empty when the run recorded none. */
    lastPassedAt?: string;
    /** Whether the proof is fresh or stale against the working tree. */
    status?: string;
    /** How many commits HEAD sits ahead of the run commit when stale with changed code, else 0. */
    commitsBehind?: number;
    /** Why the proof is stale, only when stale. */
    staleReason?: string;
  };
  machine?: {
    coded?: boolean;
    verified?: boolean;
    met?: boolean;
    measurePassed?: boolean | null;
    tested?: boolean;
  };
  reachable?: string[];
  keptBy?: Record<string, number>;
  intentCoverage?: {
    reached?: boolean | number;
    total?: number;
    reachedBy?: string[];
    claims?: ReachClaim[];
    steps?: { id?: string; reached?: boolean; reachedBy?: string[]; claims?: ReachClaim[] }[];
  };
  why?: { text?: string; links?: { id?: string; kind?: string }[] }[];
  out?: BookEdgeJson[];
  in?: BookEdgeJson[];
  /** The whole explanation rendered as one page; the writer omits it and the reader never needs it. */
  bodyHtml?: string;
  sections?: BookSections | null;
  fieldsHtml?: Record<string, string>;
  requires?: string[];
  calls?: string[];
  route?: {
    path?: string;
    surfaces?: string[];
    frontend?: string;
    route?: string;
    file?: string;
    reader?: string;
  } | null;
  steps?: { text?: string; cites?: string[] }[];
  stepServers?: Record<string, string[]>;
  stepScreens?: Record<string, string[]>;
  captures?: { path: string; bookPath?: string; theme?: string; width?: number; capturedOn?: string }[];
  captureState?: CaptureState;
  evidenceStrength?: string;
  parent?: string | null;
  results?: string[];
  scenarios?: string[];
  rules?: string[];
  events?: string[];
  screens?: string[];
  shownBy?: string[];
  /** The concerns of a decision's followers, computed by the book command from the classification rules applies. */
  concerns?: string[];
  outcome?: GoalOutcome;
}

/** One outline node: a record, a group, or the code, test or finding under its subject. */
export interface OutlineNode {
  id: string;
  kind: OutlineKind;
  label: string;
  type?: string | null;
  children: OutlineNode[];
  shard?: string;
}

/** One test proving a record: its name, file, line, and whether a proves link confirms it. */
export interface BookTest {
  name?: string;
  path?: string | null;
  file?: string;
  line?: number | null;
  confirmed?: boolean;
  passing?: boolean | null;
  browser?: boolean;
}

/** One finding on a record: its code, level, message, and whether a waiver covers it. */
export interface BookFinding {
  code?: string | null;
  tier?: string | null;
  level?: string | null;
  message?: string;
  /** The message rendered by the CLI: record references as book links, inline Markdown as HTML. */
  messageHtml?: string;
  covered?: boolean;
  outcome?: unknown;
  reason?: unknown;
  path?: string | null;
  line?: number | null;
}

/** One edge as the records packs carry it: the other end and the field key. */
export interface BookEdgeJson {
  id: string;
  key: string;
}

/** One explanation section: its heading and its rendered HTML. */
export interface BookSection {
  h?: string;
  html?: string;
}

/** A record's explanation split by the CLI: the intro and the headed sections. */
export interface BookSections {
  intro?: string;
  list?: BookSection[];
}

/** One record type: its label, layer and the question its page answers. */
export interface LexiconType {
  label: string;
  plural: string;
  layer: string;
  question: string;
}

/** One record layer: its label, who writes it, and the question it answers. */
export interface LexiconLayer {
  label: string;
  question?: string;
  writes: string;
}

/** One carried prose section the client explains from. */
export interface LexiconSection {
  text: string;
  source: string;
}

/** One Links-table row: the field, its carriers and both phrases, read from the annotations on the schema. */
export interface LexiconLink {
  field: string;
  carriers: string[] | string;
  forward: string;
  inverse: string;
}

/** The book lexicon: types, layers, links, sections, glossary, field labels and finding codes. */
export interface Lexicon {
  types: Record<string, LexiconType>;
  layers: Record<string, LexiconLayer>;
  links: LexiconLink[];
  sections: Record<string, LexiconSection>;
  glossary: Record<string, { term: string; definition: string }>;
  fields: Record<string, string>;
  findings?: Record<string, { level: string; meaning: string }>;
}

/** One Intent coverage entry: a requirement, flow step, problem or constraint, whether a Behavior record reaches it, and the reaching records. */
export interface IntentCoverageEntry {
  id: string;
  kind: "requirement" | "step" | "problem" | "constraint";
  reached: boolean;
  reachedBy: string[];
  claims?: ReachClaim[];
}

/** The computed Intent coverage: counts, the share reached, and one entry per requirement, flow step, problem and constraint. */
export interface IntentCoverage {
  reached: number;
  total: number;
  share: number;
  requirements: { reached: number; total: number };
  steps: { reached: number; total: number };
  problems?: { reached: number; total: number };
  constraints?: { reached: number; total: number };
  entries: IntentCoverageEntry[];
}

/** One product area in the Intent product-area grouping: a Behavior module with the features and flows its records reach. */
export interface ProductArea {
  id: string;
  title: string;
  features: string[];
  flows: string[];
}

/** The Intent product-area grouping: one entry per Behavior module, with the features and flows no module reaches. */
export interface ProductAreas {
  modules: ProductArea[];
  unfulfilled: { features: string[]; flows: string[] };
}

/** One record layer the project claims, with the practice it follows: litespec or own. */
export interface LayerClaim {
  layer: string;
  practice: string;
}

/** The manifest: project, product, source links, release, test run, totals, layers, claims, shard map, search, unplaced and build time. */
export interface Manifest {
  project: { id: string; title: string };
  /** The product record id (`product/<name>`), or "" when the model holds no product. */
  productId: string;
  /** The source link template with {path} and {line} left to fill; null when the book links no source. */
  links: string | null;
  /** The LiteSpec version that generated the book, for example 7.0.0. */
  release: string;
  claims?: LayerClaim[];
  intentCoverage?: IntentCoverage;
  testRun?: { date?: string; passed?: number; failed?: number; total?: number } | null;
  totals: Record<string, number>;
  layers: { key: string; label: string; outline: string[] }[];
  shardOf: Record<string, string>;
  search: string[];
  unplaced: string[];
  generatedOn: string;
}

/** One security item: cross-tenant access, an outside call, or personal or sensitive data. */
export interface SecurityItem {
  kind: "cross" | "calls" | "data";
  id: string;
  text: string;
}

/** A link edge: the link field and the linked record id. */
export interface LinkEdge {
  field: string;
  id: string;
}

/** One search row: id, title, type and excerpt. */
export type SearchRow = string[];

/** A source reference: the file and line a record comes from. */
export interface SourceRef {
  path?: string | null;
  line?: number | null;
  litespec?: boolean;
}

/** One reaching record and how it stands: verified when its proof passes, claimed when only its claim stands. */
export interface ReachClaim {
  id: string;
  state: "verified" | "claimed";
}

/** One measured observation of a goal: when, what was read and where it came from. */
export interface GoalObservation {
  date: string;
  value: string;
  source: string;
}

/** A goal's outcome, apart from delivery: measured once observations exist, else not measured. */
export interface GoalOutcome {
  state: "measured" | "not measured";
  observations: GoalObservation[];
}
