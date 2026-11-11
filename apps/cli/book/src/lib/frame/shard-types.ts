// The shapes the book client reads, shared by the store, the views and the tests.

/** A test step: the unit tests that prove it and its screenshots, if any. */
export interface TestStep {
  unit?: { path?: string; cases?: { name?: string; browser?: boolean }[] }[];
  screenshots?: string[];
}

/** A record the Records packs carry, with every computed detail the book adds. */
export interface BookRecord {
  type: string;
  title: string;
  /** The folder the record file sits in, relative to the consumer root. */
  folder?: string;
  module?: string | null;
  fields: Record<string, unknown>;
  layer?: string | null;
  src?: SourceRef | null;
  impl?: { path?: string; line?: number | null; kind?: string }[];
  tests?: BookTest[];
  expect?: { impl?: boolean; proof?: boolean };
  lacks?: string[];
  /** Computed completion from proof: code, tests and passing scenarios, each null when the record expects nothing there. */
  completion?: {
    hasCode?: boolean | null;
    hasTests?: boolean | null;
    scenarios?: { pass?: number; of?: number } | null;
  } | null;
  findings?: BookFinding[];
  proof?: {
    state?: ProofState | null;
    tests?: number;
    test?: { name?: string; path?: string | null; line?: number | null } | null;
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
    steps?: { id?: string; reached?: boolean; reachedBy?: string[] }[];
  };
  why?: { text?: string; links?: { id?: string; kind?: string }[] }[];
  out?: BookEdgeJson[];
  in?: BookEdgeJson[];
  bodyHtml?: string;
  sections?: BookSections | null;
  fieldsHtml?: Record<string, string>;
  requires?: string[];
  route?: { frontend?: string; path?: string; surfaces?: string[] } | null;
  steps?: { text?: string; serves?: string[] }[];
  captures?: { path?: string; bookPath?: string }[];
  captureState?: string;
  evidenceStrength?: string;
  parent?: string | null;
  [k: string]: unknown;
}

/** One outline node: a record, a group, or the code, test or finding under its subject. */
export interface OutlineNode {
  id: string;
  kind: "record" | "group" | "code" | "test" | "finding";
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

/** One finding on a record: its code, level, message, and whether a departure covers it. */
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
  id?: string;
  key?: string;
  field?: string;
  to?: string;
  from?: string;
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

/** One Links-table row: the field, its carriers and both phrases. */
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
  findings: Record<string, { level: string; meaning: string }>;
}

/** One Intent coverage entry: a requirement or journey step, whether a Behavior record reaches it, and the reaching records. */
export interface IntentCoverageEntry {
  id: string;
  kind: "requirement" | "step";
  reached: boolean;
  reachedBy: string[];
}

/** The computed Intent coverage: counts, the share reached, and one entry per requirement and journey step. */
export interface IntentCoverage {
  reached: number;
  total: number;
  share: number;
  requirements: { reached: number; total: number };
  steps: { reached: number; total: number };
  entries: IntentCoverageEntry[];
}

/** One record layer the project claims, with the practice it follows: litespec or own. */
export interface LayerClaim {
  layer: string;
  practice: string;
}

/** The manifest: project, roots, editor, test run, totals, layers, claims, shard map, search, unplaced and generation date. */
export interface Manifest {
  project: { id: string; title: string };
  roots: { consumer: string; litespec: string };
  editor: string;
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

/** A scenario or rule proof state from its proves links and its proving test. */
export type ProofState =
  | "passing"
  | "failing"
  | "awaiting"
  | "unconfirmed"
  | "unproved"
  | "indirect"
  | "norun";

/** A link edge: the link field and the linked record id. */
export interface LinkEdge {
  field: string;
  id: string;
}

/** A record's fields, read by name. */
export type RecordFields = Record<string, any>;

/** One search row: id, title, type and excerpt. */
export type SearchRow = string[];

/** A source reference: the file and line a record comes from. */
export interface SourceRef {
  path?: string | null;
  line?: number | null;
  litespec?: boolean;
}

/** One component heading: its label and the popover it opens, if any. */
export interface TableHead {
  label: string;
  explain?: string;
}
