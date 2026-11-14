<script lang="ts">
  import { book, inline } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import SourceList from "../SourceList.svelte";
  import { objectList, stringField } from "../page-values";

  // Research: the conclusion, and the sources it rests on.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const conclusionText = $derived(stringField(f, "conclusion"));
  const conclusion = $derived(conclusionText ? inline(book, conclusionText, r.src ?? null) : "");
  const sources = $derived(objectList(f, "sources"));
</script>

{#if conclusion}
  <PageSection blockKey="con" heading="conclusion"><RenderedProse html={conclusion} /></PageSection>
{/if}
<SourceList {sources} />
