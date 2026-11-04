<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import Refs from "../blocks/Refs.svelte";

  // A test: the scenarios it claims.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const claims = $derived(((r.fields?.claims as string[] | undefined) ?? []).map(String));
</script>

{#if claims.length}
  <Block blockKey="claims" heading="claims" count={claims.length}><Refs ids={claims} /></Block>
{/if}
