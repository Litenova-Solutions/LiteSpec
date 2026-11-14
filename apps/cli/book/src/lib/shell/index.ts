// The shell: the top bar over the columns, and the record navigation the App
// calls. openRecord and syncDocumentTitle are the contract names; the rest
// serves the App alone.

export { default as TopBar } from "./TopBar.svelte";
export { NavDrawer } from "./nav-drawer.svelte";
export {
  delegatedRecordId,
  openRecord,
  reassertFromHash,
  reassertFromHistory,
  scrollToTop,
  syncDocumentTitle,
} from "./record-navigation";
