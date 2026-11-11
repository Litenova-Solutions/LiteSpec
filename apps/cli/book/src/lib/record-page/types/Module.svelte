<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";

  // A module: how many records of each behavior kind it holds.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const KINDS = ["aggregate", "use-case", "rule", "event", "reaction", "job", "notification", "setting"];
  const all = $derived(KINDS.flatMap((t) => book.descByType(id, t, 6)));
</script>

{#if all.length}
  <PageSection blockKey="size" heading="Records">
    <FactGrid>
      {#each KINDS as t}
        {@const n = all.filter((x) => book.typeOf(x) === t).length}
        {#if n}
          <FactCell k={book.pluralName(t)} big>{n}</FactCell>
        {/if}
      {/each}
    </FactGrid>
  </PageSection>
{/if}
