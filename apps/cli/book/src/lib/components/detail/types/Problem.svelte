<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import { proseOf } from "../helpers";

  // A problem: the situation it describes. The evidence table is a generic section.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const situation = $derived(proseOf(r, "situation"));
</script>

{#if situation}
  <Block blockKey="sit" heading="situation"><Prose html={situation} /></Block>
{/if}
