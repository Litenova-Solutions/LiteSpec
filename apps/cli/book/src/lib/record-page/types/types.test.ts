import { flushSync, mount, unmount } from "svelte";
import { describe, expect, it } from "vitest";
import { fixtureBook } from "$lib/frame";
import type { BookRecord } from "$lib/frame";
import { TYPES } from "./index";
import recordTypesSource from "../../../../../../model/src/LiteSpec.Model/Records/RecordTypes.cs?raw";

// The written record types, read from the model library that defines them.
function recordTypes() {
  const block = /HashSet<string> All = new HashSet<string>\(StringComparer\.Ordinal\)\s*\{([^}]*)\}/.exec(recordTypesSource);
  return [...(block?.[1] ?? "").matchAll(/"([a-z-]+)"/g)].map((m) => m[1]);
}

function render(type: string, r: BookRecord) {
  const target = document.createElement("div");
  const view = mount(TYPES[type], { target, props: { id: type + "/x", r } });
  flushSync();
  const html = target.innerHTML;
  unmount(view);
  return html;
}

describe("the record page", () => {
  it("has a renderer for every written record type", () => {
    const types = recordTypes();
    expect(types.length).toBeGreaterThan(20);
    expect(types.filter((t) => !TYPES[t])).toEqual([]);
  });

  it("links research sources and data schemas only through the safe link rule", async () => {
    await fixtureBook();
    const research = render("research", {
      type: "research", title: "Sources",
      fields: { sources: [{ key: "a", title: "Bad", url: "javascript:alert(1)" }, { key: "b", title: "Good", url: "https://litespec.dev/x" }] },
    });
    expect(research).not.toContain('href="javascript:');
    expect(research).toContain('href="https://litespec.dev/x"');

    const event = render("integration-event", { type: "integration-event", title: "Paid", fields: { dataschema: "javascript:alert(1)" } });
    expect(event).toContain("javascript:alert(1)");
    expect(event).not.toContain('href="javascript:');
  });
});
