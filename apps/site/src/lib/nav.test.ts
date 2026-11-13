// Tests for the docs navigation: the README list order, the per-folder page
// and folder order with their fallbacks, the breadcrumb trail, the pager
// neighbors, and the reading time.
import { describe, it } from "node:test";
import assert from "node:assert";
import { groups } from "./areas.ts";
import {
  crumbTrail,
  folderOrder,
  groupHomeUrl,
  groupPages,
  pageNeighbors,
  pageOrder,
  readmeLinkOrder,
  readingTime,
} from "./nav.ts";
import { pageByFile, pageUrl, version, type Page } from "./spec.ts";

describe("readmeLinkOrder", () => {
  it("reads list-item links in order, first occurrence of each", () => {
    const markdown = [
      "# Overview",
      "",
      "See [Setup](setup.md) first.",
      "",
      "- [System](system.md): the parts.",
      "- [Scope](scope.md): the shape.",
      "- [System again](system.md): repeated.",
    ].join("\n");
    assert.deepStrictEqual(readmeLinkOrder(markdown), ["system.md", "scope.md"]);
  });

  it("strips anchors and skips external, absolute and non-Markdown targets", () => {
    const markdown = [
      "- [Scoped](scope.md#layer-claims): an anchor.",
      "- [Site](https://example.com/page.md): external.",
      "- [Root](/docs/core/scope.md): absolute.",
      "- [Image](./diagram.svg): not Markdown.",
    ].join("\n");
    assert.deepStrictEqual(readmeLinkOrder(markdown), ["scope.md"]);
  });
});

describe("pageOrder", () => {
  it("follows docs/core/README.md after its overview", () => {
    const files = pageOrder("docs/core").map((page) => page.file);
    assert.strictEqual(files[0], "docs/core/README.md");
    assert.deepStrictEqual(
      files.slice(1).map((file) => file.slice("docs/core/".length)),
      [
        "system.md",
        "scope.md",
        "stacks.md",
        "scopes.md",
        "principles.md",
        "model.md",
        "departures.md",
        "layers.md",
        "intent.md",
        "behavior.md",
        "domain-model.md",
        "use-cases.md",
        "automation.md",
        "edges.md",
        "privacy.md",
        "access.md",
        "agent.md",
        "authoring.md",
        "language.md",
        "writing.md",
        "maintaining.md",
        "release.md",
      ],
    );
  });

  it("lists pages the README does not link after, by title", () => {
    const files = pageOrder("docs/frontend/web").map((page) => page.file);
    assert.strictEqual(files[0], "docs/frontend/web/README.md");
    assert.strictEqual(files[files.length - 1], "docs/frontend/web/patterns.md");
  });

  it("keeps path order in a folder with no README", () => {
    assert.deepStrictEqual(
      pageOrder("docs/tutorial").map((page) => page.file),
      ["docs/tutorial/getting-started.md", "docs/tutorial/model-domain.md"],
    );
  });
});

describe("folderOrder", () => {
  it("follows the parent README lists, where patterns.md comes before web/README.md", () => {
    assert.deepStrictEqual(folderOrder("docs/frontend"), [
      "docs/frontend/patterns",
      "docs/frontend/web",
      "docs/frontend/mobile",
      "docs/frontend/blazor",
    ]);
  });

  it("lists the single subfolder of backend and analyzers", () => {
    assert.deepStrictEqual(folderOrder("docs/backend"), ["docs/backend/dotnet"]);
    assert.deepStrictEqual(folderOrder("docs/analyzers"), ["docs/analyzers/rules"]);
  });
});

describe("groupHomeUrl", () => {
  it("points each group at its overview", () => {
    const home = new Map(groups.map((group) => [group.key, groupHomeUrl(group)]));
    assert.strictEqual(home.get("learn"), `/${version}/learn/`);
    assert.strictEqual(home.get("tutorial"), `/${version}/tutorial/getting-started/`);
    assert.strictEqual(home.get("guides"), `/${version}/guide/`);
    assert.strictEqual(home.get("standard"), `/${version}/standard/`);
    assert.strictEqual(home.get("reference"), `/${version}/reference/`);
  });
});

describe("crumbTrail", () => {
  it("links the group and area overviews of a standards page", () => {
    const page = pageByFile("docs/backend/domain.md");
    assert.deepStrictEqual(crumbTrail(page!), [
      { name: "Standard", url: `/${version}/standard/` },
      { name: "Backend", url: `/${version}/backend/` },
    ]);
  });

  it("collapses the group and area into one crumb on a group home", () => {
    const page = pageByFile("docs/learn/README.md");
    assert.deepStrictEqual(crumbTrail(page!), [{ name: "Learn", url: null }]);
  });

  it("names the worked example folder inside Learn", () => {
    const page = pageByFile("docs/example/the-product.md");
    assert.deepStrictEqual(crumbTrail(page!), [
      { name: "Learn", url: `/${version}/learn/` },
      { name: "Worked Example: Claimwick", url: `/${version}/example/` },
    ]);
  });

  it("links the first tutorial page from the second", () => {
    const page = pageByFile("docs/tutorial/model-domain.md");
    assert.deepStrictEqual(crumbTrail(page!), [{ name: "Tutorial", url: `/${version}/tutorial/getting-started/` }]);
  });

  it("walks every folder between the area and the page", () => {
    const page = pageByFile("docs/analyzers/rules/ls1001.md");
    assert.deepStrictEqual(crumbTrail(page!), [
      { name: "Reference", url: `/${version}/reference/` },
      { name: "Analyzers", url: `/${version}/analyzers/` },
      { name: "Rules", url: `/${version}/analyzers/rules/` },
    ]);
  });

  it("keeps pages outside the groups under their area label", () => {
    const page = pageByFile("docs/intent/decisions/0001-keep-release-reasoning-as-records/0001-keep-release-reasoning-as-records.md");
    assert.deepStrictEqual(crumbTrail(page!), [
      { name: "Research and Decisions", url: `/${version}/intent/` },
      { name: "Decisions", url: null },
    ]);
  });
});

describe("pageNeighbors", () => {
  it("walks the reading order within one group", () => {
    const page = pageByFile("docs/core/system.md");
    const { prev, next, nextGroup } = pageNeighbors(page!);
    assert.strictEqual(prev!.file, "docs/core/README.md");
    assert.strictEqual(next!.file, "docs/core/scope.md");
    assert.strictEqual(nextGroup, null);
  });

  it("starts a group with no previous page", () => {
    const page = pageByFile("docs/learn/README.md");
    const { prev, next } = pageNeighbors(page!);
    assert.strictEqual(prev, null);
    assert.strictEqual(next!.file, "docs/learn/why-litespec.md");
  });

  it("ends a group at the next group's overview labelled with its name", () => {
    const learn = groups.find((group) => group.key === "learn")!;
    const last = groupPages(learn).at(-1)!;
    const { prev, next, nextGroup } = pageNeighbors(last);
    assert.strictEqual(last.file, "docs/example/model-and-checks.md");
    assert.ok(prev);
    assert.strictEqual(next, null);
    assert.deepStrictEqual(nextGroup, { url: `/${version}/tutorial/getting-started/`, label: "Tutorial" });
  });

  it("ends the last group with no next page", () => {
    const reference = groups.find((group) => group.key === "reference")!;
    const last = groupPages(reference).at(-1)!;
    const { next, nextGroup } = pageNeighbors(last);
    assert.strictEqual(next, null);
    assert.strictEqual(nextGroup, null);
  });
});

describe("readingTime", () => {
  const page = (words: number): Page => ({ markdown: "word ".repeat(words).trim() } as Page);

  it("counts minutes at 220 words a minute, at least one", () => {
    assert.strictEqual(readingTime(page(30)), 1);
    assert.strictEqual(readingTime(page(220)), 1);
    assert.strictEqual(readingTime(page(440)), 2);
    assert.strictEqual(readingTime(page(0)), 1);
  });

  it("agrees with the select page rule on a real page", () => {
    const learn = pageByFile("docs/learn/why-litespec.md")!;
    assert.strictEqual(readingTime(learn), Math.max(1, Math.round(learn.markdown.split(/\s+/).length / 220)));
    assert.strictEqual(pageUrl(learn), `/${version}/learn/why-litespec/`);
  });
});
