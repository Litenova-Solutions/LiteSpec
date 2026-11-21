<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import { stringList } from "../page-values";

  // A feature: what success looks like, and what it excludes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const successCriteria = $derived(stringList(r.fields ?? {}, "successCriteria"));
  const excludes = $derived(stringList(r.fields ?? {}, "excludes"));
</script>

{#if successCriteria.length}
  <PageSection blockKey="succ" heading="successCriteria" count={successCriteria.length}>
    <BulletList items={successCriteria} />
  </PageSection>
{/if}
{#if excludes.length}
  <PageSection blockKey="exc" heading="excludes" count={excludes.length}>
    <BulletList items={excludes} />
  </PageSection>
{/if}
