// Record prose: the rendered fields and explanation sections of a record,
// as pure functions.

import { book, inline } from "$lib/frame";
import type { BookRecord } from "$lib/frame";

export interface BodySection {
  h: string;
  html: string;
}

export interface WhyPart {
  label: string;
  html: string;
}

/** One rendered prose field of a record. */
export function proseOf(r: BookRecord, name: string) {
  return r.fieldsHtml?.[name] ?? "";
}

/** The explanation sections of a record. The CLI writes each as { h, html }. */
export function secList(r: BookRecord): BodySection[] {
  const list = r.sections?.list;
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
