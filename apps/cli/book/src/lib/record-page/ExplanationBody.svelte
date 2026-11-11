<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import Note from "./blocks/Note.svelte";
  import { bodyIntroOf, bodySectionsOf } from "./page-prose";

  // The explanation body: the intro, then each section, with Known Limits,
  // Examples and open questions set apart.
  let { r }: { r: BookRecord } = $props();
  const intro = $derived(bodyIntroOf(r));
  const sections = $derived(bodySectionsOf(r));
</script>

{#if intro}
  <div class="mb-6"><RenderedProse html={intro} /></div>
{/if}
{#each sections as s}
  <PageSection blockKey={s.key} heading={s.h}>
    {#if s.kind === "risk"}
      <div class="border-l-[3px] border-rule-border py-0.5 pl-3.5"><RenderedProse html={s.html} /></div>
    {:else if s.kind === "example"}
      <div class="border-l-[3px] border-border py-0.5 pl-3.5"><RenderedProse html={s.html} /></div>
    {:else if s.kind === "questions"}
      <Note kind="info" icon="question"><RenderedProse html={s.html} /></Note>
    {:else}
      <RenderedProse html={s.html} />
    {/if}
  </PageSection>
{/each}
