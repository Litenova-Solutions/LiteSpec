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

export { Book, type Explanation } from "./book.svelte";
export { hoverRecord, hoverType, hoverRelation, hoverSection, hoverState, type Hover } from "./hover-text";
export { capitalize, dedupe, formatCount, formatLabel, pluralize, stripStep } from "./text";
export { cn, type WithElementRef, type WithoutChild, type WithoutChildrenOrChild } from "./cn";
export { duration, esc, inline, recordRefHtml, safeHref, sourceLinkHref, srcHref } from "./html";
export { startLive } from "./live";
export { proofLabel, proofRank, proofWord } from "./proof-order";
export { layerStats } from "./relations";
export { completionLines, coveragePercent, freshnessLine, stageLine } from "./completion";
export { copyText } from "./clipboard";
export { applyTheme, currentTheme, initTheme, type BookTheme } from "./theme";
export { MODULE_MEMBER_TYPES, isScenarioType } from "./shard-types";
export { UNFULFILLED_AREA, areaGroupKey, areaKeysOf } from "./product-areas";
export type {
  BookRecord,
  BookTest,
  CheckLevel,
  ImplKind,
  IntentGrouping,
  LayerKey,
  ParkedChild,
  ProductArea,
  ProductAreas,
  ProofState,
  ReachClaim,
  RecordFields,
  SecurityItem,
  SortKey,
  SourceRef,
  View,
} from "./shard-types";
