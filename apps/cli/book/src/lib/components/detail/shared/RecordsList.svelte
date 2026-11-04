<script lang="ts">
  import { book } from "$lib/book";
  import Block from "../blocks/Block.svelte";
  import Refs from "../blocks/Refs.svelte";

  // The records below a grouping: children in the outline, 150 with a Show all button.
  let { id }: { id: string } = $props();
  const kids = $derived(book.kids(id));
</script>

{#if kids.length}
  <Block blockKey="in" heading="Records" count={kids.length}>
    <Refs ids={kids} limit={150} moreKey={id + "|records"} />
  </Block>
{/if}
