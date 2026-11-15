import { flushSync, mount, unmount, type Component } from "svelte";
import { describe, expect, it } from "vitest";
import { fixtureBook } from "$lib/frame";
import type { BookRecord } from "$lib/frame";
import { TYPES } from "./index";
import NavigationMap from "../shared/NavigationMap.svelte";
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

function renderComponent<Props extends Record<string, unknown>>(Renderer: Component<Props>, props: Props) {
  const target = document.createElement("div");
  const view = mount(Renderer, { target, props });
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

  it("shows a read model's key, freshness and the payload field each row takes from an integration event", async () => {
    await fixtureBook();
    const view = render("read-model", {
      type: "read-model", title: "Loan Borrowing",
      fields: {
        keyedBy: "aggregate/lending.loan",
        freshness: { mode: "eventual", within: "PT5S" },
        removedBy: [],
        fields: [{ name: "loanId", type: "id", from: "integration-event/loan-borrowed#loanId", accumulate: "latest", classification: "internal" }],
      },
    });
    expect(view).toContain("5 seconds");
    expect(view).toContain("#loanId");
    expect(view).toContain("Latest");
  });

  it("renders a flow's start, steps and branches, each branch as a sentence", async () => {
    await fixtureBook();
    const flow = render("flow", {
      type: "flow", title: "Submit",
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
    expect(flow).toContain("The employee has paid for a work cost.");
    expect(flow).toContain("At step 2, when the manager rejects the expense, the flow ends.");
    expect(flow).toContain("At step 1, when the form is incomplete, the flow continues at step 3.");
    expect(flow).not.toContain("betweenSteps");
  });

  it("shows no intro banner on the product page", async () => {
    await fixtureBook();
    const view = render("product", { type: "product", title: "Library", fields: {} });
    expect(view).not.toContain("New here?");
  });

  it("shows aggregate sections without teaching captions", async () => {
    await fixtureBook();
    const view = render("aggregate", {
      type: "aggregate", title: "Loan",
      fields: {
        states: [{ name: "New", meaning: "Fresh." }],
        transitions: [{ from: "-", to: "New" }],
        dataClasses: [{ name: "loan", fields: ["id"], purpose: "Lending.", onErasure: "erase" }],
      },
    });
    expect(view).toContain("New");
    expect(view).not.toContain("what moves the record from state to state");
    expect(view).not.toContain("what is kept, and what happens on erasure");
  });

  it("shows term labels without the language note", async () => {
    await fixtureBook();
    const view = render("term", {
      type: "term", title: "Borrow",
      fields: { term: "borrow", labels: { nl: "lenen" }, rejected: [{ word: "lend", reason: "The library lends nothing." }] },
    });
    expect(view).toContain("lenen");
    expect(view).toContain("lend");
    expect(view).not.toContain("Adding a language changes no other record.");
  });

  it("names the missing page on a screen in one wording", async () => {
    await fixtureBook();
    const view = render("screen", { type: "screen", title: "Borrow", fields: { address: "/borrow" } });
    expect(view).toContain("No page carries this screen.");
    expect(view).not.toContain("No page carries this screen yet");
  });

  it("draws a screen's wireframe and its leadsTo screens", async () => {
    await fixtureBook();
    const view = render("screen", {
      type: "screen", title: "Borrow",
      fields: { address: "/borrow", wireframe: 'page\n  heading "Borrow a book"', leadsTo: ["screen/confirm"] },
    });
    expect(view).toContain("<svg");
    expect(view).toContain("Borrow a book");
    expect(view).toContain("screen/confirm");
  });

  it("shows a short state when a screen carries no wireframe", async () => {
    await fixtureBook();
    const view = render("screen", { type: "screen", title: "Borrow", fields: { address: "/borrow" } });
    expect(view).toContain("No wireframe drawn.");
  });

  it("shows a short state when an entry point has no screens", async () => {
    await fixtureBook();
    const view = render("entry-point", { type: "entry-point", title: "Web", fields: { path: "/app" } });
    expect(view).toContain("/app");
    expect(view).toContain("No screens.");
  });

  it("maps entry point screens as boxes with leadsTo arrows", async () => {
    await fixtureBook();
    const view = renderComponent(NavigationMap, {
      screens: [
        { id: "screen/inbox", title: "Inbox" },
        { id: "screen/detail", title: "Detail" },
      ],
      leads: { "screen/inbox": ["screen/detail"], "screen/detail": [] },
      start: "screen/inbox",
    });
    expect(view).toContain("<svg");
    expect(view).toContain("Inbox");
    expect(view).toContain("Detail");
    expect(view).toContain("Start");
    expect(view).toContain('href="#screen/inbox"');
    expect(view).toContain("url(#nav-arrow)");
  });

  it("draws a flow's storyboard from its steps' screens in step order", async () => {
    await fixtureBook();
    const view = render("flow", {
      type: "flow", title: "Submit",
      fields: { steps: [{ id: "s1", statement: "The employee submits the expense." }] },
      stepScreens: { s1: ["screen/submit"] },
    });
    expect(view).toContain("Storyboard");
    expect(view).toContain("Step 1");
    expect(view).toContain("The employee submits the expense.");
    expect(view).toContain("screen/submit");
    expect(view).toContain("No wireframe drawn.");
  });

  it("shows no hidden link blocks on a use case", async () => {
    await fixtureBook();
    const view = render("use-case", { type: "use-case", title: "Borrow", fields: {} });
    expect(view).not.toContain("Points at this");
  });
});
