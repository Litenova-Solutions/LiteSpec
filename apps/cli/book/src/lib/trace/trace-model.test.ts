import { describe, expect, it } from "vitest";
import { fixtureBook } from "$lib/frame";
import { breaksIntentChain, downOf } from "./trace-model";

describe("the trace down from a vision", () => {
  it("leads to the decisions based on it and the requirements citing it", async () => {
    const book = await fixtureBook();
    book.rec["vision/branch-lending"] = {
      type: "vision",
      title: "Branch Lending",
      fields: { statement: "Members borrow at one branch and return at any other." },
      in: [
        { id: "decision/0001-loan-ledger", key: "basedOn" },
        { id: "requirement/borrow-book.any-branch", key: "cites" },
      ],
    };
    book.rec["decision/0001-loan-ledger"] = {
      type: "decision",
      title: "Loan Ledger",
      fields: {},
      out: [{ id: "vision/branch-lending", key: "basedOn" }],
    };
    book.rec["requirement/borrow-book.any-branch"] = {
      type: "requirement",
      title: "Any Branch",
      fields: {},
      out: [{ id: "vision/branch-lending", key: "cites" }],
    };

    expect(downOf(book, "vision/branch-lending")).toEqual([
      { id: "decision/0001-loan-ledger", phrase: "is based on this vision" },
      { id: "requirement/borrow-book.any-branch", phrase: "cites this vision" },
    ]);
  });

  it("never marks a vision as a break in the Intent chain", async () => {
    const book = await fixtureBook();
    book.rec["vision/branch-lending"] = { type: "vision", title: "Branch Lending", fields: {} };

    expect(breaksIntentChain(book, "vision/branch-lending", 0)).toBe(false);
  });
});
