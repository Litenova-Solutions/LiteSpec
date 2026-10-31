// Tests for the search and sharing metadata: title building, description
// trimming, and the schema.org objects every page writes as JSON-LD.
import { describe, it } from "node:test";
import assert from "node:assert";
import {
  absoluteUrl,
  breadcrumbs,
  buildDate,
  metaDescription,
  pageTitle,
  techArticle,
  webPage,
  website,
  softwareSource,
  organization,
} from "./seo.ts";

describe("pageTitle", () => {
  it("joins the page title, its section and LiteSpec under 60 characters", () => {
    const title = pageTitle("Domain", "Backend");
    assert.strictEqual(title, "Domain - Backend - LiteSpec");
    assert.ok(title.length <= 60);
  });

  it("drops the section when the full form would pass 60 characters", () => {
    const title = pageTitle("A Very Long Documentation Page Title That Keeps Going", "Backend");
    assert.strictEqual(title, "A Very Long Documentation Page Title That Keeps Going - LiteSpec");
  });

  it("omits the section segment when no section is given", () => {
    assert.strictEqual(pageTitle("Use with Agents"), "Use with Agents - LiteSpec");
  });
});

describe("metaDescription", () => {
  it("keeps a short description as it is", () => {
    assert.strictEqual(metaDescription("LiteSpec is a standard."), "LiteSpec is a standard.");
  });

  it("trims a long description to at most 160 characters", () => {
    const text = "LiteSpec is a versioned specification standard. ".repeat(10);
    assert.ok(metaDescription(text).length <= 160);
  });
});

describe("absoluteUrl and buildDate", () => {
  it("builds an absolute URL with its trailing slash", () => {
    assert.strictEqual(absoluteUrl("/3.0.0/learn/"), "https://litespec.dev/3.0.0/learn/");
  });

  it("writes the build date as YYYY-MM-DD", () => {
    assert.match(buildDate(), /^\d{4}-\d{2}-\d{2}$/);
  });
});

describe("breadcrumbs", () => {
  it("numbers every crumb and links only the crumbs that have a path", () => {
    const list = breadcrumbs([
      { name: "LiteSpec", path: "/" },
      { name: "Backend", path: null },
      { name: "Domain", path: "/3.0.0/backend/domain/" },
    ]) as { itemListElement: Record<string, unknown>[] };
    assert.strictEqual(list["@type"], "BreadcrumbList");
    assert.deepStrictEqual(
      list.itemListElement.map((item) => item.position),
      [1, 2, 3],
    );
    assert.strictEqual(list.itemListElement[0].item, "https://litespec.dev/");
    assert.strictEqual("item" in list.itemListElement[1], false);
    assert.strictEqual(list.itemListElement[2].item, "https://litespec.dev/3.0.0/backend/domain/");
  });
});

describe("techArticle", () => {
  it("carries the given modified date and names the website it is part of", () => {
    const article = techArticle({
      title: "Domain",
      description: "The domain rules.",
      path: "/3.0.0/backend/domain/",
      modified: "2026-10-31",
      section: "Backend",
    });
    assert.strictEqual(article["@type"], "TechArticle");
    assert.strictEqual(article.headline, "Domain");
    assert.strictEqual(article.dateModified, "2026-10-31");
    assert.strictEqual(article.url, "https://litespec.dev/3.0.0/backend/domain/");
    assert.deepStrictEqual(article.isPartOf, { "@id": "https://litespec.dev/#website" });
  });

  it("falls back to a build date when no git date is given", () => {
    const article = techArticle({
      title: "Domain",
      description: "The domain rules.",
      path: "/3.0.0/backend/domain/",
      section: "Backend",
    });
    assert.match(article.dateModified as string, /^\d{4}-\d{2}-\d{2}$/);
  });
});

describe("webPage and the site objects", () => {
  it("builds a WebPage that is part of the WebSite", () => {
    const page = webPage({ title: "Use with Agents", description: "Set up an agent.", path: "/3.0.0/select/" });
    assert.strictEqual(page["@type"], "WebPage");
    assert.strictEqual(page.url, "https://litespec.dev/3.0.0/select/");
    assert.deepStrictEqual(page.isPartOf, { "@id": "https://litespec.dev/#website" });
  });

  it("names the WebSite, Organization and SoftwareSourceCode types", () => {
    assert.strictEqual(website["@type"], "WebSite");
    assert.strictEqual(organization["@type"], "Organization");
    assert.strictEqual(softwareSource["@type"], "SoftwareSourceCode");
  });
});
