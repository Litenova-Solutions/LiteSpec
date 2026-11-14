// The extra facts each type shows in the field table, and the table's rows:
// the type's relations and facts, then the plain fields. The keys and words
// match book.js.

import { capitalize, esc, formatLabel, pluralize, proofLabel, stripStep } from "$lib/frame";
import type { Book, BookRecord, ProofState, RecordFields } from "$lib/frame";
import { codeCls, inlineRefHtml } from "./page-html";
import { aggregateFields, journeyFields, routeFileOf, stringField, stringList, teamValue } from "./page-values";

export interface Fact {
  key: string;
  text: string;
}

/** What the facts read from the book: the record's proof state and the book's dates. */
export interface FactEnv {
  proof: ProofState | null;
  days(d: string): number | null;
  fmtDate(s: string | null | undefined): string;
  dateHint(s: string): string;
}

function testedFact(r: BookRecord): Fact | null {
  const tested = r.machine?.tested;
  if (tested === undefined) return null;
  return { key: "Tested", text: tested ? "Tested" : "Not yet tested" };
}

function proofFact(state: ProofState | null | undefined): Fact | null {
  if (!state) return null;
  return { key: "Proof", text: proofLabel(state) };
}

function kindFact(f: RecordFields): Fact | null {
  const kind = stringField(f, "kind");
  return kind ? { key: "Kind", text: capitalize(kind) } : null;
}

type FactFn = (f: RecordFields, r: BookRecord, env: FactEnv) => Fact[];

const facts: Record<string, FactFn> = {
  feature: () => [],
  requirement: (f, r) => {
    const out: Fact[] = [];
    const kind = kindFact(f);
    if (kind) out.push(kind);
    out.push({ key: "Met", text: r.machine?.met ? "Met" : "Not yet met" });
    const t = testedFact(r);
    if (t) out.push(t);
    return out;
  },
  decision: (f, r) => {
    const out: Fact[] = [];
    const kind = kindFact(f);
    if (kind) out.push(kind);
    const t = testedFact(r);
    if (t) out.push(t);
    return out;
  },
  constraint: (f, _r, env) => {
    const out: Fact[] = [];
    const kind = kindFact(f);
    if (kind) out.push(kind);
    out.push({ key: "Applies", text: f.applies ? "True" : "False" });
    const from = stringField(f, "effectiveFrom");
    if (from) out.push({ key: "Effective from", text: env.fmtDate(from) });
    return out;
  },
  journey: (f, r) => {
    const steps = journeyFields(f).steps;
    const srv: Record<string, string[]> = r.stepServers ?? {};
    const served = steps.filter((s) => (srv[String(s.id ?? "")] ?? []).length).length;
    return [{ key: "Serves", text: `${served} of ${steps.length} steps served` }];
  },
  actor: (f) => {
    const kind = kindFact(f);
    return kind ? [kind] : [];
  },
  aggregate: (f, r) => {
    const af = aggregateFields(f);
    const out: Fact[] = [{ key: "Kind", text: capitalize(stringField(f, "kind") ?? "entity") }];
    if (af.states.length) {
      const reach = r.reachable ?? [];
      const starts = new Set(af.transitions.filter((t) => t.from === "-").map((t) => String(t.to)));
      const unreached = af.states.filter((s) => {
        const name = String(s.name ?? "");
        return reach.length > 0 && !reach.includes(name) && !starts.has(String(s.name));
      }).length;
      out.push({ key: "States", text: unreached ? pluralize(unreached, "state") + " never reached" : "Every state is reached" });
    }
    return out;
  },
  rule: (f, r) => {
    const out: Fact[] = [];
    const kind = kindFact(f);
    if (kind) out.push(kind);
    const p = proofFact(r.proof?.state);
    if (p) out.push(p);
    return out;
  },
  "use-case": (f, _r, env) => {
    const out: Fact[] = [];
    const kind = kindFact(f);
    if (kind) out.push(kind);
    const p = proofFact(env.proof);
    if (p) out.push(p);
    return out;
  },
  reaction: () => [{ key: "Kind", text: "Reaction" }],
  job: (f) => [{ key: "Kind", text: capitalize(stringField(f, "kind") ?? "job") }],
  "integration-event": (f, r) => {
    const out: Fact[] = [{ key: "Direction", text: f.direction === "in" ? "Arrives from outside" : "Leaves the system" }];
    const t = testedFact(r);
    if (t) out.push(t);
    return out;
  },
  "external-system": (_f, r) => {
    const t = testedFact(r);
    return t ? [t] : [];
  },
  role: (f) => [{ key: "Kind", text: "Scope: " + capitalize(f.scope) }],
  "entry-point": (f) => {
    const kind = kindFact(f);
    return kind ? [kind] : [];
  },
  departure: (f, _r, env) => {
    const expiresOn = stringField(f, "expiresOn");
    if (!expiresOn) return [{ key: "Expires", text: "Lasting, no expiry date" }];
    const n = env.days(expiresOn);
    return [
      {
        key: "Expires",
        text: n !== null && n < 0 ? "Expired " + pluralize(-n, "day") + " ago" : `Expires ${env.fmtDate(expiresOn)}, ${env.dateHint(expiresOn)}`,
      },
    ];
  },
  scenario: (_f, r) => {
    const p = proofFact(r.proof?.state);
    return p ? [p] : [];
  },
  result: (f) => (f.outcome ? [{ key: "Outcome", text: capitalize(f.outcome) }] : []),
  "finding-code": (f) => {
    const level = f.level ?? f.tier;
    return level ? [{ key: "Check level", text: "Level " + String(level) }] : [];
  },
  finding: (f) => {
    const out: Fact[] = [];
    const level = f.level ?? f.tier;
    if (level) out.push({ key: "Check level", text: "Level " + String(level) });
    if (f.covered) out.push({ key: "Kind", text: "Covered" });
    if (f.outcome) out.push({ key: "Kind", text: capitalize(f.outcome) });
    if (f.reason) out.push({ key: "Kind", text: capitalize(f.reason) });
    return out;
  },
  code: (f) => stringList(f, "kinds").filter(Boolean).map((k) => ({ key: "Kind", text: capitalize(k) })),
  route: (f) => {
    const frontend = stringField(f, "frontend");
    return frontend ? [{ key: "Kind", text: capitalize(frontend) }] : [];
  },
  test: (f) => {
    const out: Fact[] = [];
    if (f.browser) out.push({ key: "Kind", text: "Browser test" });
    out.push({ key: "Kind", text: f.confirmed ? "Confirmed" : "Not confirmed" });
    if (f.passing === null || f.passing === undefined) out.push({ key: "Kind", text: "Passing: not run" });
    else out.push(f.passing ? { key: "Proof", text: "Passing" } : { key: "Proof", text: "Failing" });
    return out;
  },
};

/** The extra facts of one record for the field table, first key winning. */
export function factsFor(type: string, r: BookRecord, env: FactEnv): Fact[] {
  const fn = facts[type];
  if (!fn) return [];
  return fn(r.fields ?? {}, r, env);
}

/** One row of the field table. A route row renders a source link in the table. */
export interface FieldRow {
  label: string;
  html: string;
  route?: boolean;
}

/** What the field table's rows read from the book: the facts' env, the parent, and the field labels. */
export interface FieldTableEnv extends FactEnv {
  parent: string | null;
  labelField(key: string): string;
}

/** The fields the table never shows as plain rows: prose, relations, and record care. */
export const NON_TABLE_FIELDS: ReadonlySet<string> = new Set([
  "statement", "summary", "description", "offering", "meaning", "situation", "whyItMatters",
  "purpose", "whatHappens", "affects", "affectsSegment", "actor", "advances",
  "serves", "risk", "costToday", "workaroundToday", "finishedWhen",
  "address", "host", "entryPoint", "floorplan",
  "questions", "evidence", "lifecycle", "plannedFor", "refusedBecause", "provenance",
  "decider", "decideBy", "owner", "approvedBy", "approvedOn", "approvedHash", "reviewBy",
  // A scenario shows these in its own Given, When, Then and Result sections.
  "given", "when", "then", "result",
]);

/** The field table's rows: the type's relations and facts, then the plain fields. */
export function fieldRowsFor(book: Book, r: BookRecord, env: FieldTableEnv): FieldRow[] {
  const out: FieldRow[] = [];
  const add = (label: string, value: string) => {
    if (value !== "") out.push({ label, html: value });
  };
  const linkList = (list: unknown[]) => list.map((x) => inlineRefHtml(book, stripStep(String(x)))).join(", ");
  const f: RecordFields = r.fields ?? {};
  const flat = (v: unknown): unknown[] => (Array.isArray(v) ? v : [v]);
  if (f.affects) add("Affects", linkList(flat(f.affects)));
  if (f.affectsSegment) add("Affects segment", esc(f.affectsSegment));
  if (f.actor) add("Actor", inlineRefHtml(book, stripStep(String(f.actor))));
  if (r.type === "actor") {
    const kind = stringField(f, "kind");
    if (kind) add("Kind", esc(capitalize(kind)));
    const team = teamValue(f);
    if (team !== undefined) add("Team", team ? "Yes" : "No");
  }

  if (f.advances) add("Advances", linkList(flat(f.advances)));
  if (f.serves) add("Serves", linkList(flat(f.serves).map((x) => String(x).split("#")[0])));
  if (env.parent) add("Parent", inlineRefHtml(book, env.parent));
  if (r.type === "permission") {
    const risk = stringList(f, "risk");
    if (risk.length) add("Risk", risk.map((x) => esc(capitalize(formatLabel(x)))).join("; "));
  }
  if (r.type === "problem") {
    if (f.costToday) add("Cost today", esc(f.costToday));
    if (f.workaroundToday) add("Workaround today", esc(f.workaroundToday));
    if (r.evidenceStrength) add("Evidence strength", esc(capitalize(r.evidenceStrength)));
  }
  if (r.type === "journey") {
    const finishedWhen = stringField(f, "finishedWhen");
    if (finishedWhen) add("Finished when", esc(finishedWhen));
  }
  if (r.type === "screen") {
    const address = stringField(f, "address") || stringField(f, "host");
    if (address) add("Address", `<span class="${codeCls}">${esc(address)}</span>`);
    const entryPoint = stringField(f, "entryPoint");
    if (entryPoint) add("Entry point", inlineRefHtml(book, stripStep(entryPoint)));
    const floorplan = stringField(f, "floorplan");
    if (floorplan) add("Floorplan", esc(capitalize(formatLabel(floorplan))));
    if (r.route) {
      if (routeFileOf(r.route)) out.push({ label: "Route", html: "", route: true });
      else add("Route", "No page carries this screen");
    } else if (r.captureState) add("Capture state", esc(r.captureState));
  }
  for (const x of factsFor(r.type, r, env)) {
    if (x.text && !out.some((row) => row.label === x.key)) add(x.key, esc(x.text));
  }
  for (const key of Object.keys(f)) {
    if (NON_TABLE_FIELDS.has(key)) continue;
    // A field a type fact already shows, such as kind, keeps that one row.
    const label = env.labelField(key);
    if (out.some((row) => row.label === label)) continue;
    const v = f[key];
    if (typeof v === "string" && v) add(label, esc(v));
    else if (typeof v === "number") add(label, esc(v));
    else if (typeof v === "boolean") add(label, v ? "Yes" : "No");
  }
  return out;
}