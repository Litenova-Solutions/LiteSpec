// The shell: the top bar over the columns, the About this book page, and the
// record navigation the App calls. openRecord and syncDocumentTitle are the
// contract names; the rest serves the App alone.

export { default as AboutBook } from "./AboutBook.svelte";
export { default as TopBar } from "./TopBar.svelte";
export { PanelWidth } from "./panel-width.svelte";
export type { PanelWidthOptions } from "./panel-width.svelte";
export {
  delegatedRecordId,
  openRecord,
  reassertFromHash,
  reassertFromHistory,
  scrollToTop,
  syncDocumentTitle,
} from "./record-navigation";
