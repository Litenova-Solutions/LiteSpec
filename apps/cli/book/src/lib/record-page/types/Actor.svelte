<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { badgeHtml, inlineRefHtml, tdHtml } from "../page-html";
  import { stringField, stringList } from "../page-values";

  // An actor: the roles it holds. The scope of each grant is the role's own.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const holds = $derived(stringList(r.fields ?? {}, "holds"));
</script>

{#if holds.length}
  <PageSection blockKey="holds" heading="holds" count={holds.length}>
    <DataTable
      head={["Role", "Scope"]}
      rows={holds.map((role) => [tdHtml(inlineRefHtml(book, role)), tdHtml(badgeHtml(capitalize(stringField(book.get(role)?.fields, "scope") ?? "")))])}
    />
  </PageSection>
{/if}
