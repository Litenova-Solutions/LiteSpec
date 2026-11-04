<script lang="ts">
  import type { Fields } from "$lib/book/types";
  import { duration } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Refs from "../blocks/Refs.svelte";

  // What drives an aggregate's saga: a job, a reaction, or a person with a runbook.
  let { f }: { f: Fields } = $props();
  const d = $derived((f.drivenBy as Fields | undefined) ?? null);
  const manual = $derived(!!(d && !d.job && !d.reaction && (d.person || d.runbook || d.dueWithin)));
</script>

{#if d}
  {#if d.job}
    <Block blockKey="drv" heading="drivenBy"><Refs ids={[String(d.job)]} /></Block>
  {:else if d.reaction}
    <Block blockKey="drv" heading="drivenBy"><Refs ids={[String(d.reaction)]} /></Block>
  {:else if manual}
    <Block blockKey="drv" heading="drivenBy">
      <Kv>
        {#if d.person}<KvRow label="Person"><InlineRef id={String(d.person)} /></KvRow>{/if}
        {#if d.runbook}<KvRow label="Runbook"><InlineRef id={String(d.runbook)} /></KvRow>{/if}
        {#if d.dueWithin}<KvRow label="Due within">{duration(String(d.dueWithin))}</KvRow>{/if}
      </Kv>
    </Block>
  {/if}
{/if}
