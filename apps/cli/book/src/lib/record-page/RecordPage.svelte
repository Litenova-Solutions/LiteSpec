<script lang="ts">
  import { book, inline } from "$lib/frame";
  import { glossaryLinks, RenderedProse } from "$lib/record";
  import CompletionBlock from "./CompletionBlock.svelte";
  import LinksBlock from "./LinksBlock.svelte";
  import EvidenceBlock from "./EvidenceBlock.svelte";
  import ExplanationBody from "./ExplanationBody.svelte";
  import FieldTable from "./FieldTable.svelte";
  import FindingsBlock from "./FindingsBlock.svelte";
  import RecordHeader from "./RecordHeader.svelte";
  import ScreenCapture from "./ScreenCapture.svelte";
  import WhyBlock from "./WhyBlock.svelte";
  import { leadFieldOf, proseOf } from "./page-prose";
  import { GenericType, TYPES } from "./types/index";

  // The record page: header, lead, completion, field table, evidence, the type
  // content, Why sections, findings, body sections and links.
  type Props = Record<string, never>;
  let {}: Props = $props();
  const id = $derived(book.selected);
  const r = $derived(id ? book.get(id) : undefined);
  const TypeContent = $derived(r ? (TYPES[r.type] ?? GenericType) : GenericType);

  const leadField = $derived(r ? (leadFieldOf(r.fields ?? {}) ?? "") : "");
  const leadHtml = $derived(r && leadField ? proseOf(r, leadField) : "");
  const leadPlain = $derived(r && leadField && !leadHtml ? String(r.fields?.[leadField] ?? "") : "");
  const leadInline = $derived(r && leadPlain ? inline(book, leadPlain, r.src ?? null) : "");

  $effect(() => {
    if (!id) return;
    // A move inside the outline leaves focus in the tree so arrow keys keep
    // working; any other navigation moves focus to the record title.
    if (!book.focusTitle) return;
    document.getElementById("record-title")?.focus({ preventScroll: true });
  });
</script>

{#if !r || !id}
  <RecordHeader id={id ?? ""} r={null} />
{:else if r.type === "layer"}
  <RecordHeader {id} {r} />
  {@const Layer = TYPES["layer"] ?? GenericType}
  <Layer {id} {r} />
{:else}
  <RecordHeader {id} {r} />
  {#if r.type === "screen"}
    <ScreenCapture {id} {r} />
  {/if}
  {#if leadHtml}
    <div class="mb-6 max-w-[72ch]"><RenderedProse html={leadHtml} lead /></div>
  {:else if leadInline}
    <p class="mt-0 mb-6 max-w-[72ch] text-base leading-relaxed text-slate-900" use:glossaryLinks={leadInline}>{@html leadInline}</p>
  {/if}
  <CompletionBlock {r} />
  <FieldTable {id} {r} />
  <EvidenceBlock {r} />
  <TypeContent {id} {r} />
  <WhyBlock {r} />
  <FindingsBlock {r} />
  <ExplanationBody {r} />
  <LinksBlock {id} />
{/if}
