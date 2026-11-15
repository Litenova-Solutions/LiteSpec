import { describe, expect, it } from "vitest";
import { fixtureBook } from "../frame/fixture";
import { buildOutlineRows, expandChain } from "./outline-rows";

describe("outline-rows", () => {
  it("groups decisions by the concerns the book command computed", async () => {
    const book = await fixtureBook();
    book.lex.types["decision"] = {
      label: "Decision",
      plural: "Decisions",
      layer: "intent",
      question: "What was chosen?",
    };
    book.rec["decision/0001-render-receipts"] = {
      type: "decision",
      title: "Render Receipts",
      fields: {},
      concerns: ["tests"],
      in: [{ id: "code/apps/api/qa/Loans.cs", key: "follows" }],
    };
    book.rec["decision/0002-receipt-threshold"] = {
      type: "decision",
      title: "Receipt Threshold",
      fields: {},
      in: [{ id: "code/src/Domain/Loan.cs", key: "follows" }],
    };
    for (const id of ["decision/0001-render-receipts", "decision/0002-receipt-threshold"]) {
      book.layerOf[id] = "intent";
      const roots = book.rootsOf["intent"] ?? [];
      roots.push(id);
      book.rootsOf["intent"] = roots;
      book.order.push(id);
    }

    const rows = buildOutlineRows(book);
    const groups = rows.flatMap((row) => (row.kind === "group" ? [row.label] : []));
    expect(groups).toContain("No kind: Tests");
    expect(groups).toContain("No kind");
    expect(groups).not.toContain("No kind: Application");
    expect(groups).not.toContain("No kind: Domain");
  });

  it("gives every row a unique key when everything is open, filtered and sorted", async () => {
    const book = await fixtureBook();
    book.openLayers["behavior"] = true;
    for (const id of book.order) if (book.kids(id).length) book.expanded[id] = true;
    for (const sortKey of ["outline", "title", "proof"] as const) {
      book.sortKey = sortKey;
      for (const query of ["", "borrow"]) {
        book.setQuery(query);
        const rows = buildOutlineRows(book);
        const keys = rows.map((row) => row.key);
        expect(rows.length).toBeGreaterThan(0);
        expect(new Set(keys).size).toBe(keys.length);
      }
    }
  });

  it("keeps row keys unique when group labels hold slashes", async () => {
    const book = await fixtureBook();
    book.lex.types["decision"] = {
      label: "Decision",
      plural: "Decisions",
      layer: "intent",
      question: "What was chosen?",
    };
    book.rec["decision/0001-plan"] = {
      type: "decision",
      title: "Plan",
      fields: { kind: "plan/act" },
      concerns: ["tests"],
      in: [],
    };
    book.rec["decision/0002-act"] = {
      type: "decision",
      title: "Act",
      fields: { kind: "plan" },
      in: [],
    };
    for (const id of ["decision/0001-plan", "decision/0002-act"]) {
      book.layerOf[id] = "intent";
      const roots = book.rootsOf["intent"] ?? [];
      roots.push(id);
      book.rootsOf["intent"] = roots;
      book.order.push(id);
    }
    for (const id of book.order) if (book.kids(id).length) book.expanded[id] = true;

    const rows = buildOutlineRows(book);
    const keys = rows.map((row) => row.key);
    expect(new Set(keys).size).toBe(keys.length);
  });

  it("carries each layer's counts on its layer row", async () => {
    const book = await fixtureBook();
    const rows = buildOutlineRows(book);
    expect(rows.find((row) => row.kind === "layer" && row.layer === "intent")).toMatchObject({ total: 5, done: 3 });

    book.setQuery("borrow");
    const filtered = buildOutlineRows(book);
    expect(filtered.find((row) => row.kind === "layer" && row.layer === "intent")).toMatchObject({ total: 5, shown: 3, done: 3 });
  });

  it("sorts the records of one group by title", async () => {
    const book = await fixtureBook();
    book.lex.types["decision"] = {
      label: "Decision",
      plural: "Decisions",
      layer: "intent",
      question: "What was chosen?",
    };
    book.rec["decision/0001-zulu"] = { type: "decision", title: "Zulu", fields: {}, in: [] };
    book.rec["decision/0002-alpha"] = { type: "decision", title: "Alpha", fields: {}, in: [] };
    for (const id of ["decision/0001-zulu", "decision/0002-alpha"]) {
      book.layerOf[id] = "intent";
      book.rootsOf["intent"] = [...(book.rootsOf["intent"] ?? []), id];
      book.order.push(id);
    }
    book.sortKey = "title";

    const rows = buildOutlineRows(book);
    const ids = rows.flatMap((row) => (row.kind === "record" ? [row.id] : []));
    expect(ids.indexOf("decision/0002-alpha")).toBeLessThan(ids.indexOf("decision/0001-zulu"));
  });

  it("opens a record's chain without closing other branches", async () => {
    const book = await fixtureBook();
    book.expanded = { "decision/0001-plan": true };
    book.closedGroups = { "goal/faster-borrowing|field:Faster Borrowing/Faster Borrowing": true };

    expandChain(book, "scenario/library.borrowing.borrow.main");

    expect(book.expanded["decision/0001-plan"]).toBe(true);
    expect(book.expanded["product/library"]).toBe(true);
    expect(book.expanded["goal/faster-borrowing"]).toBe(true);
    expect(book.expanded["feature/borrow-book"]).toBe(true);
    expect(book.expanded["use-case/library.borrowing.borrow"]).toBe(true);
    expect(book.closedGroups["goal/faster-borrowing|field:Faster Borrowing/Faster Borrowing"]).toBeUndefined();
    expect(book.openLayers["intent"]).toBe(true);
  });
});
