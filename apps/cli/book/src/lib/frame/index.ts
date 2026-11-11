import { createShardLoader, type ShardLoader } from "./shard-loader";
import { Book } from "./book.svelte";

declare global {
  interface Window {
    BookData: ShardLoader;
  }
}

// Each shard script calls BookData.put, so the loader is global before any shard loads.
window.BookData = createShardLoader(document);

/** The one book the page shows; components read and change it directly. */
export const book = new Book(window.BookData);

export { Book, type Connection, type Explanation } from "./book.svelte";
export { capitalize, dedupe, formatCount, formatLabel, pluralize, stripStep } from "./text";
export { cn, type WithElementRef, type WithoutChild, type WithoutChildren, type WithoutChildrenOrChild } from "./cn";
export { duration, editorHref, esc, inline, recordRefHtml, safeHref, srcHref } from "./html";
export { fixtureBook } from "./fixture";
export { proofLabel, proofRank, proofWord } from "./proof-order";
export { layerStats } from "./relations";
export {
  completionLines,
  coveragePercent,
  type CompletionLine,
  type CompletionMark,
} from "./completion";
export { copyText } from "./clipboard";
export { theme } from "./theme.svelte";
export { preferences } from "./preferences.svelte";
export { FLOW_TYPES, MODULE_MEMBER_TYPES, isFlowType } from "./shard-types";
export type {
  BookRecord,
  BookTest,
  CaptureState,
  CheckLevel,
  ImplKind,
  LayerKey,
  LoadState,
  OutlineKind,
  ProofState,
  RecordFields,
  SecurityItem,
  SortKey,
  SourceRef,
  View,
} from "./shard-types";
