<script lang="ts">
  import { book, inline } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import { stringList } from "../page-values";

  // The product page: what the product excludes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const exclusions = $derived(stringList(r.fields ?? {}, "excludes"));
  const exclusionsHtml = $derived(exclusions.map((x) => `<p>${inline(book, x, r.src ?? null)}</p>`).join(""));
</script>

{#if exclusions.length}
  <PageSection blockKey="exc" heading="excludes" count={exclusions.length}>
    <RenderedProse html={exclusionsHtml} />
  </PageSection>
{/if}
