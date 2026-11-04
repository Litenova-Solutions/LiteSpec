<script lang="ts">
  import { cap } from "$lib/book";
  import type { Fields } from "$lib/book/types";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Refs from "../blocks/Refs.svelte";

  // The data a behavior touches: what it changes, what it reads, and how the two agree.
  let { f }: { f: Fields } = $props();
  const changes = $derived(((f.changes as string[] | undefined) ?? []).map(String));
  const reads = $derived(((f.reads as string[] | undefined) ?? []).map(String));
  const consistency = $derived((f.consistency as Fields | undefined) ?? null);
  const has = $derived(changes.length > 0 || reads.length > 0 || !!consistency);
</script>

{#if has}
  <Block blockKey="data" heading="data">
    <Kv>
      {#if changes.length}
        <KvRow label="Changes"><Refs ids={changes} /></KvRow>
      {/if}
      {#if reads.length}
        <KvRow label="Reads"><Refs ids={reads} /></KvRow>
      {/if}
      {#if consistency}
        <KvRow label="Consistency">
          <FactBadge text={cap(consistency.mode)} />
          {@const cites = ((consistency.cites as string[] | undefined) ?? []).map(String)}
          {#if cites.length}
            <div class="mt-1 font-mono text-[11px] text-muted-foreground">
              because {#each cites as c, i}{#if i > 0}, {/if}<InlineRef id={c} />{/each}
            </div>
          {/if}
        </KvRow>
      {/if}
    </Kv>
  </Block>
{/if}
