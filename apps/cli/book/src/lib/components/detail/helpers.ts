// Detail helpers: the book.js building blocks the Svelte components share, as pure
// functions. HTML fragments carry record links as data-go buttons, which the App
// opens, and explanations as data-explain, which the ExplainLayer shows.

import { base, book, cap, plural, uniq } from "$lib/book";
import type { Batch, BookRecord, Fields } from "$lib/book/types";
import { duration, editorHref, esc, inline, safeHref } from "$lib/book/html";

export interface BodySection {
  h: string;
  html: string;
}

export interface WhyPart {
  label: string;
  html: string;
}

/** An inline code span, as book.js rendered .code. */
export const codeCls =
  "inline-block rounded border border-border bg-muted px-1.5 py-px font-mono text-xs font-medium whitespace-nowrap text-subtle-foreground";

/** A record named inside a sentence: its title as a link, or the bare identifier. */
export function inlineRefHtml(id: string) {
  const b = base(id);
  if (book.has(b))
    return `<button type="button" class="underline decoration-strong underline-offset-2 hover:decoration-foreground" data-go="${esc(b)}">${esc(book.title(b))}</button>`;
  return `<span class="font-mono text-[0.9em] text-muted-foreground" title="No record with this identifier is in the book">${esc(b)}</span>`;
}

/** An identifier that names no record in the book, read as itself, never as a link. */
export function missingRefHtml(id: string) {
  return `<span class="underline decoration-dashed decoration-strong underline-offset-2 text-muted-foreground" title="No record with this identifier is in the book">${esc(id)}</span>`;
}

/** A table cell, as book.js rendered td. */
export function tdHtml(html: string, cls = "") {
  const extra =
    cls === "nm"
      ? " font-mono text-[13px] font-medium whitespace-nowrap w-[1%]"
      : cls === "mut"
        ? " text-muted-foreground"
        : cls === "num"
          ? " text-right tabular-nums"
          : "";
  return `<td class="border-b border-border px-3.5 py-2.5 align-top text-sm${extra}">${html}</td>`;
}

/** A fact: book.js rendered every badge as plain emphasized text. */
export function badgeHtml(text: unknown) {
  return `<span class="font-medium text-foreground">${esc(text)}</span>`;
}

/** Content that explains itself from LiteSpec on hover, focus or click. */
export function xpHtml(key: string, inner: string) {
  return `<button type="button" class="cursor-help rounded-sm" data-explain="${esc(key)}">${inner}</button>`;
}

/** A link to a file and line in the editor, or a plain span when none opens. */
export function fileLinkHtml(path: string, line: number | null, label?: string) {
  const root = book.man?.roots?.consumer ?? "";
  const href = editorHref(book, root, path, line ?? null);
  const name = String(path).split("/").pop() ?? path;
  const dir = String(path)
    .split("/")
    .slice(0, -1)
    .join("/")
    .replace(/\//g, "/<wbr>");
  const body =
    `<span class="block text-[13.5px] font-medium break-all">${esc(label || name)}</span>` +
    `<span class="block font-mono text-[11px] text-muted-foreground break-all">${dir}${line ? ":" + line : ""}</span>`;
  if (href)
    return `<a class="block text-foreground hover:[&_.fn]:underline" href="${esc(href)}" title="Open ${esc(path)}${line ? " at line " + line : ""}">${body}</a>`;
  return `<span class="block text-foreground">${body}</span>`;
}

/** A LiteSpec provision ID opens its standards page in the LiteSpec checkout. */
export function provisionLinkHtml(p: unknown) {
  const m = /^litespec\/(.+)#(.+)$/.exec(String(p ?? ""));
  if (!m) return `<span class="${codeCls}">${esc(p)}</span>`;
  const href = editorHref(book, book.man?.roots?.litespec ?? "", "docs/" + m[1] + ".md");
  return href
    ? `<a class="${codeCls} hover:underline" href="${esc(href)}" title="Open the provision in LiteSpec">${esc(p)}</a>`
    : `<span class="${codeCls}">${esc(p)}</span>`;
}

export function linkOrTextHtml(value: unknown) {
  const href = safeHref(book, value);
  return href ? `<a href="${esc(href)}">${esc(value)}</a>` : esc(value);
}

// ---------- prose ----------

/** One rendered prose field of a record. */
export function proseOf(r: BookRecord, name: string) {
  return r.fieldsHtml?.[name] ?? "";
}

/** The explanation sections of a record. The CLI writes each as { h, html }. */
export function secList(r: BookRecord): BodySection[] {
  const list = (r.sections as { list?: unknown } | null)?.list;
  return Array.isArray(list) ? (list as BodySection[]) : [];
}

export function secIntro(r: BookRecord) {
  return r.sections?.intro ?? "";
}

export function leadTextOf(r: BookRecord) {
  const f = r.fields ?? {};
  return String(f.description ?? f.statement ?? f.summary ?? f.offering ?? f.purpose ?? f.goal ?? f.meaning ?? f.boundary ?? f.question ?? f.reason ?? "");
}

function stripHtml(html: string) {
  return html
    .replace(/<[^>]*>/g, "")
    .replace(/\s+/g, " ");
}

export const WHY_SECTIONS = /^(Why.*|Rationale)$/;

/** The explanation intro, unless it repeats the lead the page already shows. */
export function bodyIntroOf(r: BookRecord) {
  const intro = secIntro(r).trim();
  const lt = leadTextOf(r).slice(0, 40);
  if (intro && !(lt && stripHtml(intro).indexOf(lt) === 0)) return intro;
  return "";
}

export interface BodySectionView {
  key: string;
  h: string;
  html: string;
  kind: "risk" | "example" | "questions" | "default";
}

/** The explanation sections, minus the Why sections the page places separately. */
export function bodySectionsOf(r: BookRecord): BodySectionView[] {
  return secList(r)
    .map((s, i) => ({ key: "s" + i, h: s.h, html: s.html ?? "", kind: "default" as BodySectionView["kind"] }))
    .filter((s) => (s.html || "").trim() && !WHY_SECTIONS.test(s.h))
    .map((s) => ({
      ...s,
      kind: /^Known Limits$/.test(s.h) ? "risk" : /^Example$/.test(s.h) ? "example" : /Open (Modeling )?Questions/.test(s.h) ? "questions" : "default",
    }));
}

/** The Why rows: reason fields first, then the Why explanation sections. */
export function whyPartsOf(r: BookRecord): WhyPart[] {
  const f = r.fields ?? {};
  const parts: WhyPart[] = [];
  const rows: [string, string][] = [
    ["reason", "Reason"],
    ["rationale", "Rationale"],
    ["appliesBecause", "Applies because"],
    ["refusedBecause", "Refused because"],
  ];
  for (const [field, label] of rows) if (f[field]) parts.push({ label, html: inline(book, String(f[field]), r.src ?? null) });
  for (const s of secList(r)) if (WHY_SECTIONS.test(s.h) && (s.html || "").trim()) parts.push({ label: s.h, html: s.html });
  return parts;
}

/** The prose fields a type without its own renderer shows, in display order. */
export const PROSE_ORDER = [
  "summary", "purpose", "whatHappens", "offering", "position", "refusals", "situation", "whyItMatters",
  "consequences", "betweenSteps", "delivery", "exposure", "exit", "impact", "prerequisites", "procedure",
  "verification", "recovery", "escalation",
];

// ---------- record details ----------

export interface ResultInfo {
  title: string;
  outcome: string;
  code: string;
  meaning: string;
}

export function resultInfoOf(scenarioId: string): ResultInfo | null {
  const r = book.get(scenarioId);
  if (r?.resultInfo) return r.resultInfo as ResultInfo;
  const rid = book.outs(base(scenarioId), "result")[0];
  const q = rid ? book.get(rid) : undefined;
  if (!q) return null;
  const f = q.fields ?? {};
  return { title: q.title, outcome: f.outcome, code: f.code, meaning: f.meaning };
}

export function refusalOf(ruleId: string): { info: ResultInfo | null; id: string | null } {
  const r = book.get(ruleId);
  if (r?.refusalInfo) return { info: r.refusalInfo as ResultInfo, id: null };
  const rid = book.outs(base(ruleId), "refusal")[0];
  const q = rid ? book.get(rid) : undefined;
  const f = q?.fields ?? {};
  return { info: q ? { title: q.title, outcome: "", code: f.code, meaning: f.meaning } : null, id: rid ?? null };
}

/** Why the record carries its status, in one sentence. */
export function statusReasonHtml(id: string) {
  const r = book.get(id);
  if (!r) return "";
  const f = r.fields ?? {};
  if (r.status === "draft")
    return (
      "Waiting for " +
      esc(r.decider || "a decider") +
      (r.decideBy ? " to decide by " + book.fmtDate(r.decideBy) : "") +
      (book.openQs(id).length ? ", with " + plural(book.openQs(id).length, "open question") : "")
    );
  if (r.effective === "draft") {
    const held = Array.isArray(r.heldBy) ? (r.heldBy as unknown[]) : [];
    return (
      "Counts as draft while " +
      (held.length
        ? held.map((x) => inlineRefHtml(typeof x === "string" ? x : String((x as Fields)?.id ?? ""))).join(", ") + " draft"
        : "a record above it is draft")
    );
  }
  if (r.status === "retired") return "Replaced";
  return f.approvedBy
    ? (book.isBeh(id) ? "Reviewed by " : "Approved by ") + esc(f.approvedBy) + (f.approvedOn ? " on " + book.fmtDate(f.approvedOn) : "")
    : "Settled";
}

export interface Completion {
  stage: string;
  proof: string;
  heldBy: string[];
  heldByMore: number;
  counts: Record<string, { done?: unknown; of?: unknown }>;
}

export function completionOf(r: BookRecord): Completion | null {
  const c = r.completion as Fields | null;
  if (!c || typeof c !== "object") return null;
  let held: string[] = [];
  if (Array.isArray(c.heldBy)) held = (c.heldBy as unknown[]).map((x) => (typeof x === "string" ? x : String((x as Fields)?.id ?? (x as Fields)?.record ?? ""))).filter(Boolean);
  else if (c.heldBy && Array.isArray((c.heldBy as Fields).ids)) held = ((c.heldBy as Fields).ids as string[]).slice();
  const holder = c.heldBy as Fields | null;
  const more = Number(c.heldByCount || c.heldByMore || (holder && !Array.isArray(holder) ? holder.more || holder.count : 0) || 0);
  return { stage: String(c.stage ?? ""), proof: String(c.proof ?? ""), heldBy: held, heldByMore: more, counts: (c.counts as Completion["counts"]) ?? {} };
}

export function teamValue(f: Fields) {
  for (const k of Object.keys(f ?? {})) if (/team/i.test(k) && typeof f[k] === "boolean") return f[k] as boolean;
  return undefined;
}

/** Each link field read as a sentence, in the Links table phrases. */
export function relationLabelOf(label: string, items: { id: string }[], selfType: string) {
  const inbound = label.indexOf("Points at this: ") === 0;
  const field = inbound ? label.slice(16) : label;
  const phrase = inbound
    ? book.phraseFor(field, items.length ? book.typeOf(items[0].id) : null, "inverse")
    : book.phraseFor(field, selfType, "forward");
  return cap(String(phrase || field).replace(/^is\s+/i, "").replace(/^are\s+/i, ""));
}

/** A batch named by what it holds, as release.md describes the four kinds. */
export function batchNameOf(b: Batch) {
  const p = String(b.selector ?? "").split("/");
  if (b.kind === "use-case") return "This use case with its results and scenarios";
  if (b.kind === "decision") {
    const dn = base(String(b.selector ?? "").replace(/^decision\//, ""));
    return "Decision " + (book.has(dn) ? '"' + book.title(dn) + '"' : String(b.selector ?? "")) + " with what it decides";
  }
  if (b.kind === "record") return "This record on its own";
  return "All draft " + String(book.pluralName(p[1] ?? "") || p[1] || "").toLowerCase() + (p[0] === "intent" || p[0] === "governance" ? "" : " in " + p[0].replace(/-/g, " "));
}

export function ballotNameOf(key: string) {
  if (book.has(key)) return book.title(key);
  return key.replace(/^decision\//, "").replace(/-/g, " ");
}

// ---------- behavior values ----------

/** One wave-5 value in LiteSpec words: the mode names the case, the reason completes "because ...". */
export function modeWord(m: unknown) {
  return String(m ?? "").replace(/-/g, " ");
}

export function opRefHtml(op: unknown) {
  const b = base(op);
  const tail = String(op ?? "").split("#")[1];
  return inlineRefHtml(b) + (tail ? ` <span class="font-mono text-[11px] text-muted-foreground">${esc(tail.replace(/-/g, " "))}</span>` : "");
}

export function freshnessNoteHtml(g: Fields) {
  const f = g.freshness as Fields | undefined;
  if (!f) return "";
  if (f.mode === "bounded") return "fresh within " + duration(String(f.within ?? ""));
  if (f.mode === "advisory") return "advisory, authoritative value in " + inlineRefHtml(String(f.authority ?? ""));
  return esc(cap(modeWord(f.mode)));
}

export function pagingRows(f: Fields): { label: string; html: string }[] | null {
  const p = f.paging as Fields | undefined;
  if (!p) return null;
  if (p.mode === "none") return [{ label: "Paging", html: "None" + (p.reason ? ", because " + esc(p.reason) : "") }];
  return [
    { label: "Order", html: esc(p.order) },
    { label: "Paging", html: esc(p.offset ?? p.cursor) },
    { label: "Page size", html: `${esc(p.defaultLimit)} by default, at most ${esc(p.maxLimit)}` },
  ];
}

export function compensationHtml(c: Fields | undefined) {
  if (!c) return "";
  if (c.mode === "use-case") return inlineRefHtml(String(c.useCase ?? ""));
  return "None" + (c.reason ? ", because " + esc(c.reason) : "");
}

export function periodSuffixHtml(p: unknown) {
  if (!p) return "";
  if (typeof p === "string") return " after " + duration(p);
  const r = (p as Fields).reason;
  return ", None" + (r ? ", because " + esc(r) : "");
}

export function releasedByHtml(r: unknown) {
  if (!r) return "";
  if (typeof r === "string") return inlineRefHtml(r);
  const reason = (r as Fields).reason;
  return "Never" + (reason ? ", because " + esc(reason) : "");
}

export function erasureHtml(e: Fields | undefined) {
  if (!e) return "";
  if (e.mode === "call") return "Calls " + opRefHtml(e.operation);
  if (e.mode === "event") return "Forwards through " + inlineRefHtml(String(e.event ?? ""));
  return "Impossible" + (e.reason ? ", because " + esc(e.reason) : "");
}

export function edgeFailureText(o: Fields | undefined) {
  if (!o) return "";
  if (o.mode === "retry") {
    const r = (o.retry as Fields) ?? {};
    return (
      "On failure: " +
      plural(Number(r.maxAttempts ?? 0), "attempt") +
      (r.initialDelay ? ", from " + duration(String(r.initialDelay)) : "") +
      (r.maxDelay ? " up to " + duration(String(r.maxDelay)) : "") +
      (r.backoff ? ", " + esc(modeWord(r.backoff)) + " backoff" : "") +
      (o.then ? ", then " + esc(modeWord(o.then)) : "")
    );
  }
  if (o.mode === "skip-and-log") return "On failure: skips and logs";
  if (o.mode === "discard") return "On failure: discards";
  return "On failure: " + esc(modeWord(o.mode));
}

export function idemTileOf(f: Fields): { value: string; hint: string } | null {
  const i = f.idempotency as Fields | undefined;
  if (!i) return null;
  const MODE: Record<string, string> = { natural: "Naturally idempotent", key: "Idempotency key", none: "Not idempotent", "read-only": "Reads only" };
  return {
    value: esc(MODE[String(i.mode)] ?? cap(i.mode)),
    hint: [
      i.key ? `<span class="${codeCls}">${esc(i.key)}</span>` : "",
      i.scope ? "scoped to " + esc(i.scope) : "",
      i.reason ? esc(i.reason) : "",
    ]
      .filter(Boolean)
      .join(" &middot; "),
  };
}

export function triggeredByHtml(t: unknown) {
  if (!t) return "";
  if (Array.isArray(t)) return t.length ? `<div class="flex flex-wrap items-center gap-1.5 text-sm">${t.map((x) => inlineRefHtml(String(x))).join(" ")}</div>` : "";
  if (typeof t === "string") return `<div class="flex flex-wrap items-center gap-1.5 text-sm">${inlineRefHtml(t)}</div>`;
  return "";
}

export function typedFieldHtml(x: Fields) {
  const bits: string[] = [];
  if (x.type) bits.push(`<span class="${codeCls}">${esc(x.type)}</span>`);
  if (x.required) bits.push(badgeHtml("Required"));
  if (x.of) bits.push(Array.isArray(x.of) ? esc(x.of.join(", ")) : inlineRefHtml(String(x.of)));
  return bits.join(" ");
}

const CLS: Record<string, string> = { public: "public", internal: "internal", personal: "personal", sensitive: "sensitive", confidential: "confidential" };

export function clsBadgeHtml(c: unknown) {
  return badgeHtml(CLS[String(c)] ?? cap(c));
}

export function bulletsHtml(list: unknown) {
  const items = typeof list === "string" && list ? [list] : (list as string[]);
  if (items?.length) return "<ul>" + items.map((x) => "<li>" + esc(x) + "</li>").join("") + "</ul>";
  return '<span class="font-mono text-[11px] text-muted-foreground">&ndash;</span>';
}

export const EFFECT_VERBS: Record<string, { label: string; icon: string }> = {
  runs: { label: "Runs", icon: "use-case" },
  sends: { label: "Sends", icon: "notification" },
  emits: { label: "Emits", icon: "integration-event" },
  calls: { label: "Calls", icon: "ext" },
};

export function effectVerbOf(e: Fields) {
  return (Object.keys(EFFECT_VERBS).find((k) => e[k]) as "runs" | "sends" | "emits" | "calls" | undefined) ?? null;
}

export function effectCondHtml(e: Fields) {
  return [
    e.forEach ? "For each " + esc(e.forEach) : "",
    e.onFailure ? edgeFailureText(e.onFailure as Fields) : "",
    e.sharesTransaction !== undefined ? (String(e.sharesTransaction) === "true" ? "In the same transaction" : "In its own transaction") : "",
  ]
    .filter(Boolean)
    .join(" &middot; ");
}

/** Whether a browser page can reach the record: a screen hangs below one of its entry points. */
export function browserReached(r: BookRecord) {
  return (((r.fields as Fields)?.reachedThrough as string[]) ?? []).some((x) => book.kids(base(x)).some((k) => book.typeOf(k) === "screen"));
}

export function serverLineInfo(b: string): { built: boolean; scenarios: number; proven: number } | null {
  const r = book.get(b);
  if (!r?.machine) return null;
  const sc = book.scenariosOf(base(b)).filter((x) => book.get(x));
  const proven = sc.filter((x) => {
    const s = book.get(x)?.proof?.state;
    return s === "awaiting" || s === "passing";
  }).length;
  return { built: !!(r.machine as Fields).implemented, scenarios: sc.length, proven };
}

export function uniqStrings(xs: unknown[]) {
  return uniq(xs.filter(Boolean).map((x) => base(String(x))));
}

