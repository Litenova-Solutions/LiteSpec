// The outline's own state: the filter panel and reveal requests from the tools,
// the search box and the shortcut keys. Expansion, closed groups and open
// layers stay on the book so they survive selection changes.

import { book } from "$lib/frame";
import { expandChain as expandChainOf } from "./outline-rows";

class OutlineState {
  /** Whether the filter panel is open. */
  facetsOpen = $state(false);
  /** Bumped to ask the outline to reveal the selected row and focus it. */
  revealRequest = $state(0);
  /** The record the last outline click chose; the panel skips its follow-selection scroll for it. */
  fromOutline = $state<string | null>(null);

  requestReveal(): void {
    this.revealRequest++;
  }

  /** Selects a record the reader chose in the outline, leaving focus in the tree. */
  choose(id: string): void {
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
