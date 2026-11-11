// Pure lexicon readers: every word the book prints about a record or a link comes
// from the lexicon shard, never invented here.

import type { Lexicon } from "./shard-types";
import { capitalize, dedupe } from "./text";

/** A LiteSpec explanation: its title, text and source. */
export interface Explanation {
  title: string;
  text: string;
  source: string;
}

/** A carried prose section's text, or nothing when the lexicon carries none. */
export function lexiconSection(lex: Lexicon, title: string): string {
  return lex.sections?.[title]?.text ?? "";
}

/** A carried prose section's source, or the fallback when the lexicon carries none. */
export function lexiconSectionSource(lex: Lexicon, title: string, fallback: string): string {
  return lex.sections?.[title]?.source || fallback;
}

/** A glossary term's definition, or nothing when the lexicon carries none. */
export function lexiconGlossaryDefinition(lex: Lexicon, term: string): string {
  return lex.glossary?.[String(term).toLowerCase()]?.definition ?? "";
}

/** A finding code's meaning, or nothing when the lexicon carries none. */
export function lexiconFindingMeaning(lex: Lexicon, code: string): string {
  return lex.findings?.[code]?.meaning ?? "";
}

/** A record type's label, or the type itself when the lexicon carries none. */
export function lexiconTypeName(lex: Lexicon, type: string): string {
  return lex.types?.[type]?.label || type;
}

/** A record type's plural, or its label, or the type itself. */
export function lexiconPluralName(lex: Lexicon, type: string): string {
  const entry = lex.types?.[type];
  return entry?.plural || entry?.label || type;
}

/** A record type's tag: its label in lower case. */
export function lexiconTypeTag(lex: Lexicon, type: string): string {
  return lexiconTypeName(lex, type).toLowerCase();
}

/** A record type's question, or nothing when the lexicon carries none. */
export function lexiconTypeQuestion(lex: Lexicon, type: string): string {
  return lex.types?.[type]?.question ?? "";
}

/** A field's LiteSpec label; an unknown field renders raw, never invented. */
export function lexiconFieldLabel(lex: Lexicon, field: string): string {
  return lex.fields?.[field] || field;
}

function carriersOf(carriers: string[] | string): string[] {
  if (Array.isArray(carriers)) return carriers;
  if (typeof carriers === "string") return carriers.split(",").map((part) => part.trim()).filter(Boolean);
  return [];
}

/** A link field's Links-table row name, dropping type and nesting prefixes. */
export function lexiconResolveField(lex: Lexicon, field: string): string | null {
  let candidate = field;
  const dot = candidate.indexOf(".");
  if (dot > 0 && lex.types?.[candidate.slice(0, dot)]) candidate = candidate.slice(dot + 1);
  const links = lex.links ?? [];
  for (;;) {
    if (links.some((link) => link.field === candidate)) return candidate;
    const nested = candidate.lastIndexOf(".");
    if (nested < 0) break;
    candidate = candidate.slice(0, nested);
  }
  // A leaf link field reads from its own row: journey steps name their actor.
  const leaf = field.slice(field.lastIndexOf(".") + 1);
  return leaf !== candidate && links.some((link) => link.field === leaf) ? leaf : null;
}

function linkRow(lex: Lexicon, field: string, carrierType: string | null) {
  const candidate = lexiconResolveField(lex, field);
  if (candidate === null) return null;
  const rows = (lex.links ?? []).filter((link) => link.field === candidate);
  if (rows.length === 0) return null;
  if (carrierType) {
    for (const row of rows) {
      const carriers = carriersOf(row.carriers);
      if (carriers.includes(carrierType)) return { ...row, carriers };
    }
  }
  const first = rows[0];
  if (first === undefined) return null;
  return { ...first, carriers: carriersOf(first.carriers) };
}

/** A link field's Links-table phrase, forward or inverse, for the carrier type. */
export function lexiconPhraseFor(
  lex: Lexicon,
  field: string,
  carrierType: string | null,
  dir: "forward" | "inverse",
): string {
  const row = linkRow(lex, field, carrierType);
  const text = row?.[dir];
  if (row === null || row === undefined || !text) return field;
  if (text.includes(" / ")) {
    const parts = text.split(" / ");
    const idx = carrierType ? row.carriers.indexOf(carrierType) : -1;
    const part = idx >= 0 ? parts[idx] : undefined;
    return (part ?? parts[0] ?? text).trim();
  }
  return text;
}

/** Whether a link field's inverse phrase varies by carrier. */
export function lexiconLinkVaries(lex: Lexicon, field: string): boolean {
  const candidate = lexiconResolveField(lex, field);
  if (candidate === null) return false;
  const rows = (lex.links ?? []).filter((link) => link.field === candidate);
  if (rows.some((link) => (link.forward ?? "").includes(" / ") || (link.inverse ?? "").includes(" / "))) return true;
  return dedupe(rows.map((link) => link.inverse ?? "")).length > 1;
}

/** The LiteSpec text that explains a key: a proof state, a check level, a glossary term or a record type. */
export function lexiconExplain(lex: Lexicon, key: string): Explanation | null {
  const stages = (): Explanation => ({
    title: "Completion Stages",
    text: lexiconSection(lex, "Completion Stages"),
    source: lexiconSectionSource(lex, "Completion Stages", "model.md, Completion Stages"),
  });
  const glossary = (title: string, term: string): Explanation => ({
    title,
    text: lexiconGlossaryDefinition(lex, term),
    source: "Glossary",
  });
  const levels = (): Explanation => ({
    title: "Check level",
    text: lexiconSection(lex, "Check Levels"),
    source: lexiconSectionSource(lex, "Check Levels", "model.md, Check Levels"),
  });
  const table: Record<string, () => Explanation> = {
    awaiting: stages,
    unconfirmed: stages,
    unproved: stages,
    passing: stages,
    failing: stages,
    indirect: stages,
    norun: stages,
    coded: () => glossary("Coded", "coded"),
    reachable: () => glossary("Reachable", "reachable"),
    G: levels,
    S: levels,
    A: levels,
    covered: () => glossary("Covered", "covered"),
    finding: () => glossary("Finding", "finding"),
  };
  let found: Explanation | null = table[key]?.() ?? null;
  if (found === null && key.startsWith("type:")) {
    const type = key.slice(5);
    const entry = lex.types?.[type];
    const definition = lexiconGlossaryDefinition(lex, lexiconTypeName(lex, type));
    if (entry) {
      const layerKey = String(entry.layer ?? "").split(",")[0]?.trim() ?? "";
      const layer = lex.layers?.[layerKey] ?? { label: "", writes: "" };
      found = {
        title: lexiconTypeName(lex, type),
        text:
          (entry.question ?? "") +
          (definition ? " " + definition : "") +
          (layer.writes ? " Written by " + layer.writes + "." : ""),
        source: "model.md, Record Types",
      };
    } else if (definition) found = { title: lexiconTypeName(lex, type), text: definition, source: "Glossary" };
  }
  if (found === null && key.startsWith("g:")) found = glossary(capitalize(key.slice(2)), key.slice(2));
  return found && found.text ? { ...found, text: capitalize(found.text) } : null;
}
