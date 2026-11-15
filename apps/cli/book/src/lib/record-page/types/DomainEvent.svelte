<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";
  import { objectList, stringField } from "../page-values";

  // A domain event: the fields it carries, each with its type.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const carries = $derived(objectList(r.fields ?? {}, "carries"));
</script>

{#if carries.length}
  <PageSection blockKey="carry" heading="carries" count={carries.length}>
    <div class="flex flex-wrap gap-1">
      {#each carries as c, i (i)}<span class={codeCls}>{stringField(c, "name") ?? ""}: {stringField(c, "type") ?? ""}</span>{/each}
    </div>
  </PageSection>
{/if}
