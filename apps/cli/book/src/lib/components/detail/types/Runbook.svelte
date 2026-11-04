<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import { proseOf } from "../helpers";

  // A runbook: the trigger, and each procedure section in order.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const NAMES: [string, string][] = [
    ["impact", "imp"], ["prerequisites", "pre"], ["procedure", "proc"],
    ["verification", "ver"], ["recovery", "rec"], ["escalation", "esc"],
  ];
</script>

{#if f.trigger}
  <Callout kind="warn" icon="alert"><b>Trigger.</b> {String(f.trigger)}</Callout>
{/if}
{#each NAMES as [name, key]}
  {@const h = proseOf(r, name)}
  {#if h}
    <Block blockKey={key} heading={name}><Prose html={h} /></Block>
  {/if}
{/each}
