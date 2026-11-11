// The outline's own state: the filter panel and reveal requests from the tools,
// the search box and the shortcut keys. Expansion, closed groups and open
// layers stay on the book so they survive selection changes.
import { book } from "$lib/frame";
import type { LayerKey } from "$lib/frame";

class OutlineState {
  /** Whether the filter panel is open. */
  facetsOpen = $state(false);
  /** Bumped to ask the outline to reveal the selected row and focus it. */
  revealRequest = $state(0);

  requestReveal(): void {
    this.revealRequest++;
  }
}

/** The outline's own state; the panel consumes reveal requests. */
export const outlineState = new OutlineState();

/** The real records and record types in a layer, for the layer header and page. */
export function layerStats(layer: LayerKey): { ids: string[]; types: string[] } {
  void book.rev;
  const ids = book.order.filter((id) => book.layerOf[id] === layer && book.isReal(id));
  const types = [...new Set(ids.map((id) => book.typeOf(id)))];
  return { ids, types };
}
