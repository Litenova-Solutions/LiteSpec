<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import { refusalOf, releasedByHtml, stringField, stringList } from "../page-values";

  // A rule: the refusal it raises, and what it binds, reads and releases.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const refusalId = $derived(book.outs(stripStep(id), "refusal")[0] ?? null);
  const rf = $derived(refusalOf(refusalId ? book.get(refusalId) : null, refusalId));
  const binds = $derived(stringList(f, "binds"));
  const reads = $derived(stringList(f, "reads"));
  const frozenWhen = $derived(stringField(f, "frozenWhen"));
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
{#if frozenWhen}
  <PageSection blockKey="frozen" heading="frozenWhen">{frozenWhen}</PageSection>
{/if}
