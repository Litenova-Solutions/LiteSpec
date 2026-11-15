// The record navigation: opening records from delegated links and history,
// keeping the address and history, and setting the page title. The App calls
// each function from one handler or effect; the search box and the shortcut
// keys open records through openRecord. History runs through popstate alone:
// the delegated click handler covers prose anchors, so no hashchange handler
// runs and one Back press selects exactly once.

import { book, stripStep } from "$lib/frame";

/** Section hashes never name a record: pills scroll without writing them, and pasted section links must not select a record either. */
function isSectionHash(id: string): boolean {
  return id.startsWith("b-") || id.startsWith("body-");
}

// Every selection request takes a token. When a stale request finishes after
// a newer one started, it reasserts the newest request instead of landing on
// the wrong record.
let navToken = 0;
let latestId: string | null = null;

/** Select through the token, pushing a history entry for a new move and none for a history move. */
async function guardedSelect(id: string, fromHistory: boolean, replace: boolean): Promise<void> {
  const mine = ++navToken;
  latestId = id;
  if (fromHistory) await book.select(id, { fromHistory: true });
  else if (replace) await book.select(id, { replace: true });
  else await book.select(id);
  if (mine !== navToken && latestId !== null && book.selected !== latestId) {
    await book.select(latestId, { fromHistory: true });
  }
}

/** Open a record the reader chose: select it, letting the store push the history entry. Rapid moves pass replace to coalesce into one entry. */
export function openRecord(id: string, opts: { replace?: boolean } = {}): Promise<void> {
  return guardedSelect(stripStep(id), false, opts.replace ?? false);
}

/**
 * The record id a click delegates to, if any. Data-go buttons and links come
 * first; then prose anchors the CLI renders as plain hashes. Section hashes
 * and anchors that name no record return null, so the browser handles them
 * natively (a section scroll, a skip link) instead of selecting a record.
 */
export function delegatedRecordId(event: MouseEvent): string | null {
  const target = event.target as Element | null;
  const goId = target?.closest?.("[data-go]")?.getAttribute("data-go");
  if (goId) return goId;
  const href = target?.closest?.('a[href^="#"]')?.getAttribute("href");
  if (!href || href.length < 2) return null;
  let frag = href.slice(1);
  try {
    frag = decodeURIComponent(frag);
  } catch {
    return null;
  }
  const record = stripStep(frag);
  if (!record || isSectionHash(record)) return null;
  if (!record.includes("/") && !book.has(record)) return null;
  return record;
}

/** Return to the top of the page; the App calls it when it opens the Intent tree. */
export function scrollToTop(): void {
  window.scrollTo(0, 0);
}

/** Name the open record in the document title, so tabs and history entries tell records apart. */
export function syncDocumentTitle(id: string | null): void {
  const project = book.man?.project?.title || book.man?.project?.id || "Book";
  if (!id) {
    document.title = project + " Book";
    return;
  }
  const tag = book.typeTag(book.typeOf(id));
  document.title = (tag ? tag + " " : "") + book.title(id) + " — " + project;
}

/** The id a history entry names, if it names one. */
function idFromState(state: unknown): string | null {
  if (typeof state !== "object" || state === null) return null;
  if (!("id" in state)) return null;
  const id: unknown = state.id;
  return typeof id === "string" ? id : null;
}

/** Re-assert the selection from a history entry's state. Returns the record it asserts, if any. */
export function reassertFromHistory(state: unknown): string | null {
  const raw = idFromState(state);
  if (!raw) return null;
  const id = stripStep(raw);
  if (!id || isSectionHash(id)) return null;
  void guardedSelect(id, true, false);
  return id;
}

/** Re-assert the selection from the hash, for entries written without state. Returns the record it asserts, if any. */
export function reassertFromHash(): string | null {
  let frag = location.hash.slice(1);
  try {
    frag = decodeURIComponent(frag);
  } catch {
    return null;
  }
  const id = stripStep(frag);
  if (!id || isSectionHash(id)) return null;
  void guardedSelect(id, true, false);
  return id;
}
