import { describe, expect, it } from "vitest";
import { fixtureBook } from "../frame/fixture";
import { buildOutlineRows } from "./outline-rows";

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
});
