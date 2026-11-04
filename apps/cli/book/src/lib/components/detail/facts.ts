// The extra facts each type shows in the field table. book.js collected them while
// the type renderer ran; here each type has a pure function, so the field table
// reads them without running the renderer. The keys and words match book.js.

import { base, book, cap, PLABEL, plural } from "$lib/book";
import type { BookRecord, Fields } from "$lib/book/types";

export interface Fact {
  key: string;
  text: string;
}

function testedFact(r: BookRecord): Fact | null {
  const m = r.machine as Fields | null;
  if (!m || m.tested === undefined) return null;
  return { key: "Tested", text: m.tested ? "Tested" : "Not yet tested" };
}

function proofFact(state: unknown): Fact | null {
  if (!state) return null;
  return { key: "Proof", text: cap(PLABEL[state as keyof typeof PLABEL] ?? state) };
}

const facts: Record<string, (id: string, r: BookRecord, f: Fields) => Fact[]> = {
  feature: () => [],
  requirement: (_id, r, f) => {
    const out: Fact[] = [];
    if (f.kind) out.push({ key: "Kind", text: cap(f.kind) });
    out.push({ key: "Met", text: (r.machine as Fields | null)?.met ? "Met" : "Not yet met" });
    const t = testedFact(r);
    if (t) out.push(t);
    return out;
  },
  decision: (_id, r) => {
    const t = testedFact(r);
    return t ? [t] : [];
  },
  constraint: (_id, _r, f) => {
    const out: Fact[] = [];
    if (f.kind) out.push({ key: "Kind", text: cap(f.kind) });
    out.push({ key: "Applies", text: f.applies ? "True" : "False" });
    return out;
  },
  journey: (_id, r, f) => {
    const steps = (f.steps as Fields[]) ?? [];
    const srv = (r.stepServers as Record<string, string[]>) ?? {};
    const served = steps.filter((s) => (srv[s.id] ?? []).length).length;
    return [{ key: "Serves", text: `${served} of ${steps.length} steps served` }];
  },
  actor: (_id, _r, f) => (f.kind ? [{ key: "Kind", text: cap(f.kind) }] : []),
  aggregate: (_id, r, f) => {
    const out: Fact[] = [{ key: "Kind", text: cap(f.kind ?? "entity") }];
    const states = (f.states as Fields[]) ?? [];
    if (states.length) {
      const reach = (r.reachable as string[]) ?? [];
      const starts = new Set(
        ((f.transitions as Fields[]) ?? []).filter((t) => t.from === "-").map((t) => String(t.to)),
      );
      const unreached = states.filter((s) => reach.length && !reach.includes(s.name) && !starts.has(s.name)).length;
      out.push({ key: "States", text: unreached ? plural(unreached, "state") + " never reached" : "Every state is reached" });
    }
    return out;
  },
  rule: (_id, r, f) => {
    const out: Fact[] = [];
    if (f.kind) out.push({ key: "Kind", text: cap(f.kind) });
    const p = proofFact(r.proof?.state);
    if (p) out.push(p);
    return out;
  },
  "use-case": (id, _r, f) => {
    const out: Fact[] = [];
    if (f.kind) out.push({ key: "Kind", text: cap(f.kind) });
    const p = proofFact(book.proofState(id));
    if (p) out.push(p);
    return out;
  },
  reaction: () => [{ key: "Kind", text: "Reaction" }],
  job: (_id, _r, f) => [{ key: "Kind", text: cap(f.kind ?? "job") }],
  "integration-event": (_id, r, f) => {
    const out: Fact[] = [{ key: "Direction", text: f.direction === "in" ? "Arrives from outside" : "Leaves the system" }];
    const t = testedFact(r);
    if (t) out.push(t);
    return out;
  },
  "external-system": (_id, r) => {
    const t = testedFact(r);
    return t ? [t] : [];
  },
  role: (_id, _r, f) => [{ key: "Kind", text: "Scope: " + cap(f.scope) }],
  "entry-point": (_id, _r, f) => (f.kind ? [{ key: "Kind", text: cap(f.kind) }] : []),
  exception: (_id, _r, f) => {
    const out: Fact[] = [];
    if (f.kind) out.push({ key: "Kind", text: cap(f.kind) });
    const n = book.days(String(f.expiresOn ?? ""));
    out.push({
      key: "Expires",
      text: n !== null && n < 0 ? "Expired " + plural(-n, "day") + " ago" : `Expires ${book.fmtDate(f.expiresOn)}, ${book.dateHint(String(f.expiresOn ?? ""))}`,
    });
    return out;
  },
  scenario: (_id, r) => {
    const p = proofFact(r.proof?.state);
    return p ? [p] : [];
  },
  result: (_id, _r, f) => (f.outcome ? [{ key: "Outcome", text: cap(f.outcome) }] : []),
  "finding-code": (_id, _r, f) => {
    const level = f.level ?? f.tier;
    return level ? [{ key: "Check level", text: "Level " + level }] : [];
  },
  finding: (_id, _r, f) => {
    const out: Fact[] = [];
    const level = f.level ?? f.tier;
    if (level) out.push({ key: "Check level", text: "Level " + level });
    if (f.known) out.push({ key: "Kind", text: "Excepted" });
    if (f.outcome) out.push({ key: "Kind", text: cap(f.outcome) });
    if (f.reason) out.push({ key: "Kind", text: cap(f.reason) });
    return out;
  },
  code: (_id, _r, f) => ((f.kinds as string[]) ?? []).filter(Boolean).map((k) => ({ key: "Kind", text: cap(k) })),
  route: (_id, _r, f) => (f.frontend ? [{ key: "Kind", text: cap(f.frontend) }] : []),
  test: (_id, _r, f) => {
    const out: Fact[] = [];
    if (f.browser) out.push({ key: "Kind", text: "Browser test" });
    out.push({ key: "Kind", text: f.confirmed ? "Confirmed" : "Not confirmed" });
    if (f.passing === null || f.passing === undefined) out.push({ key: "Kind", text: "Passing: not run" });
    else out.push(f.passing ? { key: "Proof", text: "Passing" } : { key: "Proof", text: "Failing" });
    return out;
  },
};

/** The extra facts of one record for the field table, first key winning. */
export function factsFor(type: string, id: string, r: BookRecord): Fact[] {
  const fn = facts[type];
  if (!fn) return [];
  return fn(base(id), r, r.fields ?? {});
}
