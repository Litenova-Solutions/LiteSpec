<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import { codeCls } from "../helpers";

  // An event: the fields it carries.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const carries = $derived(((r.fields?.carries as string[] | undefined) ?? []).map(String));
</script>

{#if carries.length}
  <Block blockKey="carry" heading="carries" count={carries.length}>
    <div class="flex flex-wrap gap-1">
      {#each carries as c}<span class={codeCls}>{c}</span>{/each}
    </div>
  </Block>
{/if}
