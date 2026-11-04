<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";

  // A module: how many records of each behavior kind it holds.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const KINDS = ["aggregate", "use-case", "rule", "event", "reaction", "job", "notification", "setting"];
  const all = $derived(KINDS.flatMap((t) => book.descByType(id, t, 6)));
</script>

{#if all.length}
  <Block blockKey="size" heading="Records">
    <Tiles>
      {#each KINDS as t}
        {@const n = all.filter((x) => book.typeOf(x) === t).length}
        {#if n}
          <Tile k={book.pluralName(t)} big>{n}</Tile>
        {/if}
      {/each}
    </Tiles>
  </Block>
{/if}
