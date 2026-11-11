<script lang="ts">
  import { book, inline } from "$lib/frame";
  import { glossary, RenderedProse } from "$lib/record";
  import CompletionBlock from "./CompletionBlock.svelte";
  import ConnectionsBlock from "./ConnectionsBlock.svelte";
  import EvidenceBlock from "./EvidenceBlock.svelte";
  import ExplanationBody from "./ExplanationBody.svelte";
  import FieldTable from "./FieldTable.svelte";
  import FindingsBlock from "./FindingsBlock.svelte";
  import RecordHeader from "./RecordHeader.svelte";
  import ScreenCapture from "./ScreenCapture.svelte";
  import WhyBlock from "./WhyBlock.svelte";
  import { factsFor } from "./type-facts";
  import { leadTextOf, proseOf } from "./page-prose";
  import { GenericType, TYPES } from "./types/index";

  // The record page: header, lead, completion, field table, evidence, the type
  // content, Why sections, findings, body sections and links.
  const id = $derived(book.selected);
  const r = $derived(id ? book.get(id) : undefined);
  const facts = $derived(r && id ? factsFor(r.type, id, r) : []);
  const TypeContent = $derived(r ? (TYPES[r.type] ?? GenericType) : GenericType);

  const leadField = $derived.by(() => {
    if (!r) return "";
    const f = r.fields ?? {};
    return f.description ? "description" : f.summary ? "summary" : f.offering ? "offering" : f.purpose ? "purpose" : "";
  });
  const leadHtml = $derived(r && leadField ? proseOf(r, leadField) : "");
  const leadPlain = $derived(r && !leadHtml ? leadTextOf(r) : "");
  const leadInline = $derived(r && leadPlain ? inline(book, leadPlain, r.src ?? null) : "");
</script>

{#if !r || !id}
  <nav class="mb-[18px] flex flex-wrap items-center gap-1.5 text-[13px] text-muted-foreground" aria-label="Location">
    <b class="font-semibold text-foreground">{id ?? ""}</b>
  </nav>
  <h1 class="m-0 mb-1.5 max-w-[72ch] text-[34px] leading-[1.15] font-semibold tracking-[-0.03em]">{id ?? ""}</h1>
  <p class="mt-0 mb-2 text-sm font-semibold text-subtle-foreground">Record</p>
  <div class="mb-[18px] flex flex-wrap items-center gap-x-3.5 gap-y-2 text-xs text-muted-foreground">
    <code class="font-mono text-xs font-medium text-foreground">{id ?? ""}</code>
  </div>
  <p class="mx-2 mt-0 mb-2 text-xs text-muted-foreground">No record with this identifier is in the book.</p>
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
    <p class="mt-0 mb-6 max-w-[72ch] text-lg leading-relaxed" use:glossary={leadInline}>{@html leadInline}</p>
  {/if}
  <CompletionBlock {r} />
  <FieldTable {id} {r} {facts} />
  <EvidenceBlock {r} />
  <TypeContent {id} {r} />
  <WhyBlock {r} />
  <FindingsBlock {r} />
  <ExplanationBody {r} />
  <ConnectionsBlock {id} />
{/if}
