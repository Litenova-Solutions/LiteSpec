<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import { codeCls } from "../helpers";

  // An entry point: the path it serves.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

{#if r.fields?.path}
  <Block blockKey="path" heading="path">
    <span class="{codeCls} px-2.5 py-1 text-sm">{String(r.fields.path)}</span>
  </Block>
{/if}
