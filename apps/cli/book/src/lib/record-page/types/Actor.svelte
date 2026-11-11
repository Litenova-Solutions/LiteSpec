<script lang="ts">
  import { capitalize } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { badgeHtml, inlineRefHtml, tdHtml } from "../page-html";
  import { objectList, stringField } from "../page-values";

  // An actor: the roles it holds, each with its scope.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const holds = $derived(objectList(r.fields ?? {}, "holds"));
</script>

{#if holds.length}
  <PageSection blockKey="holds" heading="holds" count={holds.length}>
    <DataTable
      head={["Role", "Scope"]}
      rows={holds.map((x) => [tdHtml(inlineRefHtml(stringField(x, "role") ?? "")), tdHtml(badgeHtml(capitalize(stringField(x, "scope") ?? "")))])}
    />
  </PageSection>
{/if}
