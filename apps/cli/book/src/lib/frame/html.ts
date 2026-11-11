// Text the book renders as HTML: inline markdown from record fields, editor links
// and provision links. Record fields and explanations arrive rendered by the CLI;
// these helpers cover only the short strings the client still formats itself.
//
// A record link in rendered HTML is a button with data-go; a term with an
// explanation carries data-explain. The App handles both by delegation.

import type { Book } from "./book.svelte";
import type { SourceRef } from "./shard-types";
import { stripStep } from "./text";

export function esc(s: unknown) {
  return String(s ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function editorScheme(book: Book) {
  const t = book.man?.editor;
  if (!t || t === "none") return null;
  return /^([A-Za-z][A-Za-z0-9+.-]*):/.exec(t.trim())?.[1].toLowerCase() ?? null;
}

/** A link target the book may open: http, https, mailto, the editor scheme, or a relative path. */
export function safeHref(book: Book, value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const text = String(value).trim();
  // eslint-disable-next-line no-control-regex
  if (!text || /[\u0000-\u001f\u007f]/.test(text)) return null;
  if (text.startsWith("#")) return text;
  const scheme = /^([A-Za-z][A-Za-z0-9+.-]*):/.exec(text);
  if (scheme) {
    const name = scheme[1].toLowerCase();
    return name === "http" || name === "https" || name === "mailto" || name === editorScheme(book) ? text : null;
  }
  if (text.startsWith("//") || text.startsWith("\\\\")) return null;
  return text;
}

export function editorHref(book: Book, root: string, path: string, line?: number | null) {
  const t = book.man?.editor;
  if (!t || t === "none" || !path) return null;
  let href = t.split("{root}").join(root || "").split("{path}").join(path);
  href = line ? href.split("{line}").join(String(line)) : href.replace(/:{line}/g, "").split("{line}").join("");
  return safeHref(book, href);
}

export function srcHref(book: Book, src: SourceRef | null | undefined) {
  if (!src?.path) return null;
  const roots = book.man?.roots;
  return editorHref(book, (src.litespec ? roots?.litespec : roots?.consumer) ?? "", src.path, src.line ?? null);
}

/** A LiteSpec provision ID opens its standards page in the LiteSpec checkout. */
export function provisionHref(book: Book, provision: string) {
  const m = /^litespec\/(.+)#(.+)$/.exec(provision);
  return m ? editorHref(book, book.man?.roots?.litespec ?? "", "docs/" + m[1] + ".md") : null;
}

export function resolvePath(from: string, rel: string) {
  const parts = from.split("/").slice(0, -1);
  for (const p of rel.split("/")) {
    if (p === "..") parts.pop();
    else if (p && p !== ".") parts.push(p);
  }
  return parts.join("/");
}

/** An ISO 8601 duration in words: P2D reads "2 days". */
export function duration(iso: string) {
  const m = /^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?)?$/.exec(String(iso ?? ""));
  if (!m) return String(iso ?? "");
  const words = (n: string | undefined, unit: string) => (n ? [+n + " " + unit + (+n === 1 ? "" : "s")] : []);
  return [...words(m[1], "day"), ...words(m[2], "hour"), ...words(m[3], "minute"), ...words(m[4], "second")].join(" ") || iso;
}

/**
 * Inline markdown from a record field: code spans that name a record become record
 * links, bold, emphasis, and links. A relative link resolves against the record's
 * own file and opens in the editor.
 */
export function inline(book: Book, s: unknown, curSrc?: SourceRef | null) {
  const recordLink = (id: string, text: string) =>
    `<button type="button" class="book-ref" data-go="${esc(id)}">${text}</button>`;
  let out = esc(s);
  out = out.replace(/(^|[\s(])(https?:\/\/[^\s<)]+)/g, (_m, pre, url) => pre + "[" + url.replace(/^https?:\/\/(www\.)?/, "") + "](" + url + ")");
  out = out.replace(/`([^`]+)`/g, (_m, c) => {
    const id = stripStep(c.replace(/&amp;/g, "&"));
    return book.has(id) ? recordLink(id, esc(book.title(id))) : "<code>" + c + "</code>";
  });
  out = out.replace(/\*\*([^*]+)\*\*/g, "<b>$1</b>").replace(/(^|[^*])\*([^*]+)\*/g, "$1<em>$2</em>");
  out = out.replace(/\[([^\]]+)\]\((https?:[^)\s]+)\)/g, (_m, text, href) => {
    const direct = safeHref(book, href);
    return direct ? `<a href="${esc(direct)}" target="_blank" rel="noopener">${text}</a>` : text;
  });
  return out.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (_m, text, href: string) => {
    if (/^[A-Za-z][A-Za-z0-9+.-]*:/.test(href)) {
      const external = safeHref(book, href);
      return external ? `<a href="${esc(external)}" target="_blank" rel="noopener">${text}</a>` : text;
    }
    const bid = href.split("#")[0];
    if (/^[a-z][a-z-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(bid)) return book.has(bid) ? recordLink(bid, text) : text;
    if (!curSrc?.path) return text;
    const target = srcHref(book, { path: resolvePath(curSrc.path, bid), line: null, litespec: curSrc.litespec });
    return target ? `<a href="${esc(target)}" title="Open ${esc(href)} in the editor">${text}</a>` : text;
  });
}
