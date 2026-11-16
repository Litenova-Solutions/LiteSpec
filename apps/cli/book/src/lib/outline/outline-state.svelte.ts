// The outline's own state: the filter panel and reveal requests from the tools,
// the search box and the shortcut keys. Expansion, closed groups and open
// layers stay on the book so they survive selection changes.

import { book } from "$lib/frame";
import { expandChain as expandChainOf, findingAnchor, findingSubject } from "./outline-rows";

class OutlineState {
  /** Whether the filter panel is open. */
  facetsOpen = $state(false);
  /** Bumped to ask the outline to reveal the selected row and focus it. */
  revealRequest = $state(0);
  /** The record the last outline click chose; the panel skips its follow-selection scroll for it. */
  fromOutline = $state<string | null>(null);
  /** The navigation filter text; the tree narrows to matching rows while it holds text. */
  navFilter = $state("");

  requestReveal(): void {
    this.revealRequest++;
  }

  /** Selects a record the reader chose in the outline, leaving focus in the tree. A finding row opens its subject record and scrolls the page to that finding. */
  choose(id: string): void {
    const anchor = findingAnchor(id);
    const subject = anchor ? findingSubject(book, id) : "";
    if (anchor && subject) {
      this.fromOutline = subject;
      void book.select(subject, { focusTitle: false }).then(() => {
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            document.getElementById(anchor)?.scrollIntoView();
          });
        });
      });
      return;
    }
    this.fromOutline = id;
    void book.select(id, { focusTitle: false });
  }

  /** Opens the chain above a record without closing anything else. */
  expandChain(id: string): void {
    expandChainOf(book, id);
  }
}

/** The outline's own state; the panel consumes reveal requests. */
export const outlineState = new OutlineState();
