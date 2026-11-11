// Record HTML builders: the links, cells and badges the record page renders
// as HTML strings. Record links carry data-go buttons, which the App opens,
// and explanations carry data-explain, which the ExplainLayer shows.

import { book, editorHref, esc, formatLabel, safeHref, stripStep } from "$lib/frame";

/** An inline code span, as book.js rendered .code. */
export const codeCls =
  "inline-block rounded border border-border bg-muted px-1.5 py-px font-mono text-xs font-medium whitespace-nowrap text-subtle-foreground";

/** A record named inside a sentence: its title as a link, or the bare identifier. */
export function inlineRefHtml(id: string, label?: string) {
  const b = stripStep(id);
  if (book.has(b))
    return `<button type="button" class="underline decoration-strong underline-offset-2 hover:decoration-foreground" data-go="${esc(b)}">${esc(label ?? book.title(b))}</button>`;
  return `<span class="font-mono text-[0.9em] text-muted-foreground" title="No record with this identifier is in the book">${esc(label ?? b)}</span>`;
}

/** A table cell, as book.js rendered td. */
export function tdHtml(html: string, cls = "") {
  const extra =
    cls === "nm"
      ? " font-mono text-[0.8125rem] font-medium whitespace-nowrap w-[1%]"
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

/** A LiteSpec provision ID opens its standards page in the LiteSpec checkout. */
export function provisionLinkHtml(p: unknown) {
  const m = /^litespec\/(.+)#(.+)$/.exec(String(p ?? ""));
  if (!m) return `<span class="${codeCls}">${esc(p)}</span>`;
  const href = editorHref(book, book.man?.roots?.litespec ?? "", "docs/" + (m[1] ?? "") + ".md");
  return href
    ? `<a class="${codeCls} hover:underline" href="${esc(href)}" title="Open the provision in LiteSpec">${esc(p)}</a>`
    : `<span class="${codeCls}">${esc(p)}</span>`;
}

export function linkOrTextHtml(value: unknown) {
  const href = safeHref(book, value);
  return href ? `<a href="${esc(href)}">${esc(value)}</a>` : esc(value);
}

export function opRefHtml(op: unknown) {
  const b = stripStep(op);
  const tail = String(op ?? "").split("#")[1];
  return inlineRefHtml(b) + (tail ? ` <span class="font-mono text-[0.6875rem] text-muted-foreground">${esc(formatLabel(tail))}</span>` : "");
}
