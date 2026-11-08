// A small book for the store tests: one product, one goal, one feature, one
// use case with a scenario, and the lexicon words the explanations read.

import { Book } from "./book.svelte";
import type { Lexicon, Manifest, OutlineNode, SecurityItem } from "./types";

const records: Record<string, Record<string, any>> = {
  "product/library": {
    type: "product", title: "Riverside Library",
    fields: { boundary: "Lending for one branch." },
  },
  "goal/faster-borrowing": {
    type: "goal", title: "Faster Borrowing",
    fields: {},
    in: [{ from: "feature/borrow-book", key: "advances" }],
  },
  "feature/borrow-book": {
    type: "feature", title: "Borrow Book",
    fields: {},
    out: [
      { to: "goal/faster-borrowing", key: "advances" },
      { to: "use-case/library.borrowing.borrow", key: "fulfilledBy" },
    ],
    in: [{ from: "use-case/library.borrowing.borrow", key: "fulfills" }],
  },
  "use-case/library.borrowing.borrow": {
    type: "use-case", title: "Borrow",
    qualifiedId: "library/borrowing/use-case/borrow",
    fields: {},
    impl: [{ path: "src/borrow.ts", line: 12, kind: "handler" }],
    tests: [],
    expect: { impl: true, proof: true },
    completion: { hasCode: true, hasTests: false, scenarios: { pass: 0, of: 1 } },
    lacks: "scenarios not tested",
    out: [{ to: "feature/borrow-book", key: "fulfills" }],
    in: [{ from: "feature/borrow-book", key: "fulfilledBy" }],
  },
  "scenario/library.borrowing.borrow.main": {
    type: "scenario", title: "Main",
    fields: {},
    tests: [],
    expect: { impl: false, proof: true },
    completion: { hasTests: false },
    proof: { state: "unproved", tests: 0, test: null },
    lacks: "not tested",
  },
  "feature/checkout": {
    type: "feature", title: "Checkout",
    fields: {},
  },
};

const outline: OutlineNode[] = [
  {
    id: "product/library", kind: "record", label: "Riverside Library", type: "product",
    children: [
      {
        id: "goal/faster-borrowing", kind: "record", label: "Faster Borrowing", type: "goal",
        children: [
          {
            id: "feature/borrow-book", kind: "record", label: "Borrow Book", type: "feature",
            children: [
              {
                id: "use-case/library.borrowing.borrow", kind: "record", label: "Borrow", type: "use-case",
                children: [
                  { id: "scenario/library.borrowing.borrow.main", kind: "record", label: "Main", type: "scenario", children: [] },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
];

const lexicon = {
  types: {
    product: { label: "Product", plural: "Products", layer: "intent", question: "What product is this?" },
    goal: { label: "Goal", plural: "Goals", layer: "intent", question: "What outcome does the product want?" },
    feature: { label: "Feature", plural: "Features", layer: "intent", question: "What change delivers the goal?" },
    "use-case": { label: "Use case", plural: "Use cases", layer: "behavior", question: "What does the product do?" },
    scenario: { label: "Scenario", plural: "Scenarios", layer: "behavior", question: "When does the behavior hold?" },
  },
  layers: {
    intent: { label: "Intent", question: "What should the product do?", writes: "product people" },
    behavior: { label: "Behavior", question: "How does the product behave?", writes: "product and engineering" },
  },
  links: [
    { field: "advances", carriers: "feature", forward: "advances the goal", inverse: "is advanced by the feature" },
    { field: "fulfills", carriers: "use-case", forward: "fulfills the feature", inverse: "is fulfilled by the use case" },
    { field: "fulfilledBy", carriers: "feature", forward: "is fulfilled by the use case", inverse: "fulfills the feature" },
  ],
  sections: {
    "Completion Stages": { text: "Completion is computed from code, tests and passing scenarios.", source: "model.md, Completion Stages" },
    "Check Levels": { text: "Blocking findings stop a release.", source: "model.md, Check Levels" },
  },
  glossary: {
    finding: { term: "Finding", definition: "A check result on a record." },
    exception: { term: "Exception", definition: "A finding the product accepts." },
  },
  fields: { boundary: "Boundary" },
  findings: { "goal-unused": { level: "G", meaning: "A goal nothing advances." } },
} as unknown as Lexicon;

const manifest = {
  project: { id: "product/library", title: "Riverside Library" },
  roots: { consumer: "C:/work/shop", litespec: "C:/work/litespec" },
  editor: "vscode://file/{root}/{path}:{line}",
  conformance: "full",
  intentCoverage: {
    reached: 1, total: 2, share: 0.5,
    requirements: { reached: 1, total: 1 },
    steps: { reached: 0, total: 1 },
    entries: [
      { id: "requirement/borrow-book.main", kind: "requirement", reached: true, reachedBy: ["use-case/library.borrowing.borrow"] },
      { id: "journey/borrowing#s1", kind: "step", reached: false, reachedBy: [] },
    ],
  },
  testRun: null,
  totals: { product: 1, goal: 1, feature: 2, "use-case": 1, scenario: 1 },
  layers: [
    { key: "intent", label: "Intent", outline: ["outline-intent"] },
    { key: "behavior", label: "Behavior", outline: ["outline-behavior"] },
  ],
  shardOf: {
    "product/library": "records-a", "goal/faster-borrowing": "records-a", "feature/borrow-book": "records-a",
    "use-case/library.borrowing.borrow": "records-a", "scenario/library.borrowing.borrow.main": "records-a",
    "feature/checkout": "records-a",
  },
  search: [],
  unplaced: [],
  generatedOn: "2026-01-02T00:00:00.000Z",
} as unknown as Manifest;

const security: SecurityItem[] = [];

const shards: Record<string, unknown> = {
  manifest,
  lexicon,
  security,
  "outline-intent": outline,
  "outline-behavior": [],
  "records-a": records,
};

/** A book over the fixture shards, booted and ready for assertions. */
export async function fixtureBook(hash = "", opts: { editor?: string; conformance?: string } = {}) {
  const data: Record<string, unknown> = structuredClone(shards);
  if (opts.editor !== undefined) (data.manifest as Manifest).editor = opts.editor;
  if (opts.conformance !== undefined) (data.manifest as Manifest).conformance = opts.conformance;
  const book = new Book({ load: async (key: string) => structuredClone(data[key]) } as any);
  await book.boot(hash);
  return book;
}
