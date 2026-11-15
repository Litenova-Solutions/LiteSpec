// Text the book renders as HTML: inline markdown from record fields and source
// links. Record fields and explanations arrive rendered by the CLI;
// these helpers cover only the short strings the client still formats itself.
//
// A record link in rendered HTML is an anchor with data-go; a term with an
// explanation carries data-explain. The App handles both by delegation.

import type { Book } from "./book.svelte";
import type { SourceRef } from "./shard-types";
import { stripStep } from "./text";

export function esc(s: unknown) {
  return String(s ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function linkScheme(book: Book) {
  const t = book.man?.links;
  if (!t) return null;
  const scheme = /^([A-Za-z][A-Za-z0-9+.-]*):/.exec(t.trim())?.[1];
  return scheme ? scheme.toLowerCase() : null;
}

/** A link target the book may open: http, https, mailto, the source link scheme, or a relative path. */
export function safeHref(book: Book, value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const text = String(value).trim();
  // eslint-disable-next-line no-control-regex
  if (!text || /[\u0000-\u001f\u007f]/.test(text)) return null;
  if (text.startsWith("#")) return text;
  const scheme = /^([A-Za-z][A-Za-z0-9+.-]*):/.exec(text);
  if (scheme) {
    const name = (scheme[1] ?? "").toLowerCase();
    return name === "http" || name === "https" || name === "mailto" || name === linkScheme(book) ? text : null;
  }
  if (text.startsWith("//") || text.startsWith("\\\\")) return null;
  return text;
}

/** A source link: the manifest's template with the path and line filled in, or null when the book links no source. */
export function sourceLinkHref(book: Book, path: string, line?: number | null) {
  const t = book.man?.links;
  if (!t || !path) return null;
  let href = t.split("{path}").join(path);
  href = line ? href.split("{line}").join(String(line)) : href.replace(/:{line}/g, "").split("{line}").join("");
  return safeHref(book, href);
}

/** The source link of a record's file and line; a LiteSpec release file is plain text, never a link. */
export function srcHref(book: Book, src: SourceRef | null | undefined) {
  if (!src?.path || src.litespec) return null;
  return sourceLinkHref(book, src.path, src.line ?? null);
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

/** Escaped text back to raw, for a label the shared link builder escapes again. */
function unesc(s: string): string {
  return s.replace(/&quot;/g, '"').replace(/&gt;/g, ">").replace(/&lt;/g, "<").replace(/&amp;/g, "&");
}

/** One record link in rendered HTML: an anchor the App opens by delegation. */
export function recordRefHtml(book: Book, id: string, label?: string): string {
  const target = stripStep(id);
  return `<a href="#${esc(target)}" class="book-ref text-link hover:underline" data-go="${esc(target)}">${esc(label ?? book.title(target))}</a>`;
}

/**
 * Inline markdown from a record field: code spans that name a record become record
 * links, bold, emphasis, and links. A relative link resolves against the record's
 * own file and opens as a source link.
 */
export function inline(book: Book, s: unknown, curSrc?: SourceRef | null) {
  let out = esc(s);
  out = out.replace(/(^|[\s(])(https?:\/\/[^\s<)]+)/g, (_m, pre, url) => pre + "[" + url.replace(/^https?:\/\/(www\.)?/, "") + "](" + url + ")");
  out = out.replace(/`([^`]+)`/g, (_m, c) => {
    const id = stripStep(c.replace(/&amp;/g, "&"));
    return book.has(id) ? recordRefHtml(book, id) : "<code>" + c + "</code>";
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
    const bid = href.split("#")[0] ?? "";
    if (/^[a-z][a-z-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(bid)) return book.has(bid) ? recordRefHtml(book, bid, unesc(text)) : text;
    if (!curSrc?.path) return text;
    const target = srcHref(book, { path: resolvePath(curSrc.path, bid), line: null, litespec: curSrc.litespec });
    return target ? `<a href="${esc(target)}">${text}</a>` : text;
  });
}
