<script lang="ts">
  import { book, capitalize, esc, safeHref, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import DataTable from "./blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "./page-html";
  import { objectList, stringField } from "./page-values";

  // The evidence table of a problem: each claim, its kind, its source and when read.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const list = $derived(objectList(r.fields ?? {}, "evidence"));
</script>

{#if r.type === "problem" && list.length}
  <PageSection blockKey="evidence" heading="Evidence" count={list.length}>
    <DataTable
      head={["Claim", "Kind", "Source", "Read on"]}
      rows={list.map((e) => {
        const source = stringField(e, "source") ?? "";
        const isId = /^[a-z][a-z-]*\//.test(source);
        const href = !isId ? safeHref(book, source) : null;
        const sourceHtml = isId
          ? inlineRefHtml(stripStep(source))
          : href
            ? `<a href="${esc(href)}" target="_blank" rel="noopener">${esc(source)}</a>`
            : esc(source);
        const kind = stringField(e, "kind");
        const readOn = stringField(e, "readOn");
        return [
          tdHtml(esc(stringField(e, "claim") ?? "")),
          tdHtml(kind ? esc(capitalize(kind)) : ""),
          tdHtml(sourceHtml),
          tdHtml(readOn ? book.fmtDate(readOn) : ""),
        ];
      })}
    />
    {#if r.evidenceStrength}
      <p class="mt-2.5 mb-0 text-sm text-subtle-foreground">Evidence strength: {capitalize(r.evidenceStrength)}</p>
    {/if}
  </PageSection>
{/if}
