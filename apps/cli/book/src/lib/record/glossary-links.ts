import type { Action } from "svelte/action";
import { book } from "$lib/frame";

// Links the first use of each glossary term in a block of prose: to the term
// record when the book holds one, else to its glossary definition. One matcher
// is built per data revision and shared by every block on the page.

export interface TermEntry {
  term: string;
  recordId: string | null;
}

export interface TermMatcher {
  /** One longest-first alternation over every term, matched case-insensitively. */
  pattern: RegExp;
  /** The term record per lower-cased term, or null when the book holds none. */
  recordOf: Map<string, string | null>;
}

function escapeTerm(term: string): string {
  return term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** One matcher over the entries: terms of two letters or fewer never link. */
export function createTermMatcher(entries: TermEntry[]): TermMatcher | null {
  const seen = new Set<string>();
  const terms: TermEntry[] = [];
  for (const entry of entries) {
    const key = entry.term.toLowerCase();
    if (entry.term.length <= 2 || seen.has(key)) continue;
    seen.add(key);
    terms.push(entry);
  }
  terms.sort((a, b) => b.term.length - a.term.length);
  if (!terms.length) return null;
  return {
    // A term links only as a whole word, with an optional plural ending, so
    // "book" never links inside "unbooked".
    pattern: new RegExp(String.raw`(?<![\p{L}\p{N}-])(` + terms.map((t) => escapeTerm(t.term)).join("|") + String.raw`)(?:e?s)?(?![\p{L}\p{N}])`, "giu"),
    recordOf: new Map(terms.map((t) => [t.term.toLowerCase(), t.recordId])),
  };
}

let cached: { rev: number; matcher: TermMatcher | null } | null = null;

function currentMatcher(): TermMatcher | null {
  if (cached && cached.rev === book.rev) return cached.matcher;
  const titles = new Map<string, { title: string; id: string }>();
  for (const [id, r] of Object.entries(book.rec)) {
    if (r.type !== "term" || !r.title) continue;
    const key = r.title.toLowerCase();
    if (!titles.has(key)) titles.set(key, { title: r.title, id });
  }
  const entries: TermEntry[] = [];
  for (const [k, g] of Object.entries(book.lex.glossary ?? {})) {
    const term = g.term || k;
    if (term) entries.push({ term, recordId: titles.get(term.toLowerCase())?.id ?? null });
  }
  for (const { title, id } of titles.values()) entries.push({ term: title, recordId: id });
  const matcher = createTermMatcher(entries);
  cached = { rev: book.rev, matcher };
  return matcher;
}

function link(root: HTMLElement): void {
  const matcher = currentMatcher();
  if (!matcher) return;
  const linked = new Set<string>();
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
    acceptNode(node) {
      if (!node.nodeValue?.trim()) return NodeFilter.FILTER_REJECT;
      for (let p = node.parentElement; p && p !== root; p = p.parentElement) {
        if (/^(A|BUTTON|CODE|PRE)$/.test(p.tagName) || p.hasAttribute("data-go")) return NodeFilter.FILTER_REJECT;
      }
      return NodeFilter.FILTER_ACCEPT;
    },
  });
  const nodes: Text[] = [];
  while (walker.nextNode()) {
    const current = walker.currentNode;
    if (current instanceof Text) nodes.push(current);
  }
  for (const node of nodes) {
    const text = node.nodeValue ?? "";
    matcher.pattern.lastIndex = 0;
    let at = -1;
    let length = 0;
    let key = "";
    let found: RegExpExecArray | null;
    while ((found = matcher.pattern.exec(text)) !== null) {
      const candidate = (found[1] ?? found[0]).toLowerCase();
      if (!linked.has(candidate)) {
        at = found.index;
        length = found[0].length;
        key = candidate;
        linked.add(candidate);
        break;
      }
      if (found[0].length === 0) break;
    }
    if (at < 0) continue;
    const rid = matcher.recordOf.get(key) ?? null;
    let el: HTMLElement;
    if (rid) {
      const anchor = document.createElement("a");
      anchor.href = "#" + encodeURIComponent(rid);
      anchor.dataset.go = rid;
      el = anchor;
    } else {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.dataset.explain = "g:" + key;
      el = btn;
    }
    el.className = "book-glossary";
    el.textContent = text.slice(at, at + length);
    const frag = document.createDocumentFragment();
    if (at > 0) frag.append(text.slice(0, at));
    frag.append(el);
    if (at + length < text.length) frag.append(text.slice(at + length));
    node.replaceWith(frag);
  }
}

/** Svelte action: link glossary terms in the node's prose, again only when the HTML changes. */
export const glossaryLinks: Action<HTMLElement, string> = (node, html) => {
  link(node);
  let last = html ?? "";
  return {
    update(value) {
      if ((value ?? "") === last) return;
      last = value ?? "";
      link(node);
    },
  };
};
