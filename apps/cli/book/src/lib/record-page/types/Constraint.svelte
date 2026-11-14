<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import SourceList from "../SourceList.svelte";
  import { objectList, stringField } from "../page-values";

  // A constraint: the authority behind it, the sources it rests on, and what it requires.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const authority = $derived(stringField(f, "authority") ?? "");
  const sources = $derived(objectList(f, "sources"));
  const requires = $derived(r.requires ?? []);
</script>

<PageSection blockKey="src" heading="authority">
  <Note kind="quiet" icon="quote">{authority}</Note>
</PageSection>
<SourceList {sources} />
{#if requires.length}
  <PageSection blockKey="req" heading="What It Requires" count={requires.length}><RecordList ids={requires} /></PageSection>
{/if}
