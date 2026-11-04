<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import Refs from "../blocks/Refs.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import { refusalOf, releasedByHtml } from "../helpers";

  // A rule: the refusal it raises, and what it binds, reads and releases.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const rf = $derived(refusalOf(id));
  const binds = $derived(((f.binds as string[] | undefined) ?? []).map(String));
  const reads = $derived(((f.reads as string[] | undefined) ?? []).map(String));
</script>

<Block blockKey="ref" heading="refusal">
  {#if rf.info}
    <ResultCards items={[{ title: rf.info.title, outcome: "refusal", code: rf.info.code, meaning: rf.info.meaning }]} />
  {:else if rf.id}
    <Refs ids={[rf.id]} />
  {:else}
    <Callout kind="quiet" icon="info" html="This rule names no refusal; it holds by construction." />
  {/if}
</Block>
{#if binds.length}
  <Block blockKey="binds" heading="binds" count={binds.length}><Refs ids={binds} /></Block>
{/if}
{#if reads.length}
  <Block blockKey="reads" heading="reads" count={reads.length}><Refs ids={reads} /></Block>
{/if}
{#if f.releasedBy}
  <Block blockKey="released" heading="releasedBy">{@html releasedByHtml(f.releasedBy)}</Block>
{/if}
{#if f.frozenWhen}
  <Block blockKey="frozen" heading="frozenWhen">{String(f.frozenWhen)}</Block>
{/if}
