<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";

  // An event: the fields it carries.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const carries = $derived(((r.fields?.carries as string[] | undefined) ?? []).map(String));
</script>

{#if carries.length}
  <PageSection blockKey="carry" heading="carries" count={carries.length}>
    <div class="flex flex-wrap gap-1">
      {#each carries as c}<span class={codeCls}>{c}</span>{/each}
    </div>
  </PageSection>
{/if}
