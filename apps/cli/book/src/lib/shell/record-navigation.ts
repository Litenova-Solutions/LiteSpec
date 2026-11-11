// The record navigation: opening records from delegated links and history,
// returning to the top on selection, and keeping the document title. The App
// calls each function from one handler or effect.

import { book } from "$lib/frame";

/** Open a record the reader chose: select it, letting the store push the history entry. */
export function openRecord(id: string): void {
  void book.select(id);
}

/** The record id a click delegates to, from the closest data-go element, if any. */
export function delegatedRecordId(event: MouseEvent): string | null {
  const link = (event.target as Element | null)?.closest?.("[data-go]");
  return link?.getAttribute("data-go") ?? null;
}

/** Return to the top of the page; the App calls it on every selection. */
export function scrollToTop(): void {
  window.scrollTo(0, 0);
}

/** Keep the document title on the book's project. It takes the selection so the App effect re-runs with it. */
export function syncDocumentTitle(id: string | null): void {
  void id;
  const name = book.man?.project?.title || book.man?.project?.id;
  document.title = (name || "Book") + " Book";
}

/** The id a history entry names, if it names one. */
function idFromState(state: unknown): string | null {
  if (typeof state !== "object" || state === null) return null;
  if (!("id" in state)) return null;
  const id: unknown = state.id;
  return typeof id === "string" ? id : null;
}

/** Re-assert the selection after the reader moves through history. */
export function reassertFromHistory(state: unknown): void {
  const id = idFromState(state) || decodeURIComponent(location.hash.slice(1));
  if (id) void book.select(id, { fromHistory: true });
}

/** Re-assert the selection when the hash changes, for file:// where History state is thin. */
export function reassertFromHash(): void {
  const id = decodeURIComponent(location.hash.slice(1));
  if (id) void book.select(id, { fromHistory: true });
}
