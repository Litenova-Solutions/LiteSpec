// Record behavior values: the computed details the record page reads from
// behavior records, as pure functions.

import { book, capitalize, dedupe, duration, esc, formatLabel, pluralize, stripStep } from "$lib/frame";
import type { BookRecord, RecordFields } from "$lib/frame";
import { badgeHtml, codeCls, inlineRefHtml, opRefHtml } from "./page-html";

export interface ResultInfo {
  title: string;
  outcome: string;
  code: string;
  meaning: string;
}

export function resultInfoOf(scenarioId: string): ResultInfo | null {
  const r = book.get(scenarioId);
  if (r?.resultInfo) return r.resultInfo as ResultInfo;
  const rid = book.outs(stripStep(scenarioId), "result")[0];
  const q = rid ? book.get(rid) : undefined;
  if (!q) return null;
  const f = q.fields ?? {};
  return { title: q.title, outcome: String(f.outcome ?? ""), code: String(f.code ?? ""), meaning: String(f.meaning ?? "") };
}

export function refusalOf(ruleId: string): { info: ResultInfo | null; id: string | null } {
  const r = book.get(ruleId);
  if (r?.refusalInfo) return { info: r.refusalInfo as ResultInfo, id: null };
  const rid = book.outs(stripStep(ruleId), "refusal")[0];
  const q = rid ? book.get(rid) : undefined;
  const f = q?.fields ?? {};
  return { info: q ? { title: q.title, outcome: "", code: String(f.code ?? ""), meaning: String(f.meaning ?? "") } : null, id: rid ?? null };
}

export function teamValue(f: RecordFields) {
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
  return capitalize(String(phrase || field).replace(/^is\s+/i, "").replace(/^are\s+/i, ""));
}

/** One wave-5 value in LiteSpec words: the mode names the case, the reason completes "because ...". */
export function modeWord(m: unknown) {
  return formatLabel(m);
}

export function freshnessNoteHtml(g: RecordFields) {
  const f = g.freshness as RecordFields | undefined;
  if (!f) return "";
  if (f.mode === "bounded") return "fresh within " + duration(String(f.within ?? ""));
  if (f.mode === "advisory") return "advisory, authoritative value in " + inlineRefHtml(String(f.authority ?? ""));
  return esc(capitalize(modeWord(f.mode)));
}

export function pagingRows(f: RecordFields): { label: string; html: string }[] | null {
  const p = f.paging as RecordFields | undefined;
  if (!p) return null;
  if (p.mode === "none") return [{ label: "Paging", html: "None" + (p.reason ? ", because " + esc(p.reason) : "") }];
  return [
    { label: "Order", html: esc(p.order) },
    { label: "Paging", html: esc(p.offset ?? p.cursor) },
    { label: "Page size", html: `${esc(p.defaultLimit)} by default, at most ${esc(p.maxLimit)}` },
  ];
}

export function periodSuffixHtml(p: unknown) {
  if (!p) return "";
  if (typeof p === "string") return " after " + duration(p);
  const r = (p as RecordFields).reason;
  return ", None" + (r ? ", because " + esc(r) : "");
}

export function releasedByHtml(r: unknown) {
  if (!r) return "";
  if (typeof r === "string") return inlineRefHtml(r);
  const reason = (r as RecordFields).reason;
  return "Never" + (reason ? ", because " + esc(reason) : "");
}

export function erasureHtml(e: RecordFields | undefined) {
  if (!e) return "";
  if (e.mode === "call") return "Calls " + opRefHtml(e.operation);
  if (e.mode === "event") return "Forwards through " + inlineRefHtml(String(e.event ?? ""));
  return "Impossible" + (e.reason ? ", because " + esc(e.reason) : "");
}

export function edgeFailureText(o: RecordFields | undefined) {
  if (!o) return "";
  if (o.mode === "retry") {
    const r = (o.retry as RecordFields) ?? {};
    return (
      "On failure: " +
      pluralize(Number(r.maxAttempts ?? 0), "attempt") +
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

export function idemTileOf(f: RecordFields): { value: string; hint: string } | null {
  const i = f.idempotency as RecordFields | undefined;
  if (!i) return null;
  const MODE: Record<string, string> = { natural: "Naturally idempotent", key: "Idempotency key", none: "Not idempotent", "read-only": "Reads only" };
  return {
    value: esc(MODE[String(i.mode)] ?? capitalize(i.mode)),
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

export function typedFieldHtml(x: RecordFields) {
  const bits: string[] = [];
  if (x.type) bits.push(`<span class="${codeCls}">${esc(x.type)}</span>`);
  if (x.required) bits.push(badgeHtml("Required"));
  if (x.of) bits.push(Array.isArray(x.of) ? esc(x.of.join(", ")) : inlineRefHtml(String(x.of)));
  return bits.join(" ");
}

const CLS: Record<string, string> = { public: "public", internal: "internal", personal: "personal", sensitive: "sensitive", confidential: "confidential" };

export function clsBadgeHtml(c: unknown) {
  return badgeHtml(CLS[String(c)] ?? capitalize(c));
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

export function effectVerbOf(e: RecordFields) {
  return (Object.keys(EFFECT_VERBS).find((k) => e[k]) as "runs" | "sends" | "emits" | "calls" | undefined) ?? null;
}

export function effectCondHtml(e: RecordFields) {
  return [
    e.forEach ? "For each " + esc(e.forEach) : "",
    e.onFailure ? edgeFailureText(e.onFailure as RecordFields) : "",
    e.sharesTransaction !== undefined ? (String(e.sharesTransaction) === "true" ? "In the same transaction" : "In its own transaction") : "",
  ]
    .filter(Boolean)
    .join(" &middot; ");
}

/** Whether a browser page can reach the record: a screen hangs below one of its entry points. */
export function browserReached(r: BookRecord) {
  return (((r.fields as RecordFields)?.reachedThrough as string[]) ?? []).some((x) => book.kids(stripStep(x)).some((k) => book.typeOf(k) === "screen"));
}

export function serverLineInfo(b: string): { built: boolean; scenarios: number; verified: number } | null {
  const r = book.get(b);
  if (!r?.machine) return null;
  const sc = book.scenariosOf(stripStep(b)).filter((x) => book.get(x));
  const verified = sc.filter((x) => {
    const s = book.get(x)?.proof?.state;
    return s === "awaiting" || s === "passing";
  }).length;
  return { built: !!(r.machine as RecordFields).coded, scenarios: sc.length, verified };
}

export function uniqStrings(xs: unknown[]) {
  return dedupe(xs.filter(Boolean).map((x) => stripStep(String(x))));
}
