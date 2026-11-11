<script lang="ts">
  import { book, dedupe } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import RecordList from "./blocks/RecordList.svelte";
  import { relationLabelOf } from "./page-values";

  // The links of a record, grouped by relation, eight with a Show all button each.
  let { id }: { id: string } = $props();
  const rows = $derived(book.connectionsOf(id));
  const labels = $derived(dedupe(rows.map((x) => x.label)));
  const selfType = $derived(book.typeOf(id));
</script>

{#if rows.length}
  <PageSection blockKey="links" heading="Links" count={rows.length}>
    {#each labels as label}
      {@const items = rows.filter((x) => x.label === label)}
      <div class="mb-[18px] last:mb-0">
        <h3 class="m-0 mb-1.5 text-sm font-semibold">{relationLabelOf(label, items, selfType)} ({items.length})</h3>
        <RecordList ids={items.map((x) => x.id)} limit={8} moreKey={id + "|" + label} />
      </div>
    {/each}
  </PageSection>
{/if}
