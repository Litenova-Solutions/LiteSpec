// Record behavior values: the computed details the record page reads from
// behavior records, as pure functions over values the components resolve.

import { capitalize, duration, esc, formatLabel, pluralize, stripStep } from "$lib/frame";
import type { Book, BookRecord, CheckLevel, ProofState, RecordFields } from "$lib/frame";
import { badgeHtml, codeCls, inlineRefHtml, opRefHtml } from "./page-html";

/** Whether a value is a plain field map. */
export function isRecord(value: unknown): value is RecordFields {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** One string field, read as the book wrote it. */
export function stringField(f: RecordFields | null | undefined, name: string): string | undefined {
  const v = f?.[name];
  if (v === undefined || v === null) return undefined;
  return typeof v === "string" ? v : String(v);
}

/** One string-list field, read as strings. */
export function stringList(f: RecordFields | null | undefined, name: string): string[] {
  const v = f?.[name];
  return Array.isArray(v) ? v.map(String) : [];
}

/** One nested field map, or undefined. */
export function objectField(f: RecordFields | null | undefined, name: string): RecordFields | undefined {
  const v = f?.[name];
  return isRecord(v) ? v : undefined;
}

/** One list of nested field maps, skipping anything else. */
export function objectList(f: RecordFields | null | undefined, name: string): RecordFields[] {
  const v = f?.[name];
  return Array.isArray(v) ? v.filter(isRecord) : [];
}

/** The fields the use-case renderer reads. */
export interface UseCaseFields {
  kind?: string;
  input: RecordFields[];
  output: unknown;
  effects: RecordFields[];
  changes: string[];
  reads: string[];
  access?: RecordFields;
  reachedThrough: string[];
  background?: string;
  audit?: RecordFields;
}

/** The fields the use-case renderer reads, through the typed readers. */
export function useCaseFields(f: RecordFields): UseCaseFields {
  return {
    kind: stringField(f, "kind"),
    input: objectList(f, "input"),
    output: f.output,
    effects: objectList(f, "effects"),
    changes: stringList(f, "changes"),
    reads: stringList(f, "reads"),
    access: objectField(f, "access"),
    reachedThrough: stringList(f, "reachedThrough"),
    background: stringField(f, "background"),
    audit: objectField(f, "audit"),
  };
}

/** The fields the scenario renderer reads. */
export interface ScenarioFields {
  given?: string;
  when?: string;
  then?: string;
  exercises: string[];
  examples: RecordFields[];
}

/** The fields the scenario renderer reads, through the typed readers. */
export function scenarioFields(f: RecordFields): ScenarioFields {
  return {
    given: stringField(f, "given"),
    when: stringField(f, "when"),
    then: stringField(f, "then"),
    exercises: stringList(f, "exercises"),
    examples: objectList(f, "examples"),
  };
}

/** The fields the requirement renderer reads. */
export interface RequirementFields {
  except: RecordFields[];
  values: RecordFields[];
  measure?: string;
  target?: string;
  dueBy?: string;
  check?: string;
  runsOn?: string;
  evidence?: string;
}

/** The fields the requirement renderer reads, through the typed readers. */
export function requirementFields(f: RecordFields): RequirementFields {
  return {
    except: objectList(f, "except"),
    values: objectList(f, "values"),
    measure: stringField(f, "measure"),
    target: stringField(f, "target"),
    dueBy: stringField(f, "dueBy"),
    check: stringField(f, "check"),
    runsOn: stringField(f, "runsOn"),
    evidence: stringField(f, "evidence"),
  };
}

/** The fields the journey renderer reads. */
export interface JourneyFields {
  steps: RecordFields[];
  finishedWhen?: string;
}

/** The fields the journey renderer reads, through the typed readers. */
export function journeyFields(f: RecordFields): JourneyFields {
  return { steps: objectList(f, "steps"), finishedWhen: stringField(f, "finishedWhen") };
}

/** The fields the screen renderer reads. */
export interface ScreenFields {
  address?: string;
  host?: string;
  entryPoint?: string;
  floorplan?: string;
  offers: string[];
}

/** The fields the screen renderer reads, through the typed readers. */
export function screenFields(f: RecordFields): ScreenFields {
  return {
    address: stringField(f, "address"),
    host: stringField(f, "host"),
    entryPoint: stringField(f, "entryPoint"),
    floorplan: stringField(f, "floorplan"),
    offers: stringList(f, "offers"),
  };
}

/** The fields the job renderer reads. */
export interface JobFields {
  kind?: string;
  schedule?: RecordFields;
  guard?: string;
  batchSize?: string;
  lease: unknown;
  runsWhere?: string;
  cursor: unknown;
  selection?: string;
  serviceLevel?: string;
  evidence?: string;
  runbook?: string;
}

/** The fields the job renderer reads, through the typed readers. */
export function jobFields(f: RecordFields): JobFields {
  return {
    kind: stringField(f, "kind"),
    schedule: objectField(f, "schedule"),
    guard: stringField(f, "guard"),
    batchSize: stringField(f, "batchSize"),
    lease: f.lease,
    runsWhere: stringField(f, "runsWhere"),
    cursor: f.cursor,
    selection: stringField(f, "selection"),
    serviceLevel: stringField(f, "serviceLevel"),
    evidence: stringField(f, "evidence"),
    runbook: stringField(f, "runbook"),
  };
}

/** The fields the reaction renderer reads. */
export interface ReactionFields {
  triggeredBy: unknown;
  skippedWhen?: string;
}

/** The fields the reaction renderer reads, through the typed readers. */
export function reactionFields(f: RecordFields): ReactionFields {
  return { triggeredBy: f.triggeredBy, skippedWhen: stringField(f, "skippedWhen") };
}

/** The fields the aggregate renderer reads. */
export interface AggregateFields {
  kind?: string;
  states: RecordFields[];
  transitions: RecordFields[];
  dataClasses: RecordFields[];
  data: RecordFields[];
  finishedWhen?: string;
}

/** The fields the aggregate renderer reads, through the typed readers. */
export function aggregateFields(f: RecordFields): AggregateFields {
  return {
    kind: stringField(f, "kind"),
    states: objectList(f, "states"),
    transitions: objectList(f, "transitions"),
    dataClasses: objectList(f, "dataClasses"),
    data: objectList(f, "data"),
    finishedWhen: stringField(f, "finishedWhen"),
  };
}

export interface ResultInfo {
  title: string;
  outcome: string;
  code: string;
  meaning: string;
}

/** The result a scenario shows, read from its result record. */
export function resultInfoOf(result: BookRecord | null | undefined): ResultInfo | null {
  if (!result) return null;
  const f = result.fields ?? {};
  return {
    title: result.title,
    outcome: stringField(f, "outcome") ?? "",
    code: stringField(f, "code") ?? "",
    meaning: stringField(f, "meaning") ?? "",
  };
}

/** The refusal a rule raises: its record when one is linked, else its id. */
export function refusalOf(
  refusal: BookRecord | null | undefined,
  refusalId: string | null,
): { info: ResultInfo | null; id: string | null } {
  if (!refusal) return { info: null, id: refusalId };
  const f = refusal.fields ?? {};
  return {
    info: {
      title: refusal.title,
      outcome: "",
      code: stringField(f, "code") ?? "",
      meaning: stringField(f, "meaning") ?? "",
    },
    id: refusalId,
  };
}

/** Whether an actor record carries a team mark, in any team-named field. */
export function teamValue(f: RecordFields): boolean | undefined {
  for (const k of Object.keys(f)) {
    const v = f[k];
    if (/team/i.test(k) && typeof v === "boolean") return v;
  }
  return undefined;
}

/** What a relation label reads from the lexicon: its phrase and its carrier types. */
export interface PhraseLookup {
  phraseFor(field: string, carrier: string | null, dir: "forward" | "inverse"): string;
  typeOf(id: string): string;
}

/** Each link field read as a sentence, in the Links table phrases. */
export function relationLabelOf(label: string, items: { id: string }[], selfType: string, lookup: PhraseLookup): string {
  const inbound = label.indexOf("Points at this: ") === 0;
  const field = inbound ? label.slice(16) : label;
  const first = items[0];
  const phrase = inbound
    ? lookup.phraseFor(field, first ? lookup.typeOf(first.id) : null, "inverse")
    : lookup.phraseFor(field, selfType, "forward");
  return capitalize(String(phrase || field).replace(/^is\s+/i, "").replace(/^are\s+/i, ""));
}

/** One wave-5 value in LiteSpec words: the mode names the case, the reason completes "because ...". */
export function modeWord(m: unknown) {
  return formatLabel(m);
}

export function freshnessNoteHtml(book: Book, g: RecordFields) {
  const f = objectField(g, "freshness");
  if (!f) return "";
  const mode = stringField(f, "mode");
  if (mode === "bounded") return "fresh within " + duration(stringField(f, "within") ?? "");
  if (mode === "advisory") return "advisory, authoritative value in " + inlineRefHtml(book, stringField(f, "authority") ?? "");
  return esc(capitalize(modeWord(f.mode)));
}

export function pagingRows(f: RecordFields): { label: string; html: string }[] | null {
  const p = objectField(f, "paging");
  if (!p) return null;
  if (stringField(p, "mode") === "none") {
    const reason = stringField(p, "reason");
    return [{ label: "Paging", html: "None" + (reason ? ", because " + esc(reason) : "") }];
  }
  return [
    { label: "Order", html: esc(p.order) },
    { label: "Paging", html: esc(p.offset ?? p.cursor) },
    { label: "Page size", html: `${esc(p.defaultLimit)} by default, at most ${esc(p.maxLimit)}` },
  ];
}

export function periodSuffixHtml(p: unknown) {
  if (!p) return "";
  if (typeof p === "string") return " after " + duration(p);
  const reason = isRecord(p) ? stringField(p, "reason") : undefined;
  return ", None" + (reason ? ", because " + esc(reason) : "");
}

export function releasedByHtml(book: Book, r: unknown) {
  if (!r) return "";
  if (typeof r === "string") return inlineRefHtml(book, r);
  const reason = isRecord(r) ? stringField(r, "reason") : undefined;
  return "Never" + (reason ? ", because " + esc(reason) : "");
}

export function erasureHtml(book: Book, e: RecordFields | undefined) {
  if (!e) return "";
  if (stringField(e, "mode") === "call") return "Calls " + opRefHtml(book, e.operation);
  if (stringField(e, "mode") === "event") return "Forwards through " + inlineRefHtml(book, stringField(e, "event") ?? "");
  const reason = stringField(e, "reason");
  return "Impossible" + (reason ? ", because " + esc(reason) : "");
}

export function edgeFailureText(o: RecordFields | undefined) {
  if (!o) return "";
  if (stringField(o, "mode") === "retry") {
    const r: RecordFields = objectField(o, "retry") ?? {};
    return (
      "On failure: " +
      pluralize(Number(r.maxAttempts ?? 0), "attempt") +
      (r.initialDelay ? ", from " + duration(String(r.initialDelay)) : "") +
      (r.maxDelay ? " up to " + duration(String(r.maxDelay)) : "") +
      (r.backoff ? ", " + esc(modeWord(r.backoff)) + " backoff" : "") +
      (o.then ? ", then " + esc(modeWord(o.then)) : "")
    );
  }
  if (stringField(o, "mode") === "skip-and-log") return "On failure: skips and logs";
  if (stringField(o, "mode") === "discard") return "On failure: discards";
  return "On failure: " + esc(modeWord(o.mode));
}

type IdempotencyMode = "natural" | "key" | "none" | "read-only";
const IDEM_MODES = {
  natural: "Naturally idempotent",
  key: "Idempotency key",
  none: "Not idempotent",
  "read-only": "Reads only",
} satisfies Record<IdempotencyMode, string>;
function isIdempotencyMode(v: string): v is IdempotencyMode {
  return v === "natural" || v === "key" || v === "none" || v === "read-only";
}

export function idemTileOf(f: RecordFields): { value: string; hint: string } | null {
  const i = objectField(f, "idempotency");
  if (!i) return null;
  const mode = stringField(i, "mode");
  return {
    value: esc(mode && isIdempotencyMode(mode) ? IDEM_MODES[mode] : capitalize(i.mode)),
    hint: [
      i.key ? `<span class="${codeCls}">${esc(i.key)}</span>` : "",
      i.scope ? "scoped to " + esc(i.scope) : "",
      i.reason ? esc(i.reason) : "",
    ]
      .filter(Boolean)
      .join(" &middot; "),
  };
}

export function triggeredByHtml(book: Book, t: unknown) {
  if (!t) return "";
  if (Array.isArray(t))
    return t.length ? `<div class="flex flex-wrap items-center gap-1.5 text-sm">${t.map((x) => inlineRefHtml(book, String(x))).join(" ")}</div>` : "";
  if (typeof t === "string") return `<div class="flex flex-wrap items-center gap-1.5 text-sm">${inlineRefHtml(book, t)}</div>`;
  return "";
}

export function typedFieldHtml(book: Book, x: RecordFields) {
  const bits: string[] = [];
  if (x.type) bits.push(`<span class="${codeCls}">${esc(x.type)}</span>`);
  if (x.required) bits.push(badgeHtml("Required"));
  if (x.of) bits.push(Array.isArray(x.of) ? esc(x.of.join(", ")) : inlineRefHtml(book, String(x.of)));
  return bits.join(" ");
}

type Classification = "public" | "internal" | "personal" | "sensitive" | "confidential";
const CLASSIFICATIONS = {
  public: "public",
  internal: "internal",
  personal: "personal",
  sensitive: "sensitive",
  confidential: "confidential",
} satisfies Record<Classification, string>;
function isClassification(v: string): v is Classification {
  return v === "public" || v === "internal" || v === "personal" || v === "sensitive" || v === "confidential";
}

export function clsBadgeHtml(c: unknown) {
  const key = typeof c === "string" ? c : "";
  return badgeHtml(key && isClassification(key) ? CLASSIFICATIONS[key] : capitalize(c));
}

export function bulletsHtml(list: unknown) {
  const items: unknown[] = typeof list === "string" ? (list ? [list] : []) : Array.isArray(list) ? list : [];
  if (items.length) return "<ul>" + items.map((x) => "<li>" + esc(x) + "</li>").join("") + "</ul>";
  return '<span class="font-mono text-[0.6875rem] text-muted-foreground">&ndash;</span>';
}

export type EffectVerb = "runs" | "sends" | "emits" | "calls";
export const EFFECT_VERBS = {
  runs: { label: "Runs", icon: "use-case" },
  sends: { label: "Sends", icon: "notification" },
  emits: { label: "Emits", icon: "integration-event" },
  calls: { label: "Calls", icon: "ext" },
} satisfies Record<EffectVerb, { label: string; icon: string }>;
const EFFECT_VERB_KEYS: readonly EffectVerb[] = ["runs", "sends", "emits", "calls"];

export function effectVerbOf(e: RecordFields): EffectVerb | null {
  return EFFECT_VERB_KEYS.find((k) => e[k]) ?? null;
}

export function effectCondHtml(e: RecordFields) {
  return [
    e.forEach ? "For each " + esc(e.forEach) : "",
    e.onFailure ? edgeFailureText(isRecord(e.onFailure) ? e.onFailure : {}) : "",
    e.sharesTransaction !== undefined
      ? String(e.sharesTransaction) === "true"
        ? "In the same transaction"
        : "In its own transaction"
      : "",
  ]
    .filter(Boolean)
    .join(" &middot; ");
}

/** Whether a browser page can reach the record: a screen hangs below one of its entry points. */
export function browserReached(reachedThrough: string[], hasScreenBelow: (entryId: string) => boolean): boolean {
  return reachedThrough.some((x) => hasScreenBelow(stripStep(x)));
}

/** What one journey step's server line reads: whether it is coded, and how many of its scenarios pass. */
export function serverLineInfo(
  machine: BookRecord["machine"],
  scenarioStates: (ProofState | null | undefined)[],
): { built: boolean; scenarios: number; verified: number } | null {
  if (!machine) return null;
  return {
    built: !!machine.coded,
    scenarios: scenarioStates.length,
    verified: scenarioStates.filter((s) => s === "awaiting" || s === "passing").length,
  };
}

/** The width and theme choice a screenshot reads. */
export interface CaptureChoice {
  width: string;
  theme: string;
}

/** The capture a width and theme choice reads: the nearest match, else the first. */
export function pickCapture(captures: NonNullable<BookRecord["captures"]>, choice: CaptureChoice) {
  const wantWidth = choice.width === "phone" ? 390 : 1440;
  const narrow = choice.width === "phone";
  const widthOf = (c: (typeof captures)[number]) => c.width ?? Number.NaN;
  return (
    captures.find((c) => c.theme === choice.theme && (widthOf(c) === wantWidth || (narrow ? widthOf(c) < 600 : widthOf(c) >= 600))) ??
    captures.find((c) => c.theme === choice.theme) ??
    captures[0]
  );
}

type TenantScope = "own" | "cross" | "public" | "none";
const TENANT_SCOPES = { own: "own", cross: "cross", public: "public", none: "none" } satisfies Record<TenantScope, string>;
function isTenantScope(v: string): v is TenantScope {
  return v === "own" || v === "cross" || v === "public" || v === "none";
}

/** A tenant scope in LiteSpec words, read as itself when the model names it. */
export function tenantWord(tenant: unknown): string {
  const key = typeof tenant === "string" ? tenant : "";
  return key && isTenantScope(key) ? TENANT_SCOPES[key] : capitalize(tenant);
}

const CHECK_LEVEL_TEXT = {
  G: "Blocks a release",
  S: "Blocks when qualified",
  A: "Advice",
} satisfies Record<CheckLevel, string>;
function isCheckLevel(v: string): v is CheckLevel {
  return v === "G" || v === "S" || v === "A";
}

/** A check level in words: what the level blocks. Unknown levels read as nothing. */
export function checkLevelText(level: unknown): string {
  const key = typeof level === "string" ? level : "";
  return isCheckLevel(key) ? CHECK_LEVEL_TEXT[key] : "";
}

/**
 * The source file a route object points at. The writer carried { file, route }
 * and now carries { path }; both read here until the old shape is gone.
 */
export function routeFileOf(route: RecordFields | null | undefined): string | null {
  if (!route) return null;
  return stringField(route, "file") ?? stringField(route, "path") ?? null;
}

/** The display text of a route object: the route, then the path. */
export function routeTextOf(route: RecordFields | null | undefined): string | null {
  if (!route) return null;
  return stringField(route, "route") ?? stringField(route, "path") ?? null;
}
