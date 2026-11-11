<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import { refusalOf, releasedByHtml } from "../page-values";

  // A rule: the refusal it raises, and what it binds, reads and releases.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const rf = $derived(refusalOf(id));
  const binds = $derived(((f.binds as string[] | undefined) ?? []).map(String));
  const reads = $derived(((f.reads as string[] | undefined) ?? []).map(String));
</script>

<PageSection blockKey="ref" heading="refusal">
  {#if rf.info}
    <ResultCards items={[{ title: rf.info.title, outcome: "refusal", code: rf.info.code, meaning: rf.info.meaning }]} />
  {:else if rf.id}
    <RecordList ids={[rf.id]} />
  {:else}
    <Note kind="quiet" icon="info" html="This rule names no refusal; it holds by construction." />
  {/if}
</PageSection>
{#if binds.length}
  <PageSection blockKey="binds" heading="binds" count={binds.length}><RecordList ids={binds} /></PageSection>
{/if}
{#if reads.length}
  <PageSection blockKey="reads" heading="reads" count={reads.length}><RecordList ids={reads} /></PageSection>
{/if}
{#if f.releasedBy}
  <PageSection blockKey="released" heading="releasedBy">{@html releasedByHtml(f.releasedBy)}</PageSection>
{/if}
{#if f.frozenWhen}
  <PageSection blockKey="frozen" heading="frozenWhen">{String(f.frozenWhen)}</PageSection>
{/if}
