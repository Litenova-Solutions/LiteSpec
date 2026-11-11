import { describe, expect, it } from "vitest";
import { proofLabel, proofRank } from "./proof-order";

describe("proof-order", () => {
  it("ranks failing first, passing last, and no proof last of all", () => {
    expect(proofRank("failing")).toBe(0);
    expect(proofRank("awaiting")).toBe(1);
    expect(proofRank("unconfirmed")).toBe(2);
    expect(proofRank("norun")).toBe(3);
    expect(proofRank("unproved")).toBe(4);
    expect(proofRank("indirect")).toBe(5);
    expect(proofRank("passing")).toBe(6);
    expect(proofRank(null)).toBe(7);
  });

  it("sorts proof states failing first", () => {
    const states = ["passing", "unproved", "failing", "norun", "awaiting"] as const;
    const sorted = [...states].sort((a, b) => proofRank(a) - proofRank(b));
    expect(sorted).toEqual(["failing", "awaiting", "norun", "unproved", "passing"]);
  });

  it("labels each proof state with its word", () => {
    expect(proofLabel("passing")).toBe("passing");
    expect(proofLabel("failing")).toBe("failing");
    expect(proofLabel("awaiting")).toBe("awaiting");
    expect(proofLabel("unconfirmed")).toBe("unconfirmed");
    expect(proofLabel("unproved")).toBe("unproved");
    expect(proofLabel("indirect")).toBe("indirect");
    expect(proofLabel("norun")).toBe("norun");
  });
});
