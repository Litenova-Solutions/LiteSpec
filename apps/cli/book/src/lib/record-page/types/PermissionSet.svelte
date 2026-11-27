<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import { stringList } from "../page-values";

  // A permission set: what it excludes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const excludes = $derived(stringList(r.fields ?? {}, "excludes"));
</script>

{#if excludes.length}
  <PageSection blockKey="exc" heading="excludes" count={excludes.length}>
    <BulletList items={excludes} />
  </PageSection>
{/if}
