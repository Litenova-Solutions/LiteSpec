import { describe, expect, it } from "vitest";
import { completionLines, coveragePercent } from "./completion";

describe("completion", () => {
  it("marks nothing expected when the record expects nothing", () => {
    expect(completionLines(null)).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
    expect(completionLines(undefined)).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
    expect(completionLines({})).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
  });

  it("draws code and tests lines present or missing", () => {
    expect(completionLines({ hasCode: true, hasTests: true })).toEqual([
      { label: "Has code", mark: "present" },
      { label: "Has tests", mark: "present" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
    expect(completionLines({ hasCode: false, hasTests: false })).toEqual([
      { label: "No code", mark: "missing" },
      { label: "No tests", mark: "missing" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
    expect(completionLines({ hasCode: null, hasTests: null })).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "Scenarios", mark: "none-expected" },
    ]);
  });

  it("draws the scenarios line with its counts", () => {
    expect(completionLines({ scenarios: { pass: 2, of: 3 } })).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "2 of 3 scenarios pass", mark: "missing" },
    ]);
    expect(completionLines({ scenarios: { pass: 3, of: 3 } })).toEqual([
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "3 of 3 scenarios pass", mark: "present" },
    ]);
    expect(completionLines({ scenarios: { pass: 1, of: 1 } })[2]).toEqual({
      label: "1 of 1 scenario passes",
      mark: "present",
    });
  });

  it("reads the coverage percent from the share or the counts", () => {
    expect(coveragePercent({ share: 0.5, reached: 1, total: 2 })).toBe(50);
    expect(coveragePercent({ reached: 1, total: 2 })).toBe(50);
    expect(coveragePercent({ share: null, reached: 1, total: 4 })).toBe(25);
    expect(coveragePercent({ reached: 0, total: 0 })).toBe(100);
  });
});
