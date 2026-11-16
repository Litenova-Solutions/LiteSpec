// The Carbon HTML the record page's data tables render as strings: cells,
// code spans, state tags and record links for the rows DataTable takes.
import { capitalize, esc, stripStep } from "$lib/frame";
import type { Book, RecordFields } from "$lib/frame";

const CELL = "border-t border-border-subtle bg-layer px-4 py-3 align-top";

/** A body cell of a data table. */
export function td(html: string): string {
  return `<td class="${CELL}">${html}</td>`;
}

/** A row header cell of a data table. */
export function thRow(html: string): string {
  return `<th scope="row" class="${CELL} text-left font-normal">${html}</th>`;
}

/** An inline code span. */
export function codeOf(value: unknown): string {
  return `<code class="bg-layer px-1 font-mono text-xs">${esc(value)}</code>`;
}

/** Helper text below a cell value. */
export function muted(html: string): string {
  return `<span class="block text-xs tracking-[0.32px] text-text-helper">${html}</span>`;
}

/** Helper text inside a cell value. */
export function mutedInline(html: string): string {
  return `<span class="text-xs tracking-[0.32px] text-text-helper">${html}</span>`;
}

type TagTone = "green" | "red" | "warm" | "teal" | "cool" | "blue" | "purple" | "outline";

const TAG_TONE: Record<TagTone, string> = {
  green: "bg-tag-green text-tag-green-foreground",
  red: "bg-tag-red text-tag-red-foreground",
  warm: "bg-tag-warm text-tag-warm-foreground",
  teal: "bg-tag-teal text-tag-teal-foreground",
  cool: "bg-tag-cool text-tag-cool-foreground",
  blue: "bg-tag-blue text-tag-blue-foreground",
  purple: "bg-tag-purple text-tag-purple-foreground",
  outline: "ring-1 ring-inset ring-border-strong text-text-secondary",
};

/** A state tag, small inside tables and summaries, full size in the results table. A tag with an explanation key opens it on hover, focus or click. */
export function tag(text: string, tone: TagTone, size: "sm" | "md" = "sm", explain?: string): string {
  const height = size === "sm" ? "h-[18px]" : "h-6";
  const ex = explain ? ` data-explain="${esc(explain)}" tabindex="0"` : "";
  const cursor = explain ? " cursor-help" : "";
  return `<span class="inline-flex ${height} items-center gap-1 rounded-full px-2 text-xs tracking-[0.32px]${cursor} ${TAG_TONE[tone]}"${ex}>${esc(text)}</span>`;
}

const CLS_TONE: Record<string, TagTone> = {
  public: "cool",
  internal: "cool",
  personal: "purple",
  sensitive: "red",
  confidential: "red",
};

/** A classification as a small tag, or plain text when the book names none it knows. */
export function clsTag(c: unknown): string {
  const key = typeof c === "string" ? c : "";
  if (!key) return "";
  const tone = CLS_TONE[key];
  if (!tone) return esc(capitalize(key));
  return tag(key, tone, "sm", "g:" + key);
}

/** A record named inside a table cell: its title as a link, or the bare identifier. */
export function refLink(book: Book, id: string, label?: string): string {
  const b = stripStep(id);
  if (book.has(b)) {
    return `<a href="#${encodeURIComponent(b)}" data-go="${esc(b)}" class="text-link hover:underline">${esc(label ?? book.title(b))}</a>`;
  }
  return `<span class="font-mono text-xs text-text-secondary">${esc(label ?? b)}</span>`;
}

/** A result outcome as a tag: success reads green, a refusal reads red. Each explains from the glossary. */
export function outcomeTag(outcome: string): string {
  return outcome === "success"
    ? tag("Success", "green", "md", "g:outcome")
    : tag(capitalize(outcome || "refusal"), "red", "md", "g:refusal");
}

/** A field's type with the record its values come from below it. */
export function fieldType(book: Book, x: RecordFields): string {
  const type = x.type === undefined || x.type === null ? "" : esc(x.type);
  let of = "";
  if (Array.isArray(x.identifies)) of = x.identifies.length ? esc(x.identifies.map(String).join(", ")) : "";
  else if (x.identifies !== undefined && x.identifies !== null && String(x.identifies) !== "") {
    const id = stripStep(String(x.identifies));
    of = book.has(id) ? esc(book.title(id)) : esc(id);
  }
  return type + (of ? muted(of) : "");
}

/** An idempotency key, scope and reason as one helper line. */
export function idemHint(i: RecordFields): string {
  return [
    i.key ? codeOf(i.key) : "",
    i.scope ? "scoped to " + esc(i.scope) : "",
    i.reason ? esc(i.reason) : "",
  ]
    .filter(Boolean)
    .join(" &middot; ");
}
