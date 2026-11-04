<script lang="ts">
  import { base } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import SourceLink from "$lib/components/book/SourceLink.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";

  // A finding: its code, the record it is about, and where it was found.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const subj = $derived(f.subject ? base(String(f.subject)) : null);
</script>

<Block blockKey="about" heading="Finding">
  <Kv>
    <KvRow label="code"><InlineRef id={"finding-code/" + String(f.code ?? "")} /></KvRow>
    <KvRow label="subject">
      {#if subj}
        <InlineRef id={subj} />
      {:else}
        <span class="font-mono text-[11px] text-muted-foreground">none</span>
      {/if}
    </KvRow>
    {#if r.src}
      <KvRow label="path"><SourceLink src={r.src} /></KvRow>
    {/if}
  </Kv>
</Block>
