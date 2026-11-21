<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { glossaryLinks } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import { whyPartsOf } from "./page-prose";

  // The Why section: the reason fields as a label/value list.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const parts = $derived(whyPartsOf(book, r));
</script>

{#if parts.length}
  <PageSection blockKey="why" heading="Why">
    <dl class="grid grid-cols-[minmax(9rem,25%)_minmax(0,1fr)] border-t border-border-subtle text-sm [&_a]:text-link [&_a]:no-underline [&_a:hover]:underline">
      {#each parts as p (p.label)}
        <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">{p.label}</dt>
        <dd class="m-0 min-w-0 border-b border-border-subtle py-3" use:glossaryLinks={p.html}>{@html p.html}</dd>
      {/each}
    </dl>
  </PageSection>
{/if}
