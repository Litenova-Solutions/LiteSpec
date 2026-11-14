import { flushSync, mount, unmount } from "svelte";
import { describe, expect, it } from "vitest";
import { fixtureBook } from "$lib/frame";
import type { BookRecord } from "$lib/frame";
import { TYPES } from "./index";
import recordTypesSource from "../../../../../../model/src/LiteSpec.Model/Records/RecordTypes.cs?raw";

// The written record types, read from the model library that defines them.
function recordTypes() {
  const block = /HashSet<string> All = new HashSet<string>\(StringComparer\.Ordinal\)\s*\{([^}]*)\}/.exec(recordTypesSource);
  return [...(block?.[1] ?? "").matchAll(/"([a-z-]+)"/g)].map((m) => m[1]).filter((t): t is string => !!t);
}

function render(type: string, r: BookRecord) {
  const target = document.createElement("div");
  const Renderer = TYPES[type];
  if (!Renderer) throw new Error("no renderer for " + type);
  const view = mount(Renderer, { target, props: { id: type + "/x", r } });
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

  it("renders a journey's start, steps and branches, each branch as a sentence", async () => {
    await fixtureBook();
    const journey = render("journey", {
      type: "journey", title: "Submit",
      fields: {
        startedWhen: "The employee has paid for a work cost.",
        finishedWhen: "The employee holds the reimbursement.",
        steps: [{ id: "s1", statement: "The employee submits the expense." }, { id: "s2", statement: "The manager decides it." }, { id: "s3", statement: "Finance pays it." }],
        branches: [
          { step: "s2", condition: "the manager rejects the expense", nextStep: "end" },
          { step: "s1", condition: "the form is incomplete", nextStep: "s3" },
        ],
      },
    });
    expect(journey).toContain("The employee has paid for a work cost.");
    expect(journey).toContain("At step 2, when the manager rejects the expense, the journey ends.");
    expect(journey).toContain("At step 1, when the form is incomplete, the journey continues at step 3.");
    expect(journey).not.toContain("betweenSteps");
  });
});
