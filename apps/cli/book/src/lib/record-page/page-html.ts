// Record HTML builders: the links, cells and badges the record page renders
// as HTML strings. Record links carry data-go, which the App opens,
// and explanations carry data-explain, which the ExplainLayer shows.

import { esc, formatLabel, recordRefHtml, safeHref, stripStep } from "$lib/frame";
import type { Book } from "$lib/frame";

/** An inline code span, as book.js rendered .code. */
export const codeCls =
  "inline-block rounded-sm border border-slate-200 bg-slate-100 px-2 max-w-full font-mono text-xs font-medium break-all text-slate-600";

/** A record named inside a sentence: its title as a link, or the bare identifier. */
export function inlineRefHtml(book: Book, id: string, label?: string) {
  const b = stripStep(id);
  if (book.has(b)) return recordRefHtml(book, b, label);
  return `<span class="font-mono text-sm text-slate-600" title="No record with this identifier is in the book">${esc(label ?? b)}</span>`;
}

/** A table cell, as book.js rendered td. */
export function tdHtml(html: string, cls = "") {
  const extra =
    cls === "nm"
      ? " font-mono text-xs font-medium whitespace-nowrap w-[1%]"
      : cls === "mut"
        ? " text-slate-600"
        : cls === "num"
          ? " text-right tabular-nums"
          : "";
  return `<td class="border-b border-slate-200 px-4 py-2 align-top text-sm${extra}">${html}</td>`;
}

/** A fact: book.js rendered every badge as plain emphasized text. */
export function badgeHtml(text: unknown) {
  return `<span class="font-medium text-slate-900">${esc(text)}</span>`;
}

/** A LiteSpec provision ID as code: LiteSpec's own pages are plain text, never a link. */
export function provisionHtml(p: unknown) {
  return `<span class="${codeCls}">${esc(p)}</span>`;
}

export function linkOrTextHtml(book: Book, value: unknown) {
  const href = safeHref(book, value);
  return href ? `<a href="${esc(href)}">${esc(value)}</a>` : esc(value);
}

export function opRefHtml(book: Book, op: unknown) {
  const b = stripStep(op);
  const tail = String(op ?? "").split("#")[1];
  return inlineRefHtml(book, b) + (tail ? ` <span class="font-mono text-xs text-slate-600">${esc(formatLabel(tail))}</span>` : "");
}
