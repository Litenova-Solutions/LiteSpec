// Tests for the standard reader: the slug matches the shared CLI fixture, the page
// index covers every docs page and every shipped stack and extension, every selection
// names only indexed pages, and provisions attach from the generated index.
import { describe, it } from "node:test";
import assert from "node:assert";
import fs from "node:fs";
import path from "node:path";
import { appliedPages, backendStacks, extensionStack, frontendStacks, layers, manifest, pages, provisions, root, selectionKey, selectionLayers, selections, slug, standardUrl, version, type ProvisionIndexEntry } from "./spec.ts";

describe("slug", () => {
  it("matches the shared CLI fixture for every entry", () => {
    const file = path.join(root, "apps/cli/tests/LiteSpec.Cli.Tests/Standards/SlugCases.json");
    const cases = JSON.parse(fs.readFileSync(file, "utf8")) as { heading: string; slug: string }[];
    assert.ok(cases.length > 0);
    for (const entry of cases) {
      assert.strictEqual(slug(entry.heading), entry.slug, `slug(${JSON.stringify(entry.heading)})`);
    }
  });
});

/** Every Markdown file under docs/, relative to the repository root. */
function docsFiles(dir: string): string[] {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return docsFiles(full);
    return entry.name.endsWith(".md") ? [path.relative(root, full).split(path.sep).join("/")] : [];
  });
}

describe("page index", () => {
  it("lists every docs page exactly once", () => {
    const onDisk = docsFiles(path.join(root, "docs")).sort();
    const indexed = pages.map((page) => page.file).sort();
    assert.deepStrictEqual(indexed, onDisk);
  });

  it("gives every page a title and a path under docs", () => {
    for (const page of pages) {
      assert.ok(page.title.length > 0, page.file);
      assert.strictEqual(page.file, `docs/${page.path}.md`, page.file);
      assert.strictEqual(page.area, page.path.split("/")[0], page.file);
    }
  });

  it("lists every stack and extension the manifest ships", () => {
    assert.deepStrictEqual([...backendStacks, ...frontendStacks].sort(), Object.keys(manifest.stacks).sort());
    for (const id of Object.keys(manifest.extensions)) {
      const owner = extensionStack(id);
      assert.ok(owner === null || owner in manifest.stacks, `${id} supports ${owner}`);
      assert.ok(layers().some((layer) => layer.key === `ext/${id}`), `no layer for extension ${id}`);
    }
  });

  it("applies no record page in any selection", () => {
    for (const selection of selections()) {
      assert.ok(appliedPages(selection).every((file) => !file.startsWith("docs/intent/") && !file.startsWith("docs/implementation/")), selectionKey(selection));
    }
  });

  it("serves the generated Standard overview under the version", () => {
    assert.strictEqual(standardUrl(), `/${version}/standard/`);
  });

  it("names only indexed pages and layers in every selection", () => {
    const files = new Set(pages.map((page) => page.file));
    const keys = new Set(layers().map((layer) => layer.key));
    for (const selection of selections()) {
      assert.strictEqual(selectionKey(selection), [selection.shape, ...selection.stacks].join("."));
      for (const file of appliedPages(selection)) assert.ok(files.has(file), `${selectionKey(selection)} applies ${file}`);
      for (const key of selectionLayers(selection)) assert.ok(keys.has(key), `${selectionKey(selection)} lists layer ${key}`);
    }
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
      assert.ok(["", "required", "default", "advisory"].includes(entry.strength ?? ""), entry.id);
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
