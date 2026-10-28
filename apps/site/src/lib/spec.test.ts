// Tests for the standard reader: the slug matches the shared CLI fixture,
// structure inside fenced blocks is ignored, and every page under docs reads
// as the reference reader reads it.
import { describe, it } from "node:test";
import assert from "node:assert";
import fs from "node:fs";
import path from "node:path";
import { pages, parsePageContent, root, slug, type Page, type PageStyle, type Provision, type ProvisionKind } from "./spec.ts";

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

  it("ignores an H3 inside a backtick fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "```markdown", "### Hidden Provision", "```", ""]);
    assert.deepStrictEqual(page.provisions, []);
    assert.deepStrictEqual(page.sections, ["Intent", "Standards"]);
  });

  it("reads an H3 outside a fence as a provision", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Shown Provision", "", "**Statement:** Something MUST hold.", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].heading, "Shown Provision");
    assert.strictEqual(page.provisions[0].id, "litespec/backend/test#shown-provision");
    assert.strictEqual(page.provisions[0].kind, "rule");
  });

  it("ignores an H3 inside a tilde fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "~~~markdown", "### Hidden Provision", "~~~", "", "### Shown Provision", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].heading, "Shown Provision");
  });

  it("reads an H2 outside a fence as a section", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "```markdown", "## Hidden Section", "```", "", "## Standards", ""]);
    assert.deepStrictEqual(page.sections, ["Intent", "Standards"]);
  });

  it("ignores a verification line inside a fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Checked Provision", "", "```markdown", "**Checked by:** `fake` checks nothing.", "```", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].mark, "");
    assert.strictEqual(page.provisions[0].evidence, "");
  });

  it("reads a verification line outside a fence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Checked Provision", "", "**Reviewed for:** Which thing is out of place? Look at the shelf.", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].mark, "guidance");
    assert.strictEqual(page.provisions[0].evidence, "Which thing is out of place? Look at the shelf.");
  });

  it("ignores an H3 outside Standards and Conventions on a topic page", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Concepts", "", "### Defined Term", "", "A definition.", ""]);
    assert.deepStrictEqual(page.provisions, []);
  });

  it("reads an H3 under Conventions as a convention", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Conventions", "", "### Use the Default Location", "", "**Default:** Store the catalog at `docs/ui/patterns.json`.", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].kind, "convention");
  });

  it("keeps inline code backticks in the evidence", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Checked Provision", "", "**Checked by:** `litespec validate` emits no `PAGE_TITLE_COUNT` diagnostic.", ""]);
    assert.strictEqual(page.provisions[0].mark, "checkable");
    assert.strictEqual(page.provisions[0].evidence, "`litespec validate` emits no `PAGE_TITLE_COUNT` diagnostic.");
  });

  it("leaves the mark empty when no verification label opens a paragraph", () => {
    const page = read(["## Intent", "", "Why this page exists.", "", "## Standards", "", "### Plain Provision", "", "**Statement:** Something MUST hold.", ""]);
    assert.strictEqual(page.provisions.length, 1);
    assert.strictEqual(page.provisions[0].mark, "");
    assert.strictEqual(page.provisions[0].evidence, "");
  });
});

// The expected-value reader for the equivalence test: it finds sections,
// provisions and verification lines with line patterns, and the test requires
// parsePageContent to produce the same pages.
const referenceSlash = (value: string) => value.replace(/\\/g, "/");

function referenceStyleOf(rel: string): PageStyle {
  const segments = rel.split("/");
  if (rel.endsWith("/README.md")) return "index";
  if (rel === "docs/reference/glossary.md") return "glossary";
  if (rel.includes("/ext/")) return "extension";
  if (rel.startsWith("docs/frontend/patterns/")) return "pattern";
  if (segments[1] === "tools") return "command";
  if (segments[1] === "reference") return "reference";
  if (segments[1] === "tutorial") return "tutorial";
  if (segments[1] === "guide") return "how-to";
  if (segments[1] === "analyzers") return segments[2] === "rules" && segments.length > 3 ? "reference" : "how-to";
  if (segments[1] === "learn" || segments[1] === "example") return "explanation";
  if (segments.at(-1) === "stack.md") return "stack";
  return "topic";
}

function referenceFirstParagraph(lines: string[], section: string): string {
  const start = lines.findIndex((line) => line.replace(/\s+\{#[^}]+\}\s*$/, "").trim() === `## ${section}`);
  if (start < 0) return "";
  const body: string[] = [];
  for (const line of lines.slice(start + 1)) {
    if (line.startsWith("## ")) break;
    if (line.trim() === "") {
      if (body.length > 0) break;
      continue;
    }
    body.push(line.trim());
  }
  return body.join(" ");
}

function referenceParsePage(rel: string): Page {
  const markdown = fs.readFileSync(path.join(root, rel), "utf8").replace(/\r\n/g, "\n");
  const lines = markdown.split("\n");
  const pagePath = rel.slice("docs/".length, -".md".length);
  const style = referenceStyleOf(rel);
  const sections: string[] = [];
  const provisions: Provision[] = [];
  let section = "";
  let current: Provision | null = null;
  let fenced = false;

  for (const line of lines) {
    if (/^\s*```/.test(line)) {
      fenced = !fenced;
      continue;
    }
    if (fenced) continue;
    const h2 = /^##\s+(.+?)\s*(?:\{#.*\})?\s*$/.exec(line);
    if (h2 && !line.startsWith("###")) {
      section = h2[1].trim();
      sections.push(section);
      current = null;
      continue;
    }
    const h3 = /^###\s+(.+?)\s*$/.exec(line);
    if (h3 && !line.startsWith("####")) {
      const kind: ProvisionKind | null = section === "Standards" ? "rule" : section === "Conventions" ? "convention" : section === "Patterns" && style === "pattern" ? "pattern" : null;
      if (kind && style !== "index" && !["reference", "command", "tutorial", "how-to"].includes(style)) {
        const s = slug(h3[1]);
        current = { id: `litespec/${pagePath}#${s}`, slug: s, heading: h3[1].trim(), kind, mark: "", evidence: "" };
        provisions.push(current);
      } else {
        current = null;
      }
      continue;
    }
    const verify = /^\*\*(Checked by|Reviewed for):\*\*\s*(.*)$/.exec(line);
    if (verify && current) {
      current.mark = verify[1] === "Checked by" ? "checkable" : "guidance";
      current.evidence = verify[2].trim();
    }
  }

  const h1 = lines.find((line) => line.startsWith("# "));
  return {
    file: rel,
    path: pagePath,
    style,
    area: pagePath.split("/")[0],
    title: h1 ? h1.slice(2).trim() : pagePath,
    intent: referenceFirstParagraph(lines, "Intent") || referenceFirstParagraph(lines, "Purpose") || referenceFirstParagraph(lines, "Name"),
    sections,
    provisions,
    markdown,
  };
}

function referenceWalk(dir: string): string[] {
  const out: string[] = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true }).sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0))) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...referenceWalk(full));
    else if (entry.name.endsWith(".md")) out.push(referenceSlash(path.relative(root, full)));
  }
  return out;
}

describe("page reader equivalence", () => {
  it("reads every page under docs/ exactly as the reference reader", () => {
    const files = referenceWalk(path.join(root, "docs"));
    assert.ok(files.length > 0);
    assert.strictEqual(pages.length, files.length);
    for (const file of files) {
      const actual = pages.find((page) => page.file === file);
      assert.ok(actual, `no page parsed for ${file}`);
      assert.deepStrictEqual(actual, referenceParsePage(file), file);
    }
  });
});
