import { createShardLoader } from "$lib/data/shards";
import { Book } from "./book.svelte";
import type { BookRecord, Lexicon, Manifest, OutlineNode, Queue } from "./types";

// A small book in memory: a product, a goal, a feature that advances it, a
// requirement that cites it and a use case that fulfills the requirement.

export const lexicon: Lexicon = {
  types: {
    product: { label: "Product", plural: "Products", layer: "intent", question: "What is built, for whom, and what is excluded?" },
    goal: { label: "Goal", plural: "Goals", layer: "intent", question: "Which measurable outcome does the product serve?" },
    feature: { label: "Feature", plural: "Features", layer: "intent", question: "What is offered against a problem?" },
    requirement: { label: "Requirement", plural: "Requirements", layer: "intent", question: "What does the product do or guarantee?" },
    "use-case": { label: "Use case", plural: "Use cases", layer: "behavior", question: "Which use cases does the product offer?" },
  },
  layers: {
    intent: { label: "Intent", writes: "agents draft, the decider decides", reviews: "the decider and product people" },
    behavior: { label: "Behavior", writes: "agents", reviews: "engineering" },
  },
  links: [
    { field: "advances", carriers: ["feature"], forward: "advances", inverse: "is advanced by" },
    { field: "cites", carriers: ["requirement"], forward: "cites", inverse: "is cited by" },
    { field: "fulfills", carriers: ["use-case"], forward: "fulfills", inverse: "is fulfilled by" },
  ],
  fields: { statement: "Statement", advances: "Advances" },
  glossary: { "effective status": { definition: "the status a record takes from the records above it" } },
  sections: { Statuses: { text: "A record is draft until its decider approves it.", source: "model.md, Statuses" } },
  findings: {},
};

const rec = (r: Partial<BookRecord> & { type: string; title: string }): BookRecord => ({ status: "approved", effective: "approved", fields: {}, ...r });

export const records: Record<string, BookRecord> = {
  "product/shop": rec({ type: "product", title: "Shop", src: { path: ".litespec/intent/brief.yaml", line: 1 } }),
  "goal/sell": rec({ type: "goal", title: "Sell Tickets", out: {}, in: { "feature.advances": ["feature/checkout"] } }),
  "feature/checkout": rec({ type: "feature", title: "Checkout", fields: { lifecycle: "planned", risk: ["money"] }, out: { advances: ["goal/sell"] }, in: { cites: ["requirement/pay.card"] } }),
  "requirement/pay.card": rec({ type: "requirement", title: "Pay by Card", status: "draft", effective: "draft", decider: "Founder", decideBy: "2026-11-10", fields: { statement: "A buyer pays by `feature/checkout` card." }, out: { cites: ["feature/checkout"] }, in: { fulfills: ["use-case/orders.pay"] } }),
  "use-case/orders.pay": rec({ type: "use-case", title: "Pay", status: "reviewed", effective: "reviewed", out: { fulfills: ["requirement/pay.card"] } }),
};

const outline: Record<string, OutlineNode[]> = {
  "outline-intent-0": [
    {
      id: "product/shop", kind: "record", label: "Shop", type: "product", status: "approved",
      children: [
        { id: "goal/sell", kind: "record", label: "Sell Tickets", type: "goal", status: "approved", children: [] },
        { id: "feature/checkout", kind: "record", label: "Checkout", type: "feature", status: "approved", children: [] },
        { id: "requirement/pay.card", kind: "record", label: "Pay by Card", type: "requirement", status: "draft", children: [] },
      ],
    },
  ],
  "outline-behavior-0": [{ id: "use-case/orders.pay", kind: "record", label: "Pay", type: "use-case", status: "reviewed", children: [] }],
};

export const manifest: Manifest = {
  project: { id: "product/shop", title: "Shop" },
  roots: { consumer: "C:/work/shop", litespec: "C:/work/litespec" },
  editor: "vscode://file/{root}/{path}:{line}",
  approvals: { money: "Founder", default: "agent-batch" },
  testRun: null,
  totals: { product: 1, goal: 1, feature: 1, requirement: 1, "use-case": 1 },
  layers: [{ key: "intent", label: "Intent", outline: ["outline-intent-0"] }, { key: "behavior", label: "Behavior", outline: ["outline-behavior-0"] }],
  shardOf: Object.fromEntries(Object.keys(records).map((id) => [id, "records-000"])),
  search: ["search-0"],
  generatedOn: "2026-11-03",
};

const queue: Queue = { batches: {}, ballots: [], security: [], review: { vetoWindowDays: 7, securityReviewer: "Founder" } };

/** A book whose shards resolve from memory, booted on the given record. */
export async function fixtureBook(start = "", overrides: Partial<Manifest> = {}) {
  const shards = createShardLoader(document);
  const values: Record<string, unknown> = {
    manifest: { ...manifest, ...overrides }, lexicon, queue, "records-000": records, ...outline,
    "search-0": Object.entries(records).map(([id, r]) => [id, r.title, r.type, String(r.fields.statement ?? "")]),
  };
  shards.load = async <T>(key: string) => values[key] as T;
  const book = new Book(shards);
  history.replaceState(null, "", "#");
  await book.boot(start);
  return book;
}
