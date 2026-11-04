<script lang="ts">
  import { base, book, cap } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc, safeHref } from "$lib/book/html";
  import Block from "./blocks/Block.svelte";
  import Table from "./blocks/Table.svelte";
  import { inlineRefHtml, tdHtml } from "./helpers";

  // The evidence table of a problem: each claim, its kind, its source and when read.
  let { r }: { r: BookRecord } = $props();
  const list = $derived(((r.fields?.evidence as Fields[] | undefined) ?? []) as Fields[]);
</script>

{#if r.type === "problem" && list.length}
  <Block blockKey="evidence" heading="Evidence" count={list.length}>
    <Table
      head={["Claim", "Kind", "Source", "Read on"]}
      rows={list.map((e) => {
        const source = String(e.source ?? "");
        const isId = /^[a-z][a-z-]*\//.test(source);
        const href = !isId ? safeHref(book, source) : null;
        const sourceHtml = isId
          ? inlineRefHtml(base(source))
          : href
            ? `<a href="${esc(href)}" target="_blank" rel="noopener">${esc(source)}</a>`
            : esc(source);
        return [
          tdHtml(esc(String(e.claim ?? ""))),
          tdHtml(esc(e.kind ? cap(String(e.kind)) : "")),
          tdHtml(sourceHtml),
          tdHtml(e.readOn ? book.fmtDate(String(e.readOn)) : ""),
        ];
      })}
    />
    {#if r.evidenceStrength}
      <p class="mt-2.5 mb-0 text-sm text-subtle-foreground">Evidence strength: {cap(String(r.evidenceStrength))}</p>
    {/if}
  </Block>
{/if}
