import { describe, expect, it } from "vitest";
import { createTermMatcher } from "./glossary-links";

describe("the shared term matcher", () => {
  it("matches the longest term first at one position", () => {
    const matcher = createTermMatcher([
      { term: "bank", recordId: null },
      { term: "bank statement", recordId: "term/bank-statement" },
    ]);
    expect(matcher).not.toBeNull();
    matcher!.pattern.lastIndex = 0;
    const found = matcher!.pattern.exec("a bank statement arrives");
    expect(found?.[0]).toBe("bank statement");
    expect(found?.index).toBe(2);
  });

  it("matches terms case-insensitively and keeps one record per term", () => {
    const matcher = createTermMatcher([
      { term: "Ledger", recordId: "term/ledger" },
      { term: "ledger", recordId: "term/ledger-duplicate" },
    ]);
    matcher!.pattern.lastIndex = 0;
    const found = matcher!.pattern.exec("the LEDGER balances");
    expect(found?.[0]).toBe("LEDGER");
    expect(matcher!.recordOf.get("ledger")).toBe("term/ledger");
  });

  it("reads a term with regex characters literally", () => {
    const matcher = createTermMatcher([{ term: "entry (posted)", recordId: null }]);
    matcher!.pattern.lastIndex = 0;
    expect(matcher!.pattern.exec("one entry (posted) today")?.[0]).toBe("entry (posted)");
    matcher!.pattern.lastIndex = 0;
    expect(matcher!.pattern.exec("one entry posted today")).toBeNull();
  });

  it("links no term of two letters or fewer", () => {
    expect(createTermMatcher([{ term: "id", recordId: null }])).toBeNull();
    expect(createTermMatcher([])).toBeNull();
  });
});
