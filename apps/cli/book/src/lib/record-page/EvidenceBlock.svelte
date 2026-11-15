<script lang="ts">
  import { book, capitalize, esc, safeHref, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import DataTable from "./blocks/DataTable.svelte";
  import { objectList, stringField } from "./page-values";

  // The evidence table of a problem: each claim, its kind, its source and when read.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const list = $derived(objectList(r.fields ?? {}, "evidence"));

  const CELL = "bg-layer px-4 py-3 border-t border-border-subtle align-top text-sm";
  const ROW_HEAD = "bg-layer px-4 py-3 border-t border-border-subtle align-top text-left font-normal";

  /** One source as the book shows it: a record link, an external link, or plain text. */
  function sourceHtml(source: string): string {
    if (/^[a-z][a-z-]*\//.test(source)) {
      const id = stripStep(source);
      if (book.has(id)) return `<a href="#${esc(id)}" data-go="${esc(id)}" class="text-link hover:underline">${esc(book.title(id))}</a>`;
      return `<span class="font-mono text-xs">${esc(id)}</span>`;
    }
    const href = safeHref(book, source);
    if (href) return `<a href="${esc(href)}" target="_blank" rel="noopener" class="text-link hover:underline">${esc(source)}</a>`;
    return esc(source);
  }

  const rows = $derived(
    list.map((e) => {
      const kind = stringField(e, "kind");
      const readOn = stringField(e, "readOn");
      return [
        `<th scope="row" class="${ROW_HEAD}">${esc(stringField(e, "claim") ?? "")}</th>`,
        `<td class="${CELL}">${kind ? esc(capitalize(kind)) : ""}</td>`,
        `<td class="${CELL}">${sourceHtml(stringField(e, "source") ?? "")}</td>`,
        `<td class="${CELL}">${readOn ? book.fmtDate(readOn) : ""}</td>`,
      ];
    }),
  );
</script>

{#if r.type === "problem" && list.length}
  <PageSection blockKey="evidence" heading="Evidence" count={list.length}>
    <DataTable head={["Claim", "Kind", "Source", "Read on"]} {rows} caption={"Evidence for " + r.title} />
    {#if r.evidenceStrength}
      <p class="mt-4 text-sm text-text-secondary">Evidence strength: {capitalize(r.evidenceStrength)}</p>
    {/if}
  </PageSection>
{/if}
