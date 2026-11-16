// The shared record pieces: one icon, line, link and prose renderer per
// record, with type and proof marks, hover explanations, source links and
// glossary linking.

export { default as SourceFileLink } from "./SourceFileLink.svelte";
export { default as Hover } from "./Hover.svelte";
export { default as ExplainLayer } from "./ExplainLayer.svelte";
export { default as ProofMark } from "./ProofMark.svelte";
export { default as RecordIcon } from "./RecordIcon.svelte";
export { default as RecordLine } from "./RecordLine.svelte";
export { default as RecordLink } from "./RecordLink.svelte";
export { default as RenderedProse } from "./RenderedProse.svelte";
export { default as SourceLink } from "./SourceLink.svelte";
export { default as TypeTag } from "./TypeTag.svelte";
export { glossaryLinks } from "./glossary-links";
export { kindCode, layerBadgeClass, typeBadgeClass, CODED_TYPES } from "./record-codes";
export { pageSections } from "./page-sections.svelte";
export type { PageSectionEntry } from "./page-sections.svelte";
