// Tests for the standard reader: the slug matches the shared CLI fixture,
// structure inside fenced blocks is ignored, provisions attach from the
// generated index, and every index entry resolves to a page.
import { describe, it } from "node:test";
import assert from "node:assert";
import fs from "node:fs";
import path from "node:path";
import { appliedPages, pages, parsePageContent, provisions, root, slug, type ProvisionIndexEntry } from "./spec.ts";

describe("slug", () => {
  it("matches the shared CLI fixture for every entry", () => {
    const file = path.join(root, "apps/cli/tests/LiteSpec.Cli.Tests/Authoring/SlugCases.json");
    const cases = JSON.parse(fs.readFileSync(file, "utf8")) as { heading: string; slug: string }[];
    assert.ok(cases.length > 0);
    for (const entry of cases) {
      assert.strictEqual(slug(entry.heading), entry.slug, `slug(${JSON.stringify(entry.heading)})`);
    }
  });
});

describe("parsePageContent", () => {
  const read = (body: string[]) => parsePageContent("docs/backend/test.md", ["# Test Page", "", ...body].join("\n"));

  it("reads the title, the style and the first paragraph of the Intent section", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", ""]);
    assert.strictEqual(page.title, "Test Page");
    assert.strictEqual(page.style, "standard");
    assert.strictEqual(page.intent, "Why this page exists.");
    assert.deepStrictEqual(page.sections, ["Intent", "Standards"]);
  });

  it("ignores an H2 inside a backtick fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "```markdown", "## Hidden Section", "```", "", "## Standards", ""]);
    assert.deepStrictEqual(page.sections, ["Intent", "Standards"]);
  });

  it("ignores an H2 inside a tilde fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "~~~markdown", "## Hidden Section", "~~~", "", "## Standards", ""]);
    assert.deepStrictEqual(page.sections, ["Intent", "Standards"]);
  });

  it("attaches no provisions to a page the index does not list", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Shown Provision", "", "**Statement:** Something MUST hold.", ""]);
    assert.deepStrictEqual(page.provisions, []);
  });
});

describe("record pages", () => {
  it("parses a research page with style research and no provisions", () => {
    const page = parsePageContent("docs/intent/research/sample-study.md", ["# Sample Study", "", "## Answer", "", "What the sources say.", "", "### A Finding", "", "A detail.", ""].join("\n"));
    assert.strictEqual(page.style, "research");
    assert.deepStrictEqual(page.provisions, []);
    assert.deepStrictEqual(page.sections, ["Answer"]);
  });

  it("parses a decision page with style decision and no provisions", () => {
    const page = parsePageContent("docs/intent/decisions/0001-sample-choice.md", ["# Sample Choice", "", "## Context", "", "Why a choice is needed.", "", "## Decision", "", "The selected option.", "", "## Consequences", "", "What follows.", ""].join("\n"));
    assert.strictEqual(page.style, "decision");
    assert.deepStrictEqual(page.provisions, []);
    assert.deepStrictEqual(page.sections, ["Context", "Decision", "Consequences"]);
  });

  it("applies no record page in any selection", () => {
    const files = appliedPages({ shape: "server", stacks: ["dotnet"], extensions: [] });
    assert.ok(files.every((file) => !file.startsWith("docs/intent/") && !file.startsWith("docs/implementation/")));
  });
});

describe("provision index", () => {
  const indexFile = path.join(root, "docs/reference/provisions.json");
  const index = JSON.parse(fs.readFileSync(indexFile, "utf8")) as { version: string; provisions: ProvisionIndexEntry[] };

  it("lists every provision with its kind, mark and strength", () => {
    assert.ok(index.provisions.length > 0);
    for (const entry of index.provisions) {
      assert.match(entry.id, /^litespec\//, entry.id);
      assert.ok(entry.heading.length > 0, entry.id);
      assert.ok(["rule", "convention", "pattern"].includes(entry.kind), entry.id);
      assert.ok(["", "checkable", "guidance"].includes(entry.mark ?? ""), entry.id);
      assert.ok(["", "rule", "default", "advice"].includes(entry.strength ?? ""), entry.id);
    }
  });

  it("resolves every entry to a page that carries it", () => {
    const ids = new Set<string>();
    for (const entry of index.provisions) {
      assert.ok(!ids.has(entry.id), `duplicate ${entry.id}`);
      ids.add(entry.id);
      const page = pages.find((candidate) => candidate.file === entry.page);
      assert.ok(page, `no page parsed for ${entry.page}`);
      assert.ok(page.provisions.some((provision) => provision.id === entry.id), `${entry.id} missing on ${entry.page}`);
      assert.ok(provisions.has(entry.id), `no provision for ${entry.id}`);
    }
  });

  it("carries on each page exactly the entries that name it", () => {
    for (const page of pages) {
      const expected = index.provisions.filter((entry) => entry.page === page.file).map((entry) => entry.id).sort();
      assert.deepStrictEqual(page.provisions.map((provision) => provision.id).sort(), expected, page.file);
    }
  });
});
