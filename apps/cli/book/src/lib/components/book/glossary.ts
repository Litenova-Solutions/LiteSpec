import { book } from "$lib/book";

// Links the first use of each glossary term in a block of prose: to the term
// record when the book holds one, else to its glossary definition.
function terms() {
  const out = new Set<string>();
  for (const [k, g] of Object.entries(book.lex.glossary ?? {})) {
    const term = (g as { term?: string }).term || k;
    if (term && term.length > 2) out.add(term);
  }
  for (const r of Object.values(book.rec)) if (r.type === "term" && r.title) out.add(r.title);
  return [...out].sort((a, b) => b.length - a.length);
}

function termRecordId(term: string) {
  const wanted = term.toLowerCase();
  for (const [id, r] of Object.entries(book.rec)) if (r.type === "term" && r.title.toLowerCase() === wanted) return id;
  return null;
}

function link(root: HTMLElement) {
  const list = terms();
  if (!list.length) return;
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
  while (walker.nextNode()) nodes.push(walker.currentNode as Text);
  for (const node of nodes) {
    const text = node.nodeValue ?? "";
    const lower = text.toLowerCase();
    let match: string | null = null;
    let at = -1;
    for (const term of list) {
      if (linked.has(term.toLowerCase())) continue;
      const idx = lower.indexOf(term.toLowerCase());
      if (idx >= 0 && (at < 0 || idx < at)) {
        match = term;
        at = idx;
      }
    }
    if (!match) continue;
    linked.add(match.toLowerCase());
    const el = document.createElement("button");
    el.type = "button";
    el.className = "book-gloss";
    const rid = termRecordId(match);
    if (rid) el.dataset.go = rid;
    else el.dataset.explain = "g:" + match.toLowerCase();
    el.textContent = text.slice(at, at + match.length);
    const frag = document.createDocumentFragment();
    if (at > 0) frag.append(text.slice(0, at));
    frag.append(el);
    if (at + match.length < text.length) frag.append(text.slice(at + match.length));
    node.replaceWith(frag);
  }
}

/** Svelte action: link glossary terms in the node's prose, again whenever the HTML changes. */
export function glossary(node: HTMLElement, _html: string) {
  link(node);
  return {
    update() {
      link(node);
    },
  };
}
