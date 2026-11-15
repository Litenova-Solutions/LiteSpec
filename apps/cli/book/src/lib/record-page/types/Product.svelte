<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import { stringList } from "../page-values";

  // The product page: what the product excludes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const exclusions = $derived(stringList(r.fields ?? {}, "excludes"));
</script>

{#if exclusions.length}
  <PageSection blockKey="exc" heading="excludes" count={exclusions.length}>
    <ul class="m-0 list-none border-t border-slate-200 p-0">
      {#each exclusions as x (x)}
        <li class="grid grid-cols-[20px_1fr] gap-2 border-b border-slate-200 py-2.5 text-sm">
          <RecordIcon name="x" class="mt-1 size-3.5 text-warn-foreground" />
          <span>{x}</span>
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
