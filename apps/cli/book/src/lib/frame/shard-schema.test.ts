import { describe, expect, it } from "vitest";
import {
  parseBookEdgeJson,
  parseBookRecord,
  parseLexicon,
  parseManifest,
  parseOutlineNodes,
  parseRecordPack,
  parseSearchRows,
  parseSecurityItems,
} from "./shard-schema";

describe("shard-schema", () => {
  it("parses an edge with its id and key, ignoring old spellings", () => {
    expect(parseBookEdgeJson({ id: "goal/a", key: "advances" })).toEqual({ id: "goal/a", key: "advances" });
    expect(parseBookEdgeJson({ id: "goal/a", key: "advances", to: "goal/a", field: "advances" })).toEqual({
      id: "goal/a",
      key: "advances",
    });
    expect(() => parseBookEdgeJson({ key: "advances" })).toThrow();
    expect(() => parseBookEdgeJson({ id: "goal/a" })).toThrow();
    expect(() => parseBookEdgeJson({ id: "", key: "advances" })).toThrow();
  });

  it("parses a record with its required fields", () => {
    const record = parseBookRecord("feature/a", { type: "feature", title: "A", fields: {} });
    expect(record.type).toBe("feature");
    expect(record.title).toBe("A");
    expect(record.fields).toEqual({});
    expect(() => parseBookRecord("feature/a", { title: "A", fields: {} })).toThrow();
    expect(() => parseBookRecord("feature/a", { type: "feature", title: "A" })).toThrow();
  });

  it("keeps lacks always an array, wrapping a lone string", () => {
    expect(parseBookRecord("use-case/a", { type: "use-case", title: "A", fields: {}, lacks: ["not tested"] }).lacks).toEqual([
      "not tested",
    ]);
    expect(parseBookRecord("use-case/a", { type: "use-case", title: "A", fields: {}, lacks: "not tested" }).lacks).toEqual([
      "not tested",
    ]);
    expect(parseBookRecord("use-case/a", { type: "use-case", title: "A", fields: {} }).lacks).toBeUndefined();
  });

  it("rejects an unknown proof state or capture state", () => {
    expect(() =>
      parseBookRecord("scenario/a", { type: "scenario", title: "A", fields: {}, proof: { state: "passing" } }),
    ).not.toThrow();
    expect(() =>
      parseBookRecord("scenario/a", { type: "scenario", title: "A", fields: {}, proof: { state: "done" } }),
    ).toThrow();
    expect(() =>
      parseBookRecord("screen/a", { type: "screen", title: "A", fields: {}, captureState: "captured" }),
    ).not.toThrow();
    expect(() =>
      parseBookRecord("screen/a", { type: "screen", title: "A", fields: {}, captureState: "half" }),
    ).toThrow();
  });

  it("parses decision concerns when the book command wrote them", () => {
    const record = parseBookRecord(
      "decision/0001-a",
      { type: "decision", title: "A", fields: {}, concerns: ["tests", "application"] },
    );
    expect(record.concerns).toEqual(["tests", "application"]);
    expect(parseBookRecord("decision/0001-a", { type: "decision", title: "A", fields: {} }).concerns).toBeUndefined();
    expect(() =>
      parseBookRecord("decision/0001-a", { type: "decision", title: "A", fields: {}, concerns: "tests" }),
    ).toThrow();
  });

  it("parses a records pack by id", () => {
    const pack = parseRecordPack({
      "feature/a": { type: "feature", title: "A", fields: {} },
      "feature/b": { type: "feature", title: "B", fields: {}, out: [{ id: "feature/a", key: "relates" }] },
    });
    expect(Object.keys(pack).sort()).toEqual(["feature/a", "feature/b"]);
    expect(pack["feature/b"]?.out).toEqual([{ id: "feature/a", key: "relates" }]);
    expect(() => parseRecordPack({ "feature/a": { title: "A", fields: {} } })).toThrow();
  });

  it("parses outline nodes with their children", () => {
    const nodes = parseOutlineNodes([
      {
        id: "feature/a",
        kind: "record",
        label: "A",
        type: "feature",
        children: [{ id: "feature/b", kind: "record", label: "B", children: [] }],
      },
    ]);
    expect(nodes.length).toBe(1);
    expect(nodes[0]?.children.length).toBe(1);
    expect(() => parseOutlineNodes([{ id: "feature/a", kind: "page", label: "A", children: [] }])).toThrow();
  });

  it("parses search rows with string cells", () => {
    expect(parseSearchRows([["feature/a", "A", "feature", "excerpt"]])).toEqual([["feature/a", "A", "feature", "excerpt"]]);
    expect(() => parseSearchRows([["feature/a", 3]])).toThrow();
    expect(() => parseSearchRows("rows")).toThrow();
  });

  it("parses security items with their kind", () => {
    expect(parseSecurityItems([{ kind: "cross", id: "use-case/a", text: "Acts across tenants" }])).toEqual([
      { kind: "cross", id: "use-case/a", text: "Acts across tenants" },
    ]);
    expect(() => parseSecurityItems([{ kind: "network", id: "use-case/a", text: "Acts" }])).toThrow();
  });

  it("parses the lexicon with its tables", () => {
    const lexicon = parseLexicon({
      types: { feature: { label: "Feature", plural: "Features", layer: "intent", question: "What?" } },
      layers: { intent: { label: "Intent", writes: "product people" } },
      links: [{ field: "advances", carriers: "feature", forward: "advances", inverse: "is advanced" }],
      sections: { "Completion Stages": { text: "Stages.", source: "model.md" } },
      glossary: { finding: { term: "Finding", definition: "A check result." } },
      fields: { boundary: "Boundary" },
      findings: { "goal-unused": { level: "G", meaning: "A goal nothing advances." } },
    });
    expect(lexicon.types["feature"]?.label).toBe("Feature");
    expect(() => parseLexicon({ types: {}, layers: {} })).toThrow();
  });

  it("parses the manifest with its project and shard map", () => {
    const manifest = parseManifest({
      project: { id: "product/a", title: "A" },
      roots: { consumer: "C:/shop", litespec: "C:/litespec" },
      editor: "vscode://file/{root}/{path}:{line}",
      totals: { feature: 1 },
      layers: [{ key: "intent", label: "Intent", outline: ["outline-intent"] }],
      shardOf: { "feature/a": "records-a" },
      search: [],
      unplaced: [],
      generatedOn: "2026-01-02T00:00:00.000Z",
    });
    expect(manifest.project.id).toBe("product/a");
    expect(manifest.layers.length).toBe(1);
    expect(() => parseManifest({ project: { id: "product/a", title: "A" } })).toThrow();
  });
});
