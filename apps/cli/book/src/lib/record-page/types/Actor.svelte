<script lang="ts">
  import { capitalize } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { badgeHtml, inlineRefHtml, tdHtml } from "../page-html";

  // An actor: the roles it holds, each with its scope.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const holds = $derived(((r.fields?.holds as RecordFields[] | undefined) ?? []) as RecordFields[]);
</script>

{#if holds.length}
  <PageSection blockKey="holds" heading="holds" count={holds.length}>
    <DataTable
      head={["Role", "Scope"]}
      rows={holds.map((x) => [tdHtml(inlineRefHtml(String(x.role ?? ""))), tdHtml(badgeHtml(capitalize(String(x.scope ?? ""))))])}
    />
  </PageSection>
{/if}
