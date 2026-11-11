<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";
  import { stringField } from "../page-values";

  // An entry point: the path it serves.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const path = $derived(stringField(r.fields ?? {}, "path"));
</script>

{#if path}
  <PageSection blockKey="path" heading="path">
    <span class="{codeCls} px-2.5 py-1 text-sm">{path}</span>
  </PageSection>
{/if}
