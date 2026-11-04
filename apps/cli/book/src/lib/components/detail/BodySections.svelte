<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "./blocks/Block.svelte";
  import Callout from "./blocks/Callout.svelte";
  import { bodyIntroOf, bodySectionsOf } from "./helpers";

  // The explanation body: the intro, then each section, with Known Limits,
  // Examples and open questions set apart.
  let { r }: { r: BookRecord } = $props();
  const intro = $derived(bodyIntroOf(r));
  const sections = $derived(bodySectionsOf(r));
</script>

{#if intro}
  <div class="mb-6"><Prose html={intro} /></div>
{/if}
{#each sections as s}
  <Block blockKey={s.key} heading={s.h}>
    {#if s.kind === "risk"}
      <div class="border-l-[3px] border-rule-border py-0.5 pl-3.5"><Prose html={s.html} /></div>
    {:else if s.kind === "example"}
      <div class="border-l-[3px] border-border py-0.5 pl-3.5"><Prose html={s.html} /></div>
    {:else if s.kind === "questions"}
      <Callout kind="info" icon="question"><Prose html={s.html} /></Callout>
    {:else}
      <Prose html={s.html} />
    {/if}
  </Block>
{/each}
