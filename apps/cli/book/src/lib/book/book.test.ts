import { describe, expect, it } from "vitest";
import { fixtureBook } from "./fixture";

describe("book", () => {
  it("boots with the product page selected", async () => {
    const book = await fixtureBook();
    expect(book.ready).toBe(true);
    expect(book.error).toBe(null);
    expect(book.selected).toBe("product/library");
    expect(book.title("goal/faster-borrowing")).toBe("Faster Borrowing");
  });

  it("levels, labels and forms read from the lexicon", async () => {
    const book = await fixtureBook();
    expect(book.kindOf("goal/faster-borrowing")).toBe("intent");
    expect(book.kindOf("use-case/library.borrowing.borrow")).toBe("behavior");
    expect(book.typeName("use-case")).toBe("Use case");
    expect(book.pluralName("use-case")).toBe("Use cases");
    expect(book.tag("use-case")).toBe("use case");
    expect(book.fieldLabel("boundary")).toBe("Boundary");
    expect(book.fieldLabel("whatever")).toBe("whatever");
  });

  it("walks a record's relations in reading order", async () => {
    const book = await fixtureBook();
    expect(book.outs("feature/borrow-book", "advances")).toEqual(["goal/faster-borrowing"]);
    expect(book.ins("goal/faster-borrowing", "advances")).toEqual(["feature/borrow-book"]);
    expect(book.traceOf("use-case/library.borrowing.borrow")).toEqual([
      "goal/faster-borrowing",
      "feature/borrow-book",
      "use-case/library.borrowing.borrow",
    ]);
  });

  it("computes completion from code, tests and scenario proof", async () => {
    const book = await fixtureBook();
    expect(book.completeOf("use-case/library.borrowing.borrow")).toBe(false);
    expect(book.completeOf("scenario/library.borrowing.borrow.main")).toBe(false);
    expect(book.completeOf("feature/borrow-book")).toBe(true);
    expect(
      book.completeCounts([
        "feature/borrow-book",
        "goal/faster-borrowing",
        "use-case/library.borrowing.borrow",
        "scenario/library.borrowing.borrow.main",
      ]),
    ).toBe("2 of 4 complete");
    expect(book.completeCounts([])).toBe("");
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("scenarios not tested");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("not tested");
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe("unproved");
  });

  it("explains proof states, levels and types, and nothing about review", async () => {
    const book = await fixtureBook();
    expect(book.explain("passing")?.title).toBe("Completion Stages");
    expect(book.explain("passing")?.text).toBe("Completion is computed from code, tests and passing scenarios.");
    expect(book.explain("G")?.title).toBe("Check level");
    expect(book.explain("known")?.title).toBe("Exception");
    expect(book.explain("type:feature")?.text).toBe(
      "What change delivers the goal? Written by product people.",
    );
    expect(book.explain("draft")).toBe(null);
    expect(book.explain("approved")).toBe(null);
    expect(book.explain("risk")).toBe(null);
    expect(book.explain("ballot")).toBe(null);
  });

  it("filters by layer, type, findings and proof only", async () => {
    const book = await fixtureBook();
    expect(book.facets.map((f) => f.key)).toEqual(["layer", "type", "findings", "proof"]);
    expect(Object.keys(book.sorts)).toEqual(["outline", "title", "proof"]);
    book.toggleFacet("type", "goal");
    expect(book.passes("goal/faster-borrowing")).toBe(true);
    expect(book.passes("feature/borrow-book")).toBe(false);
    book.clearFilters();
    expect(book.passes("feature/borrow-book")).toBe(true);
  });

  it("loads the security items for the Security view", async () => {
    const book = await fixtureBook();
    await book.setView("security");
    expect(book.view).toBe("security");
    expect(book.securityItems()).toEqual([]);
  });

  it("reads the layer claims and the Intent coverage from the manifest", async () => {
    const book = await fixtureBook();
    expect(book.claims().map((claim) => claim.layer)).toEqual(["intent", "behavior", "implementation", "proof"]);
    expect(book.claimed("proof")).toBe(true);
    expect(book.intentCoverage()).toEqual({
      reached: 1, total: 2, share: 0.5,
      requirements: { reached: 1, total: 1 },
      steps: { reached: 0, total: 1 },
      entries: [
        { id: "requirement/borrow-book.main", kind: "requirement", reached: true, reachedBy: ["use-case/library.borrowing.borrow"] },
        { id: "journey/borrowing#s1", kind: "step", reached: false, reachedBy: [] },
      ],
    });
  });

  it("hides proof and lacks in the layers the project does not claim", async () => {
    const book = await fixtureBook("", { claims: [{ layer: "intent", practice: "litespec" }, { layer: "behavior", practice: "litespec" }] });
    expect(book.claimed("implementation")).toBe(false);
    expect(book.claimed("proof")).toBe(false);
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe(null);
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("");
    expect(book.intentCoverage()?.total).toBe(2);
  });

  it("drops the test gap when only implementation is claimed", async () => {
    const book = await fixtureBook("", { claims: [
      { layer: "intent", practice: "litespec" },
      { layer: "behavior", practice: "litespec" },
      { layer: "implementation", practice: "own" },
    ] });
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe(null);
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("");
  });

  it("keeps the test gap and drops the code gap when only proof is claimed", async () => {
    const book = await fixtureBook("", { claims: [
      { layer: "intent", practice: "litespec" },
      { layer: "behavior", practice: "litespec" },
      { layer: "proof", practice: "own" },
    ] });
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe("unproved");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("not tested");
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("scenarios not tested");
  });

  it("shows both identifiers, the short one and the qualified one", async () => {
    const book = await fixtureBook();
    expect(book.qualifiedId("use-case/library.borrowing.borrow")).toBe("library/borrowing/use-case/borrow");
    expect(book.qualifiedId("goal/faster-borrowing")).toBe("goal/faster-borrowing");
    expect(book.qualifiedId("use-case/library.borrowing.borrow#s1")).toBe("library/borrowing/use-case/borrow");
  });
});
