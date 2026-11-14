// The outline's own state: the filter panel and reveal requests from the tools,
// the search box and the shortcut keys. Expansion, closed groups and open
// layers stay on the book so they survive selection changes.

import { book } from "$lib/frame";

class OutlineState {
  /** Whether the filter panel is open. */
  facetsOpen = $state(false);
  /** Bumped to ask the outline to reveal the selected row and focus it. */
  revealRequest = $state(0);
  /** Runs after each record the reader chooses in the outline, the record already selected included. The App sets it to close the drawer. */
  onChoose: (() => void) | null = null;

  requestReveal(): void {
    this.revealRequest++;
  }

  /** Selects a record the reader chose in the outline, then tells the App. */
  choose(id: string): void {
    void book.select(id);
    this.onChoose?.();
  }
}

/** The outline's own state; the panel consumes reveal requests. */
export const outlineState = new OutlineState();
