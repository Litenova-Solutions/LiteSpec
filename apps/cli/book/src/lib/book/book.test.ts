import { describe, expect, it } from "vitest";
import { fixtureBook } from "./fixture";

describe("the book store", () => {
  it("opens the product when the address names no record", async () => {
    const book = await fixtureBook();
    expect(book.selected).toBe("product/shop");
    expect(book.title("feature/checkout")).toBe("Checkout");
    expect(book.parent("goal/sell")).toBe("product/shop");
  });

  it("opens the record the address names, loading its layer", async () => {
    const book = await fixtureBook("use-case/orders.pay");
    expect(book.selected).toBe("use-case/orders.pay");
    expect(book.layerOf["use-case/orders.pay"]).toBe("behavior");
    expect(book.isBeh("use-case/orders.pay")).toBe(true);
  });

  it("reads every link in the Links table phrases", async () => {
    const book = await fixtureBook();
    expect(book.outs("feature/checkout", "advances")).toEqual(["goal/sell"]);
    expect(book.ins("goal/sell", "advances")).toEqual(["feature/checkout"]);
    const conns = book.connectionsOf("goal/sell");
    expect(conns[0].label).toBe("Points at this: advances");
    expect(book.linkSentence("goal/sell", conns[0].label, conns)).toBe("This goal is advanced by");
  });

  it("traces a record up to its goal", async () => {
    const book = await fixtureBook();
    await book.ensureRecords(["use-case/orders.pay"]);
    expect(book.traceOf("use-case/orders.pay")).toEqual(["goal/sell", "feature/checkout", "requirement/pay.card", "use-case/orders.pay"]);
  });

  it("names review state in LiteSpec words and approvers from the approval table", async () => {
    const book = await fixtureBook();
    expect(book.human("requirement/pay.card")).toBe("draft");
    expect(book.needs("requirement/pay.card")).toBe(true);
    expect(book.human("use-case/orders.pay")).toBe("reviewed");
    expect(book.riskWords("feature/checkout")).toEqual(["Money"]);
    expect(book.approverFor("money", "feature/checkout")).toBe("Founder");
    expect(book.approverFor(undefined, "goal/sell")).toBe("agent-batch");
  });

  it("explains a status and a record type from the lexicon, and nothing it does not hold", async () => {
    const book = await fixtureBook();
    expect(book.explain("draft")).toEqual({ title: "Draft", text: "A record is draft until its decider approves it.", source: "model.md, Statuses" });
    expect(book.explain("type:feature")?.text).toContain("What is offered against a problem?");
    expect(book.explain("type:feature")?.text).toContain("Written by agents draft, the decider decides");
    expect(book.explain("stale")).toBeNull();
  });

  it("filters by facet and keeps the ancestors of a match visible", async () => {
    const book = await fixtureBook();
    book.toggleFacet("status", "draft");
    expect(book.matches("requirement/pay.card")).toBe(true);
    expect(book.isVisible("product/shop")).toBe(true);
    expect(book.matches("product/shop")).toBe(false);
    expect(book.isVisible("goal/sell")).toBe(false);
  });

  it("searches titles and text with prefixes and small typos", async () => {
    const book = await fixtureBook();
    await book.ensureSearch();
    book.setQuery("chekout");
    expect(book.passes("feature/checkout")).toBe(true);
    expect(book.passes("goal/sell")).toBe(false);
    book.setQuery("pay car");
    expect(book.bestHit()).toBe("requirement/pay.card");
  });

  it("moves to the next record that needs a decision after an approval", async () => {
    const book = await fixtureBook();
    expect(book.nextNeeding("product/shop")).toBe("requirement/pay.card");
    await book.select("requirement/pay.card");
    book.setVerdict("mine");
    expect(book.human("requirement/pay.card")).toBe("mine");
    expect(book.nextNeeding("product/shop")).toBeNull();
  });

  it("writes the review file the model review command reads", async () => {
    const book = await fixtureBook();
    await book.select("requirement/pay.card");
    book.setVerdict("changes");
    book.saveNote("Name the card networks.");
    expect(book.reviewFile().verdicts).toEqual([{ record: "requirement/pay.card", verdict: "request-changes", note: "Name the card networks." }]);
  });
});
