<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import { PROSE_ORDER, proseOf } from "../helpers";

  // A type without its own renderer: each prose field as its own section.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

{#each PROSE_ORDER as name, i}
  {@const h = proseOf(r, name)}
  {#if h}
    <Block blockKey={"p" + i} heading={name}><Prose html={h} /></Block>
  {/if}
{/each}
