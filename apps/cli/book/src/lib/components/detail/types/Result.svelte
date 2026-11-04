<script lang="ts">
  import { base, book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import ScenarioCard from "../shared/ScenarioCard.svelte";
  import { codeCls } from "../helpers";

  // A result: what it means, its code, and the scenarios that show it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const sc = $derived(book.shownByOf(id).filter((x) => book.get(base(x))));
</script>

{#if f.code}
  <Block blockKey="code" heading="Refusal code">
    <span class="{codeCls} px-2.5 py-1 text-[13px]">{String(f.code)}</span>
  </Block>
{/if}
{#if sc.length}
  <Block blockKey="ex" heading="Scenarios" count={sc.length}>
    <div class="grid gap-2">
      {#each sc as x (x)}
        <ScenarioCard sid={x} />
      {/each}
    </div>
  </Block>
{/if}
