<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";

  // A test: the scenarios it claims.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const claims = $derived(((r.fields?.claims as string[] | undefined) ?? []).map(String));
</script>

{#if claims.length}
  <PageSection blockKey="claims" heading="claims" count={claims.length}><RecordList ids={claims} /></PageSection>
{/if}
