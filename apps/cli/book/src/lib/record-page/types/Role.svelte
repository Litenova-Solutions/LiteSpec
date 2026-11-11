<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";
  import { stringList } from "../page-values";

  // A role: the permissions it allows, each with its meaning.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const allows = $derived(stringList(r.fields ?? {}, "allows"));
</script>

{#if allows.length}
  <PageSection blockKey="allows" heading="allows" count={allows.length}>
    <DataTable
      head={["Permission", "Meaning"]}
      rows={allows.map((p) => [tdHtml(inlineRefHtml(book, p)), tdHtml(esc(String(book.get(stripStep(p))?.fields?.meaning ?? "")))])}
    />
  </PageSection>
{/if}
