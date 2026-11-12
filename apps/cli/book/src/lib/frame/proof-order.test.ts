import { describe, expect, it } from "vitest";
import { proofLabel, proofRank, proofWord } from "./proof-order";

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

  it("labels each proof state capitalized, and words it lower case", () => {
    expect(proofLabel("passing")).toBe("Passing");
    expect(proofLabel("failing")).toBe("Failing");
    expect(proofLabel("awaiting")).toBe("Awaiting");
    expect(proofLabel("unconfirmed")).toBe("Unconfirmed");
    expect(proofLabel("unproved")).toBe("Unproved");
    expect(proofLabel("indirect")).toBe("Indirect");
    expect(proofLabel("norun")).toBe("Not run");
    expect(proofWord("norun")).toBe("not run");
    expect(proofWord("passing")).toBe("passing");
    expect(proofWord("failing")).toBe("failing");
  });
});
