import { describe, expect, it } from "vitest";
import { parseWireframe } from "./parse";
import { renderWireframe } from "./render";
import goldenWireframe from "./fixtures/record-header.wireframe?raw";
import goldenDesktop from "./fixtures/record-header.desktop.svg?raw";
import errorsJson from "./fixtures/errors.json?raw";

// The golden drawing and the rejected blocks, ported with the renderer.
describe("the wireframe renderer", () => {
  it("draws the checked-in desktop SVG byte for byte", () => {
    const parsed = parseWireframe(goldenWireframe);
    expect(parsed.errors).toEqual([]);
    expect(parsed.page).not.toBeNull();
    if (parsed.page) expect(renderWireframe(parsed.page)).toBe(goldenDesktop);
  });

  it("rejects every invalid block", () => {
    const cases: Record<string, string> = JSON.parse(errorsJson);
    for (const [name, text] of Object.entries(cases)) {
      const parsed = parseWireframe(text);
      expect(parsed.page, name).toBeNull();
      expect(parsed.errors.length, name).toBeGreaterThan(0);
    }
  });

  it("draws the copy of a small wireframe", () => {
    const parsed = parseWireframe('page\n  heading "Borrow a book"\n  action primary "Confirm"');
    expect(parsed.errors).toEqual([]);
    if (!parsed.page) throw new Error("expected a page");
    const svg = renderWireframe(parsed.page);
    expect(svg).toContain("Borrow a book");
    expect(svg).toContain("Confirm");
    expect(svg.startsWith("<svg")).toBe(true);
  });
});
