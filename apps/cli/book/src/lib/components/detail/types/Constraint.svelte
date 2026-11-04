<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import Refs from "../blocks/Refs.svelte";

  // A constraint: the authority behind it, and what it requires.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const requires = $derived(((r.requires as string[] | undefined) ?? []).map(String));
</script>

<Block blockKey="src" heading="authority">
  <Callout kind="quiet" icon="quote">{String(f.authority ?? "")}</Callout>
</Block>
{#if requires.length}
  <Block blockKey="req" heading="What It Requires" count={requires.length}><Refs ids={requires} /></Block>
{/if}
