<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { stringList } from "../page-values";

  // A test: the scenarios it claims.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const claims = $derived(stringList(r.fields ?? {}, "claims"));
</script>

{#if claims.length}
  <PageSection blockKey="claims" heading="claims" count={claims.length}><RecordList ids={claims} /></PageSection>
{/if}
