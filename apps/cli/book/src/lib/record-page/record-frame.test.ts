import { describe, expect, it } from "vitest";
import { fixtureBook } from "$lib/frame";
import type { BookRecord } from "$lib/frame";
import { fieldRowsFor } from "./type-facts";
import { whyPartsOf } from "./page-prose";

// The record frame: the field table leaves to each type renderer what the
// renderer shows itself, and the Why rows prefer the rendered HTML the CLI
// sends over client-side rendering.
describe("the record frame", () => {
  it("leaves a flow's start and finish to the flow renderer but keeps the served-steps fact", async () => {
    const book = await fixtureBook();
    const r: BookRecord = {
      type: "flow",
      title: "Submit",
      fields: {
        startedWhen: "The employee has paid for a work cost.",
        finishedWhen: "The employee holds the reimbursement.",
        steps: [
          { id: "s1", statement: "The employee submits the expense." },
          { id: "s2", statement: "The manager decides it." },
        ],
      },
      stepServers: { s1: ["use-case/library.borrowing.borrow"] },
    };
    const rows = fieldRowsFor(book, r, {
      parent: null,
      proof: null,
      days: () => null,
      fmtDate: (s) => s ?? "",
      dateHint: () => "",
      labelField: (key) => key,
    });
    const labels = rows.map((row) => row.label);
    expect(labels).not.toContain("Started when");
    expect(labels).not.toContain("Finished when");
    expect(labels).toContain("Serves");
  });

  it("leaves a screen's address, entry point and route to the screen renderer", async () => {
    const book = await fixtureBook();
    const r: BookRecord = {
      type: "screen",
      title: "Borrow",
      fields: { address: "/borrow", entryPoint: "entry-point/web", wireframe: 'page\n  heading "Borrow"' },
      route: { path: "src/routes/borrow.svelte", route: "/borrow" },
      captureState: "route-not-captured",
    };
    const rows = fieldRowsFor(book, r, {
      parent: null,
      proof: null,
      days: () => null,
      fmtDate: (s) => s ?? "",
      dateHint: () => "",
      labelField: (key) => key,
    });
    const labels = rows.map((row) => row.label);
    expect(labels).not.toContain("Address");
    expect(labels).not.toContain("Entry point");
    expect(labels).not.toContain("Route");
    expect(labels).not.toContain("Capture state");
    expect(labels).not.toContain("Wireframe");
  });

  it("prefers the rendered Why HTML the CLI sends over client-side rendering", async () => {
    const book = await fixtureBook();
    const r: BookRecord = {
      type: "decision",
      title: "Queue",
      fields: { rationale: "Bulk work buries approvals." },
      fieldsHtml: { rationale: "<p>Bulk work buries approvals.</p>" },
    };
    expect(whyPartsOf(book, r)).toEqual([{ label: "Rationale", html: "<p>Bulk work buries approvals.</p>" }]);
  });

  it("renders Why rows on the client when the CLI sent none", async () => {
    const book = await fixtureBook();
    const r: BookRecord = {
      type: "decision",
      title: "Queue",
      fields: { rationale: "Bulk work buries approvals." },
    };
    const parts = whyPartsOf(book, r);
    expect(parts).toHaveLength(1);
    expect(parts[0]?.label).toBe("Rationale");
    expect(parts[0]?.html).toContain("Bulk work buries approvals.");
  });
});
