<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import { PROSE_ORDER, proseOf } from "../page-prose";

  // A type without its own renderer: each prose field as its own section.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

{#each PROSE_ORDER as name, i}
  {@const h = proseOf(r, name)}
  {#if h}
    <PageSection blockKey={"p" + i} heading={name}><RenderedProse html={h} /></PageSection>
  {/if}
{/each}
