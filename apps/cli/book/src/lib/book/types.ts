// The shapes of the shards the book command writes (apps/cli/src/LiteSpec.Cli/Book).
// Records stay loosely typed: their fields follow the record schemas, which the
// client reads by name and never invents.

export type Json = string | number | boolean | null | Json[] | { [key: string]: Json };
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type Fields = Record<string, any>;

export interface Manifest {
  project: { id: string; title?: string } | null;
  roots: { consumer?: string; litespec?: string } | null;
  editor: string | null;
  approvals: Record<string, string | { by?: string; approver?: string; rule?: string }> | null;
  testRun: { date?: string; total?: number; passed?: number; failed?: number } | null;
  totals: Record<string, number>;
  layers: { key: string; label?: string; outline?: string[] }[];
  shardOf: Record<string, string>;
  search: string[];
  unplaced?: string[];
  generatedOn?: string;
}

export interface LexiconType { label?: string; plural?: string; layer?: string; question?: string }
export interface LexiconLayer { label?: string; writes?: string; reviews?: string; question?: string }
export interface LexiconLink { field: string; carriers: string[] | string; forward?: string; inverse?: string }

export interface Lexicon {
  types: Record<string, LexiconType>;
  layers: Record<string, LexiconLayer>;
  links: LexiconLink[];
  fields: Record<string, string>;
  glossary: Record<string, { definition?: string; record?: string }>;
  sections: Record<string, { text?: string; source?: string }>;
  findings: Record<string, { meaning?: string }>;
}

export interface OutlineNode {
  id: string;
  kind: string;
  label?: string;
  type?: string | null;
  status?: string | null;
  shard?: string;
  children?: OutlineNode[];
}

export interface Edge { field: string; id: string }

export interface SourceRef { path: string; line?: number | null; litespec?: boolean }

// One record as its pack carries it. Every key past the first few is optional,
// since each record type carries its own details.
export interface BookRecord {
  type: string;
  title: string;
  owner?: string | null;
  status?: string | null;
  effective?: string | null;
  decider?: string | null;
  decideBy?: string | null;
  fields: Fields;
  src?: SourceRef | null;
  parent?: string | null;
  layer?: string | null;
  impl?: Fields[];
  tests?: Fields[];
  expect?: { impl?: boolean; proof?: boolean } | null;
  lacks?: string | null;
  heldBy?: Fields | null;
  findings?: Fields[];
  bodyHtml?: string;
  sections?: { intro?: string; list?: { title: string; html: string }[] } | null;
  fieldsHtml?: Record<string, string>;
  out?: Record<string, string[]> | Fields[];
  in?: Record<string, string[]> | Fields[];
  completion?: Fields | null;
  approval?: { by?: string } | null;
  proof?: { state: string } | null;
  machine?: Fields | null;
  batch?: string;
  [key: string]: unknown;
}

export interface Batch {
  selector: string;
  kind: string;
  riskClasses?: string[];
  approvers?: string[];
  deciders?: string[];
  decideBy?: string | null;
  records?: string[];
  ids?: string[];
  n?: number;
  [key: string]: unknown;
}

export interface Question {
  question: string;
  riskClasses?: string[];
  approval?: unknown;
  decider?: string;
  decideBy?: string | null;
  ballot?: string | null;
  recommendation?: string;
  [key: string]: unknown;
}

export interface Ballot { id: string; i: number; q: Question }
export interface SecurityItem { kind: "cross" | "calls" | "data"; id: string; text: string }

export interface Queue {
  batches: Record<string, Batch>;
  order?: string[];
  ballots: Ballot[];
  security: SecurityItem[];
  review?: { vetoWindowDays?: number; securityReviewer?: string };
}

// A search row: id, title, type, excerpt.
export type SearchRow = [string, string, string, string];
