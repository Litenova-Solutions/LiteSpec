<script lang="ts">
  import { book } from "$lib/frame";
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
  // One pass over the links: each label keeps its first-seen order.
  const groups = $derived.by(() => {
    const order: string[] = [];
    const byLabel = new Map<string, typeof rows>();
    for (const row of rows) {
      const hit = byLabel.get(row.label);
      if (hit) hit.push(row);
      else {
        byLabel.set(row.label, [row]);
        order.push(row.label);
      }
    }
    return order.map((label) => {
      const items = byLabel.get(label) ?? [];
      return { label, items, heading: relationLabelOf(label, items, selfType, lookup) };
    });
  });
</script>

{#if rows.length}
  <PageSection blockKey="links" heading="Links" count={rows.length}>
    {#each groups as g (g.label)}
      <h3 class="mt-5 flex min-h-8 items-center justify-between border-b border-border-subtle text-sm font-semibold">
        {g.heading}
        <span class="font-normal text-text-secondary tabular-nums">{g.items.length}</span>
      </h3>
      <RecordList ids={g.items.map((x) => x.id)} limit={8} moreKey={id + "|" + g.label} />
    {/each}
  </PageSection>
{/if}
