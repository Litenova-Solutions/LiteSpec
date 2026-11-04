// Tests for the trail fence: the outline parses into nested nodes and facets,
// an unreadable line is reported, and each node draws with its type.
import { describe, it } from "node:test";
import assert from "node:assert";
import { parseTrail, renderTrail } from "./trail.ts";

const outline = [
  "use-case       Submit expense (command)",
  "  result       Submitted, Receipt missing",
  "    scenario   Submitting with a receipt succeeds",
  "code           Aggregate, command, endpoint, acceptance test",
  "facet Where it lives  spending module > expense aggregate",
].join("\n");

describe("trail", () => {
  it("nests nodes by indentation", () => {
    const { trail, errors } = parseTrail(outline);
    assert.deepStrictEqual(errors, []);
    assert.strictEqual(trail.items.length, 2);
    const [useCase, code] = trail.items;
    assert.strictEqual(useCase.type, "use-case");
    assert.strictEqual(useCase.children[0].type, "result");
    assert.strictEqual(useCase.children[0].children[0].type, "scenario");
    assert.strictEqual(code.text, "Aggregate, command, endpoint, acceptance test");
  });

  it("reads facets as a label and a path", () => {
    const { trail } = parseTrail(outline);
    assert.deepStrictEqual(trail.facets, [{ label: "Where it lives", path: ["spending module", "expense aggregate"] }]);
  });

  it("reports a line it cannot read", () => {
    const { errors } = parseTrail("product Claimwick");
    assert.strictEqual(errors.length, 1);
  });

  it("reports a review line", () => {
    const { trail, errors } = parseTrail("--- Review line: records above, code below");
    assert.strictEqual(errors.length, 1);
    assert.deepStrictEqual(trail.items, []);
  });

  it("draws each node with its type and colors it by group", () => {
    const { trail } = parseTrail(outline);
    const html = renderTrail(trail);
    assert.match(html, /<li class="trail-node trail-behavior"><div class="trail-row"><span class="trail-type">Use case<\/span>/);
    assert.match(html, /<li class="trail-node trail-build">/);
  });
});
