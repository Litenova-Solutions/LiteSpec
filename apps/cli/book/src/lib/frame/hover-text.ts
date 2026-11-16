// One hover voice for the whole book: every hover and focus explanation has a
// title and a body shaped by these rules.
// - Title: the thing's name as the page shows it.
// - Body: one or two sentences, at most about 220 characters. Sentence one is
//   LiteSpec's own definition from the lexicon: a glossary definition, a record
//   type question, a link phrase, or a carried section paragraph, trimmed to its
//   first sentence. Sentence two, only when it adds something, says what it
//   means here, with the concrete record or count.
// - Present tense, third person, plain words, sentence case, ends with a
//   period, ASCII only. No "you", no "click", no "this record" without naming
//   it, no marketing.
// - No definition is typed here for a word LiteSpec defines; only the
//   sentence-two templates live in code. Source trailers ("Home: ...",
//   "Written by ...") never reach a body. A word LiteSpec defines nowhere
//   reads with its title only.

import type { Book } from "./book.svelte";
import type { Lexicon, ProofState } from "./shard-types";
import {
  lexiconExplain,
  lexiconFieldLabel,
  lexiconGlossaryDefinition,
  lexiconPhraseFor,
  lexiconPluralName,
  lexiconResolveField,
  lexiconSection,
  lexiconTypeName,
  lexiconTypeQuestion,
} from "./lexicon";
import { edgeMatches } from "./relations";
import { capitalize, formatLabel, pluralize } from "./text";
import { proofLabel } from "./proof-order";

/** One hover explanation: the title on line one, the body below it. */
export type Hover = { title: string; body: string };

/** A proof state word the book explains, for the hover title. */
const PROOF_STATES: readonly ProofState[] = [
  "passing",
  "failing",
  "awaiting",
  "unconfirmed",
  "unproved",
  "indirect",
  "norun",
];

function isProofState(key: string): key is ProofState {
  return (PROOF_STATES as readonly string[]).includes(key);
}

/** LiteSpec prose as hover text: backticks never read, quotes and dashes read straight, one line. */
function ascii(text: string): string {
  return text
    .replace(/`/g, "")
    .replace(/[\u2018\u2019]/g, "'")
    .replace(/[\u201c\u201d]/g, '"')
    .replace(/[\u2013\u2014]/g, "-")
    .replace(/\s+/g, " ")
    .replace(/[^\x20-\x7e]/g, "");
}

/** Lexicon prose without its source trailers: a "Home:" or "Written by" sentence never reads. */
function stripTrailers(text: string): string {
  return text
    .split(/(?<=[.?!])\s+/)
    .filter((sentence) => /^(Home:|Written by)\b/.test(sentence.trim()) === false)
    .join(" ")
    .trim();
}

/** The first sentence: the boundary skips a period after one letter, as in "e.g.". */
function firstSentence(text: string): string {
  const clean = text.trim().replace(/\s+/g, " ");
  const at = [...clean.matchAll(/[.?!](\s|$)/g)].find(
    (found) => found.index === undefined || /(\betc\.|\b[a-z]\.)$/i.test(clean.slice(0, found.index)) === false,
  );
  if (!at || at.index === undefined) return clean;
  return clean.slice(0, at.index + 1).trim();
}

/** A sentence reading with its final period. */
function finish(text: string): string {
  if (!text) return "";
  return /[.?!]$/.test(text) ? text : text + ".";
}

/** A body of at most about 220 characters, cut at a word boundary. */
function cap(text: string): string {
  if (text.length <= 220) return text;
  const cut = text.lastIndexOf(" ", 217);
  return (cut > 0 ? text.slice(0, cut) : text.slice(0, 217)).trim() + "...";
}

/** LiteSpec prose as a hover body: its first sentence, finished and capped. */
function clean(text: string): string {
  if (!text.trim()) return "";
  return cap(finish(firstSentence(ascii(stripTrailers(text)))));
}

/** A label as its singular glossary term: "Data classes" reads "data class". */
function singular(label: string): string {
  const lower = label.toLowerCase();
  if (lower.endsWith("ies")) return lower.slice(0, -3) + "y";
  if (/(ches|shes|sses|xes|zes)$/.test(lower)) return lower.slice(0, -2);
  if (lower.endsWith("s") && lower.endsWith("ss") === false) return lower.slice(0, -1);
  return lower;
}

/** A link phrase as the page shows it: capitalized, without a leading "is" or "are". */
function displayLabel(phrase: string): string {
  return capitalize(phrase.replace(/^(is|are)\s+/i, ""));
}

/** "a" or "an" for a type name; "use" reads with "a". */
function article(name: string): string {
  const lower = name.toLowerCase();
  if (/^(use|uni|one)\b/.test(lower)) return "a";
  return /^[aeiou]/.test(lower) ? "an" : "a";
}

/** A shown heading back to its link row: "Superseded by" reads the supersedes row from its target side. */
function reverseLink(lex: Lexicon, label: string): { field: string; dir: "out" | "in" } | null {
  const want = label.toLowerCase();
  for (const link of lex.links ?? []) {
    if (displayLabel(link.forward).toLowerCase() === want) return { field: link.field, dir: "out" };
  }
  for (const link of lex.links ?? []) {
    if (displayLabel(link.inverse).toLowerCase() === want) return { field: link.field, dir: "in" };
  }
  return null;
}

/** The first carrier type of a link row, or null when no row names the field. */
function firstCarrier(lex: Lexicon, field: string): string | null {
  const row = (lex.links ?? []).find((link) => link.field === field);
  const carriers = row?.carriers;
  if (Array.isArray(carriers)) return carriers[0] ?? null;
  if (typeof carriers === "string") return carriers.split(",").map((part) => part.trim()).filter(Boolean)[0] ?? null;
  return null;
}

/** A record's title in a sentence-two template. */
function recordTitle(book: Book, id: string): string {
  return book.title(id);
}

/** The one type every linked record shares, or null when they differ. */
function uniformType(book: Book, ids: string[]): string | null {
  const types = [...new Set(ids.map((id) => book.typeOf(id)))];
  return types.length === 1 ? (types[0] ?? null) : null;
}

/** "<Title> has <n> <records>.": the count with the shared type's plural, or bare records. */
function countSentence(book: Book, recordId: string, ids: string[]): string {
  const shared = uniformType(book, ids);
  const count =
    shared === null
      ? pluralize(ids.length, "record")
      : pluralize(ids.length, lexiconTypeName(book.lex, shared).toLowerCase(), lexiconPluralName(book.lex, shared).toLowerCase());
  return `${recordTitle(book, recordId)} has ${count}.`;
}

/** A link section's body from this record's side, with the linked count beside it. */
function linkSectionBody(
  book: Book,
  field: string,
  dir: "out" | "in",
  carrier: string | null,
  recordId: string,
  matches: string[],
): { title: string; body: string } {
  const lex = book.lex;
  const phrase = lexiconPhraseFor(lex, field, carrier, dir === "out" ? "forward" : "inverse");
  const title = displayLabel(phrase);
  const fromName = recordId ? lexiconTypeName(lex, book.typeOf(recordId)).toLowerCase() : dir === "out" && carrier ? carrier.replace(/-/g, " ") : "record";
  const subject = `${capitalize(article(fromName))} ${fromName}`;
  let first: string;
  if (dir === "out") {
    const shared = matches.length ? uniformType(book, matches) : null;
    const target = shared ? `${article(lexiconTypeName(lex, shared))} ${lexiconTypeName(lex, shared).toLowerCase()}` : "another record";
    first = `${subject} ${lexiconPhraseFor(lex, field, carrier, "forward")} ${target}.`;
  } else {
    const shared = matches.length ? uniformType(book, matches) : carrier;
    const other = shared ? `${article(shared.replace(/-/g, " "))} ${lexiconTypeName(lex, shared).toLowerCase()}` : "another record";
    first = `${subject} ${lexiconPhraseFor(lex, field, carrier, "inverse")} ${other}.`;
  }
  if (recordId === "" || matches.length === 0) return { title, body: ascii(first) };
  const both = `${first} ${countSentence(book, recordId, matches)}`;
  return { title, body: both.length <= 240 ? ascii(both) : ascii(first) };
}

/** A fixed section's body: its carried section text or its glossary definition, else its title only. */
function fixedSectionBody(book: Book, sectionKey: string, recordId: string): Hover {
  const lex = book.lex;
  const label = lexiconFieldLabel(lex, sectionKey);
  const lower = label.toLowerCase();
  // Three headings read with their title only: "Operations" and "Access" name
  // LiteSpec words of another sense (a route operation, a risk value), and a
  // goal's "Outcome" is measured delivery, not the operation ending the
  // glossary defines.
  if (lower === "operations" || lower === "access") return { title: label, body: "" };
  if (lower === "outcome" && recordId !== "" && book.typeOf(recordId) === "goal") return { title: label, body: "" };
  // The "On failure" section is the failure policy the glossary defines, and
  // the "Data" section of an aggregate is its aggregate data.
  const aliased =
    lower === "on failure"
      ? lexiconGlossaryDefinition(lex, "failure policy")
      : lower === "data" && recordId !== "" && book.typeOf(recordId) === "aggregate"
        ? lexiconGlossaryDefinition(lex, "aggregate data")
        : "";
  const text = lexiconSection(lex, label) || lexiconSection(lex, sectionKey) || aliased || lexiconGlossaryDefinition(lex, lower) || lexiconGlossaryDefinition(lex, singular(label)) || "";
  const body = clean(text);
  if (body === "" || label !== "Links" || recordId === "") return { title: label, body };
  const links = book.connectionsOf(recordId).length;
  if (links === 0) return { title: label, body };
  const both = `${body} ${recordTitle(book, recordId)} has ${pluralize(links, "link")}.`;
  return { title: label, body: both.length <= 240 ? both : body };
}

/** A record badge: its title, with "<Type name>. <summary or statement, trimmed to one sentence>". */
export function hoverRecord(book: Book, id: string): Hover {
  const record = book.get(id);
  const name = lexiconTypeName(book.lex, record?.type ?? book.typeOf(id));
  const fields = record?.fields ?? {};
  const summary = typeof fields["summary"] === "string" ? fields["summary"] : "";
  const statement = typeof fields["statement"] === "string" ? fields["statement"] : "";
  const lead = summary || statement;
  return { title: recordTitle(book, id), body: lead ? `${name}. ${clean(lead)}` : `${name}.` };
}

/** A record type badge: its name, with its glossary definition, else its model question. */
export function hoverType(book: Book, type: string): Hover {
  const lex = book.lex;
  const name = lexiconTypeName(lex, type);
  const definition = lexiconGlossaryDefinition(lex, name);
  const question = lexiconTypeQuestion(lex, type);
  return { title: capitalize(formatLabel(name)), body: definition ? clean(definition) : question ? cap(ascii(question.trim())) : "" };
}

/** A link label, or a link between two records: the phrase read from the first record's side. */
export function hoverRelation(book: Book, field: string, dir: "out" | "in", fromId: string, toId?: string): Hover {
  const lex = book.lex;
  const resolved = lexiconResolveField(lex, field) ?? field;
  const carrier = dir === "out" ? book.typeOf(fromId) : toId ? book.typeOf(toId) : firstCarrier(lex, resolved);
  const phrase = lexiconPhraseFor(lex, field, carrier, dir === "out" ? "forward" : "inverse");
  const title = displayLabel(phrase);
  const fromName = lexiconTypeName(lex, book.typeOf(fromId)).toLowerCase();
  const subject = `${capitalize(article(fromName))} ${fromName}`;
  let first: string;
  if (dir === "out") {
    const target = toId ? `${article(lexiconTypeName(lex, book.typeOf(toId)))} ${lexiconTypeName(lex, book.typeOf(toId)).toLowerCase()}` : "another record";
    first = `${subject} ${lexiconPhraseFor(lex, field, carrier, "forward")} ${target}.`;
  } else {
    const other = carrier ? `${article(carrier.replace(/-/g, " "))} ${lexiconTypeName(lex, carrier).toLowerCase()}` : "another record";
    first = `${subject} ${lexiconPhraseFor(lex, field, carrier, "inverse")} ${other}.`;
  }
  if (!toId) return { title, body: ascii(first) };
  const forward = lexiconPhraseFor(lex, field, carrier, "forward");
  const second = dir === "out" ? `${recordTitle(book, fromId)} ${forward} ${recordTitle(book, toId)}.` : `${recordTitle(book, toId)} ${forward} ${recordTitle(book, fromId)}.`;
  const both = `${first} ${second}`;
  return { title, body: both.length <= 240 ? ascii(both) : ascii(first) };
}

/** A page section heading: a link section reads its link from this record's side with its count, a fixed section reads its carried text. */
export function hoverSection(book: Book, sectionKey: string, recordId: string): Hover {
  const inbound = sectionKey.indexOf("Points at this: ") === 0;
  const key = inbound ? sectionKey.slice("Points at this: ".length) : sectionKey;
  const direct = lexiconResolveField(book.lex, key);
  if (direct !== null) return linkSection(book, direct, recordId, inbound ? "in" : null);
  const back = reverseLink(book.lex, lexiconFieldLabel(book.lex, sectionKey));
  if (back !== null) return linkSection(book, back.field, recordId, back.dir);
  return fixedSectionBody(book, sectionKey, recordId);
}

/** A link section: the field read from this record's side, in the forced direction or the record's own edges. */
function linkSection(book: Book, resolved: string, recordId: string, forced: "out" | "in" | null): Hover {
  const record = recordId ? book.get(recordId) : undefined;
  const parent = recordId ? book.parent(recordId) : null;
  const edges = (dir: "out" | "in"): string[] => {
    if (!record) return [];
    const raw = record[dir] ?? [];
    const out: string[] = [];
    for (const edge of raw) {
      const id = String(edge.id ?? "").split("#")[0] ?? "";
      if (id && id !== parent && edgeMatches(edge.key ?? "", resolved)) out.push(id);
    }
    return out;
  };
  const outs = edges("out");
  const ins = edges("in");
  const dir: "out" | "in" = forced ?? (outs.length > 0 ? "out" : ins.length > 0 ? "in" : "out");
  const carrier =
    dir === "out"
      ? recordId
        ? book.typeOf(recordId)
        : firstCarrier(book.lex, resolved)
      : ins.length > 0
        ? book.typeOf(ins[0] ?? "")
        : firstCarrier(book.lex, resolved);
  return linkSectionBody(book, resolved, dir, carrier, recordId, dir === "out" ? outs : ins);
}

/** A state, level, kind or classification badge: the page-shown name with its lexicon sentence. */
export function hoverState(book: Book, key: string): Hover {
  if (key.indexOf("type:") === 0) return hoverType(book, key.slice("type:".length));
  const found = lexiconExplain(book.lex, key);
  if (found === null) {
    const term = key.indexOf("g:") === 0 ? key.slice("g:".length) : key;
    return { title: capitalize(term), body: "" };
  }
  return { title: stateTitle(book, key, found.title), body: clean(found.text) };
}

/** A badge's name as the page shows it: the proof label, the level pill text, or the term. */
function stateTitle(book: Book, key: string, fallback: string): string {
  if (isProofState(key)) return proofLabel(key);
  // The level pill text FindingsBlock prints through checkLevelText.
  if (key === "G") return "Blocks a release";
  if (key === "S") return "Blocks when qualified";
  if (key === "A") return "Advice";
  if (key.indexOf("g:") === 0) return book.lex.glossary?.[key.slice("g:".length).toLowerCase()]?.term || fallback;
  return fallback;
}
