// The shell: the top bar over the columns, and the record navigation the App
// calls. openRecord and syncDocumentTitle are the contract names; the rest
// serves the App alone.

export { default as RecordSearch } from "./RecordSearch.svelte";
export { default as RunWidgets } from "./RunWidgets.svelte";
export { default as ShortcutKeys } from "./ShortcutKeys.svelte";
export { default as ShortcutsDialog } from "./ShortcutsDialog.svelte";
export { default as TopBar } from "./TopBar.svelte";
export {
  delegatedRecordId,
  openRecord,
  reassertFromHash,
  reassertFromHistory,
  scrollToTop,
  syncDocumentTitle,
} from "./record-navigation";
