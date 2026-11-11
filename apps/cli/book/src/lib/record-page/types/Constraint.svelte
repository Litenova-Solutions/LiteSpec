<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";

  // A constraint: the authority behind it, and what it requires.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const requires = $derived(((r.requires as string[] | undefined) ?? []).map(String));
</script>

<PageSection blockKey="src" heading="authority">
  <Note kind="quiet" icon="quote">{String(f.authority ?? "")}</Note>
</PageSection>
{#if requires.length}
  <PageSection blockKey="req" heading="What It Requires" count={requires.length}><RecordList ids={requires} /></PageSection>
{/if}
