import { describe, expect, it } from "vitest";
import { fixtureBook } from "./fixture";
import { duration, editorHref, inline, provisionHref, safeHref, srcHref } from "./html";

describe("links and inline text", () => {
  it("opens sources through the configured editor template", async () => {
    const book = await fixtureBook();
    expect(srcHref(book, { path: ".litespec/intent/brief.yaml", line: 4 })).toBe("vscode://file/C:/work/shop/.litespec/intent/brief.yaml:4");
    expect(srcHref(book, { path: "docs/core/model.md", litespec: true })).toBe("vscode://file/C:/work/litespec/docs/core/model.md");
    expect(provisionHref(book, "litespec/core/model#organize-the-book-by-the-record-layers")).toBe("vscode://file/C:/work/litespec/docs/core/model.md");
  });

  it("writes no source link when the editor is none", async () => {
    const book = await fixtureBook("", { editor: "none" });
    expect(editorHref(book, "C:/work/shop", "a.yaml", 1)).toBeNull();
    expect(srcHref(book, { path: "a.yaml", line: 1 })).toBeNull();
  });

  it("refuses script and protocol-relative links", async () => {
    const book = await fixtureBook();
    expect(safeHref(book, "javascript:alert(1)")).toBeNull();
    expect(safeHref(book, "vbscript:msgbox")).toBeNull();
    expect(safeHref(book, "//evil.example")).toBeNull();
    expect(safeHref(book, "https://litespec.dev")).toBe("https://litespec.dev");
    expect(inline(book, "[click](javascript:alert)")).toBe("click");
  });

  it("links a record named in code and leaves an unknown one as code", async () => {
    const book = await fixtureBook();
    expect(inline(book, "See `feature/checkout`.")).toBe('See <button type="button" class="book-ref" data-go="feature/checkout">Checkout</button>.');
    expect(inline(book, "See `feature/missing`.")).toBe("See <code>feature/missing</code>.");
    expect(inline(book, "<b>x</b>")).toBe("&lt;b&gt;x&lt;/b&gt;");
  });

  it("resolves a relative link against the record's own file", async () => {
    const book = await fixtureBook();
    expect(inline(book, "[terms](../terms.md)", { path: ".litespec/intent/goals/sell.yaml" })).toBe(
      '<a href="vscode://file/C:/work/shop/.litespec/intent/terms.md" title="Open ../terms.md in the editor">terms</a>',
    );
  });

  it("reads an ISO duration in words", () => {
    expect(duration("P2DT1H")).toBe("2 days 1 hour");
    expect(duration("PT30M")).toBe("30 minutes");
  });
});
