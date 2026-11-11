<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";

  // An entry point: the path it serves.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

{#if r.fields?.path}
  <PageSection blockKey="path" heading="path">
    <span class="{codeCls} px-2.5 py-1 text-sm">{String(r.fields.path)}</span>
  </PageSection>
{/if}
