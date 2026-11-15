import { describe, expect, it } from "vitest";
import { fixtureBook } from "./fixture";
import { layerStats } from "./relations";

describe("book", () => {
  it("boots with the product page selected", async () => {
    const book = await fixtureBook();
    expect(book.load).toEqual({ kind: "ready" });
    expect(book.selected).toBe("product/library");
    expect(book.title("goal/faster-borrowing")).toBe("Faster Borrowing");
  });

  it("levels, labels and forms read from the lexicon", async () => {
    const book = await fixtureBook();
    expect(book.kindOf("goal/faster-borrowing")).toBe("intent");
    expect(book.kindOf("use-case/library.borrowing.borrow")).toBe("behavior");
    expect(book.kindOf("code/src/borrow.ts")).toBe("unlayered");
    expect(book.typeName("use-case")).toBe("Use case");
    expect(book.pluralName("use-case")).toBe("Use cases");
    expect(book.typeTag("use-case")).toBe("use case");
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
    expect(book.breadcrumbOf("use-case/library.borrowing.borrow")).toEqual([
      "product/library",
      "goal/faster-borrowing",
      "feature/borrow-book",
    ]);
    expect(book.breadcrumbOf("product/library")).toEqual([]);
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
    expect(
      book.completionSummary([
        "feature/borrow-book",
        "goal/faster-borrowing",
        "use-case/library.borrowing.borrow",
        "scenario/library.borrowing.borrow.main",
      ]),
    ).toEqual({ done: 2, total: 4, percent: 50 });
    expect(book.completionSummary([])).toEqual({ done: 0, total: 0, percent: 100 });
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("scenarios not tested");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("not tested");
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe("unproved");
    expect(book.hasFailingProof("scenario/library.borrowing.borrow.main")).toBe(false);
    expect(book.hasFailingProof("feature/borrow-book")).toBe(false);
  });

  it("explains proof states, levels and types, and nothing about review", async () => {
    const book = await fixtureBook();
    expect(book.explain("passing")?.title).toBe("Completion Stages");
    expect(book.explain("passing")?.text).toBe("Completion is computed from code, tests and passing scenarios.");
    expect(book.explain("G")?.title).toBe("Check level");
    expect(book.explain("covered")?.title).toBe("Covered");
    expect(book.explain("type:feature")?.text).toBe("What change delivers the goal? Written by product people.");
    expect(book.explain("draft")).toBe(null);
    expect(book.explain("approved")).toBe(null);
    expect(book.explain("risk")).toBe(null);
    expect(book.explain("ballot")).toBe(null);
  });

  it("filters by layer, type, findings and proof only", async () => {
    const book = await fixtureBook();
    expect(book.facets.map((facet) => facet.key)).toEqual(["layer", "type", "findings", "proof"]);
    expect(Object.keys(book.sorts)).toEqual(["outline", "title", "proof"]);
    book.toggleFacet("type", "goal");
    expect(book.passesFilters("goal/faster-borrowing")).toBe(true);
    expect(book.passesFilters("feature/borrow-book")).toBe(false);
    book.clearFilters();
    expect(book.passesFilters("feature/borrow-book")).toBe(true);
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
      reached: 1,
      total: 2,
      share: 0.5,
      requirements: { reached: 1, total: 1 },
      steps: { reached: 0, total: 1 },
      entries: [
        {
          id: "requirement/borrow-book.main",
          kind: "requirement",
          reached: true,
          reachedBy: ["use-case/library.borrowing.borrow"],
        },
        { id: "flow/borrowing#s1", kind: "step", reached: false, reachedBy: [] },
      ],
    });
  });

  it("hides proof and lacks in the layers the project does not claim", async () => {
    const book = await fixtureBook("", {
      claims: [
        { layer: "intent", practice: "litespec" },
        { layer: "behavior", practice: "litespec" },
      ],
    });
    expect(book.claimed("implementation")).toBe(false);
    expect(book.claimed("proof")).toBe(false);
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe(null);
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("");
    expect(book.intentCoverage()?.total).toBe(2);
  });

  it("drops the test gap when only implementation is claimed", async () => {
    const book = await fixtureBook("", {
      claims: [
        { layer: "intent", practice: "litespec" },
        { layer: "behavior", practice: "litespec" },
        { layer: "implementation", practice: "own" },
      ],
    });
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe(null);
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("");
  });

  it("keeps the test gap and drops the code gap when only proof is claimed", async () => {
    const book = await fixtureBook("", {
      claims: [
        { layer: "intent", practice: "litespec" },
        { layer: "behavior", practice: "litespec" },
        { layer: "proof", practice: "own" },
      ],
    });
    expect(book.proofState("scenario/library.borrowing.borrow.main")).toBe("unproved");
    expect(book.lacks("scenario/library.borrowing.borrow.main")).toBe("not tested");
    expect(book.lacks("use-case/library.borrowing.borrow")).toBe("scenarios not tested");
  });

  it("counts a layer's real records and record types", async () => {
    const book = await fixtureBook();
    expect(layerStats(book, "intent")).toEqual({
      ids: [
        "product/library",
        "goal/faster-borrowing",
        "feature/borrow-book",
        "use-case/library.borrowing.borrow",
        "scenario/library.borrowing.borrow.main",
      ],
      types: ["product", "goal", "feature", "use-case", "scenario"],
    });
    expect(layerStats(book, "behavior")).toEqual({ ids: [], types: [] });
  });

  it("shows the folder and the module a record sits in", async () => {
    const book = await fixtureBook();
    expect(book.placement("use-case/library.borrowing.borrow")).toEqual({
      folder: "behavior/library/borrowing",
      module: "library",
    });
  });

  it("reads problem and constraint coverage entries", async () => {
    const book = await fixtureBook("", {
      coverage: {
        reached: 2,
        total: 4,
        share: 0.5,
        requirements: { reached: 1, total: 1 },
        steps: { reached: 0, total: 1 },
        problems: { reached: 1, total: 1 },
        constraints: { reached: 0, total: 1 },
        entries: [
          { id: "requirement/borrow-book.main", kind: "requirement", reached: true, reachedBy: ["feature/borrow-book"] },
          { id: "flow/borrowing#s1", kind: "step", reached: false, reachedBy: [] },
          { id: "problem/slow-borrowing", kind: "problem", reached: true, reachedBy: ["feature/borrow-book"] },
          { id: "constraint/one-branch", kind: "constraint", reached: false, reachedBy: [] },
        ],
      },
    });
    const coverage = book.intentCoverage();
    expect(coverage?.entries.map((entry) => entry.kind)).toEqual(["requirement", "step", "problem", "constraint"]);
    expect(coverage?.problems).toEqual({ reached: 1, total: 1 });
    expect(coverage?.constraints).toEqual({ reached: 0, total: 1 });
  });

  it("re-selecting the current record pushes no history entry", async () => {
    const book = await fixtureBook();
    const pushes: string[] = [];
    const push = history.pushState.bind(history);
    history.pushState = ((...args: [unknown, string, string?]) => {
      pushes.push(String(args[2] ?? ""));
      return push(...args);
    }) as typeof history.pushState;
    try {
      await book.select("feature/borrow-book");
      const count = pushes.length;
      await book.select("feature/borrow-book");
      expect(pushes.length).toBe(count);
    } finally {
      history.pushState = push;
    }
  });

  it("keeps other open branches when the selection moves", async () => {
    const book = await fixtureBook();
    book.expanded = { "goal/faster-borrowing": true, "feature/borrow-book": true };
    await book.select("use-case/library.borrowing.borrow");
    expect(book.expanded["goal/faster-borrowing"]).toBe(true);
    expect(book.expanded["feature/borrow-book"]).toBe(true);
    expect(book.expanded["use-case/library.borrowing.borrow"]).toBe(true);
  });

  it("shows the record before its shards arrive", async () => {
    const book = await fixtureBook();
    const pending = book.select("feature/checkout");
    expect(book.selected).toBe("feature/checkout");
    await pending;
    expect(book.selected).toBe("feature/checkout");
  });

  it("bumps only the security revision when the security shard arrives", async () => {
    const book = await fixtureBook();
    const outline = book.outlineRev;
    const rec = book.recRev;
    const search = book.searchRev;
    const security = book.securityRev;
    const rev = book.rev;
    await book.setView("security");
    expect(book.securityRev).toBeGreaterThan(security);
    expect(book.outlineRev).toBe(outline);
    expect(book.recRev).toBe(rec);
    expect(book.searchRev).toBe(search);
    expect(book.rev).toBeGreaterThan(rev);
  });

  it("follows the rendered outline order for keyboard moves", async () => {
    const book = await fixtureBook();
    expect(book.navOrder()).toContain("feature/borrow-book");
    book.setNavOrder(["feature/borrow-book", "goal/faster-borrowing"]);
    expect(book.navOrder()).toEqual(["feature/borrow-book", "goal/faster-borrowing"]);
  });

  it("finds the first direct match for the search box", async () => {
    const book = await fixtureBook();
    expect(book.firstMatch()).toBeNull();
    book.setQuery("Borrow Book");
    expect(book.firstMatch()).toBe("feature/borrow-book");
  });

  it("replaces the history entry when asked", async () => {
    const book = await fixtureBook();
    const replaces: string[] = [];
    const pushes: string[] = [];
    const replace = history.replaceState.bind(history);
    const push = history.pushState.bind(history);
    history.replaceState = ((...args: [unknown, string, string?]) => {
      replaces.push(String(args[2] ?? ""));
      return replace(...args);
    }) as typeof history.replaceState;
    history.pushState = ((...args: [unknown, string, string?]) => {
      pushes.push(String(args[2] ?? ""));
      return push(...args);
    }) as typeof history.pushState;
    try {
      await book.select("feature/borrow-book", { replace: true });
      expect(replaces.length).toBeGreaterThan(0);
      expect(pushes.length).toBe(0);
    } finally {
      history.replaceState = replace;
      history.pushState = push;
    }
  });
});
