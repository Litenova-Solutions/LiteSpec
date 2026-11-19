<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import Note from "./blocks/Note.svelte";
  import { bodyIntroOf, bodySectionsOf } from "./page-prose";

  // The explanation body: the intro, then each section, with Excludes and
  // Boundaries as a warning note and open questions as an info note.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const intro = $derived(bodyIntroOf(r));
  const sections = $derived(bodySectionsOf(r));
</script>

{#if intro}
  <div class="mt-10"><RenderedProse html={intro} /></div>
{/if}
{#each sections as s (s.key)}
  <PageSection blockKey={s.key} heading={s.h}>
    {#if s.kind === "risk"}
      <Note kind="warn" icon="alert"><RenderedProse html={s.html} /></Note>
    {:else if s.kind === "example"}
      <RenderedProse html={s.html} />
    {:else if s.kind === "questions"}
      <Note kind="info" icon="question"><RenderedProse html={s.html} /></Note>
    {:else}
      <RenderedProse html={s.html} />
    {/if}
  </PageSection>
{/each}
