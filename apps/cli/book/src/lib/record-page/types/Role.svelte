<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";

  // A role: the permissions it allows, each with its meaning.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const allows = $derived(((r.fields?.allows as string[] | undefined) ?? []).map(String));
</script>

{#if allows.length}
  <PageSection blockKey="allows" heading="allows" count={allows.length}>
    <DataTable
      head={["Permission", "Meaning"]}
      rows={allows.map((p) => [tdHtml(inlineRefHtml(p)), tdHtml(esc(String(book.get(stripStep(p))?.fields?.meaning ?? "")))])}
    />
  </PageSection>
{/if}
