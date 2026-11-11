<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";
  import { stringList } from "../page-values";

  // An event: the fields it carries.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const carries = $derived(stringList(r.fields ?? {}, "carries"));
</script>

{#if carries.length}
  <PageSection blockKey="carry" heading="carries" count={carries.length}>
    <div class="flex flex-wrap gap-1">
      {#each carries as c (c)}<span class={codeCls}>{c}</span>{/each}
    </div>
  </PageSection>
{/if}
