import { createShardLoader, type ShardLoader } from "./shard-loader";
import { Book } from "./book.svelte";

declare global {
  interface Window { BookData: ShardLoader }
}

// Each shard script calls BookData.put, so the loader is global before any shard loads.
window.BookData = createShardLoader(document);

/** The one book the page shows; components read and change it directly. */
export const book = new Book(window.BookData);

export { Book, GROUPS, PLABEL, type Connection, type Explanation, type GroupKey, type View } from "./book.svelte";
export { capitalize, dedupe, formatCount, formatLabel, pluralize, stripStep } from "./text";
export { cn, type WithElementRef, type WithoutChild, type WithoutChildren, type WithoutChildrenOrChild } from "./cn";
export { duration, editorHref, esc, inline, safeHref, srcHref } from "./html";
export { fixtureBook } from "./fixture";
export type {
  BookRecord, BookTest, ProofState, RecordFields, SecurityItem, SourceRef,
} from "./shard-types";
