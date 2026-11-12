<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import { proseOf } from "../page-prose";

  // A problem: the situation it describes. The evidence table is a generic section.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const situation = $derived(proseOf(r, "situation"));
</script>

{#if situation}
  <PageSection blockKey="sit" heading="situation"><RenderedProse html={situation} /></PageSection>
{/if}
