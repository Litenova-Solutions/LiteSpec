<script lang="ts">
  import { book } from "$lib/book";
  import { inline } from "$lib/book/html";
  import { glossary } from "$lib/components/book/glossary";
  import Prose from "$lib/components/book/Prose.svelte";
  import BodySections from "./BodySections.svelte";
  import Capture from "./Capture.svelte";
  import Completion from "./Completion.svelte";
  import Evidence from "./Evidence.svelte";
  import FieldTable from "./FieldTable.svelte";
  import Findings from "./Findings.svelte";
  import Header from "./Header.svelte";
  import LinksBlock from "./LinksBlock.svelte";
  import Questions from "./Questions.svelte";
  import WhySections from "./WhySections.svelte";
  import { factsFor } from "./facts";
  import { leadTextOf, proseOf } from "./helpers";
  import { GenericType, TYPES } from "./types/index";

  // The record page: header, lead, completion, field table, evidence, the type
  // content, Why sections, questions, findings, body sections and links.
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
  <Header {id} {r} />
  {@const Layer = TYPES["layer"] ?? GenericType}
  <Layer {id} {r} />
{:else}
  <Header {id} {r} />
  {#if r.type === "screen"}
    <Capture {id} {r} />
  {/if}
  {#if leadHtml}
    <div class="mb-6 max-w-[72ch]"><Prose html={leadHtml} lead /></div>
  {:else if leadInline}
    <p class="mt-0 mb-6 max-w-[72ch] text-lg leading-relaxed" use:glossary={leadInline}>{@html leadInline}</p>
  {/if}
  <Completion {r} />
  <FieldTable {id} {r} {facts} />
  <Evidence {r} />
  <TypeContent {id} {r} />
  <WhySections {r} />
  <Questions {id} {r} />
  <Findings {r} />
  <BodySections {r} />
  <LinksBlock {id} />
{/if}
