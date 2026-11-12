<script lang="ts">
  import { book, dedupe } from "$lib/frame";
  import PageSection from "./blocks/PageSection.svelte";
  import RecordList from "./blocks/RecordList.svelte";
  import { relationLabelOf } from "./page-values";
  import type { PhraseLookup } from "./page-values";

  // The links of a record, grouped by relation, eight with a Show all button each.
  type Props = { id: string };
  let { id }: Props = $props();
  const rows = $derived(book.connectionsOf(id));
  const selfType = $derived(book.typeOf(id));
  const lookup: PhraseLookup = {
    phraseFor: (field, carrier, dir) => book.phraseFor(field, carrier, dir),
    typeOf: (rid) => book.typeOf(rid),
  };
  const groups = $derived(
    dedupe(rows.map((x) => x.label)).map((label) => {
      const items = rows.filter((x) => x.label === label);
      return { label, items, heading: relationLabelOf(label, items, selfType, lookup) };
    }),
  );
</script>

{#if rows.length}
  <PageSection blockKey="links" heading="Links" count={rows.length}>
    {#each groups as g (g.label)}
      <div class="mb-[18px] last:mb-0">
        <h3 class="m-0 mb-1.5 text-sm font-semibold">{g.heading} ({g.items.length})</h3>
        <RecordList ids={g.items.map((x) => x.id)} limit={8} moreKey={id + "|" + g.label} />
      </div>
    {/each}
  </PageSection>
{/if}
