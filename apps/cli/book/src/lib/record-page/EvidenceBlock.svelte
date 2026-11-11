<script lang="ts">
  import { book, capitalize, esc, safeHref, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import DataTable from "./blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "./page-html";

  // The evidence table of a problem: each claim, its kind, its source and when read.
  let { r }: { r: BookRecord } = $props();
  const list = $derived(((r.fields?.evidence as RecordFields[] | undefined) ?? []) as RecordFields[]);
</script>

{#if r.type === "problem" && list.length}
  <PageSection blockKey="evidence" heading="Evidence" count={list.length}>
    <DataTable
      head={["Claim", "Kind", "Source", "Read on"]}
      rows={list.map((e) => {
        const source = String(e.source ?? "");
        const isId = /^[a-z][a-z-]*\//.test(source);
        const href = !isId ? safeHref(book, source) : null;
        const sourceHtml = isId
          ? inlineRefHtml(stripStep(source))
          : href
            ? `<a href="${esc(href)}" target="_blank" rel="noopener">${esc(source)}</a>`
            : esc(source);
        return [
          tdHtml(esc(String(e.claim ?? ""))),
          tdHtml(esc(e.kind ? capitalize(String(e.kind)) : "")),
          tdHtml(sourceHtml),
          tdHtml(e.readOn ? book.fmtDate(String(e.readOn)) : ""),
        ];
      })}
    />
    {#if r.evidenceStrength}
      <p class="mt-2.5 mb-0 text-sm text-subtle-foreground">Evidence strength: {capitalize(String(r.evidenceStrength))}</p>
    {/if}
  </PageSection>
{/if}
