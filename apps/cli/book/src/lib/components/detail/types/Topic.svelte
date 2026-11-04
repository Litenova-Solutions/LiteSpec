<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Refs from "../blocks/Refs.svelte";
  import { proseOf } from "../helpers";

  // A topic: its position, its refusals, and the requirements below it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const reqs = $derived(book.kidsByType(id, "requirement"));
  const position = $derived(proseOf(r, "position"));
  const refusals = $derived(proseOf(r, "refusals"));
</script>

{#if position}
  <Block blockKey="pos" heading="position"><Prose html={position} /></Block>
{/if}
{#if refusals}
  <Block blockKey="ref" heading="refusals"><Prose html={refusals} /></Block>
{/if}
{#if reqs.length}
  <Block blockKey="reqs" heading="Requirements" count={reqs.length}><Refs ids={reqs} /></Block>
{/if}
