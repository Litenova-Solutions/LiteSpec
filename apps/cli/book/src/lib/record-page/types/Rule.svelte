<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { codeOf, refLink } from "../shared/record-cells";
  import { isRecord, refusalOf, stringField, stringList } from "../page-values";

  // A rule: the refusal it raises, and what it binds, reads and releases.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const refusalId = $derived(book.outs(stripStep(id), "refusal")[0] ?? null);
  const rf = $derived(refusalOf(refusalId ? book.get(refusalId) : null, refusalId));
  const refusalLink = $derived(refusalId ?? "");
  const binds = $derived(stringList(f, "binds"));
  const reads = $derived(stringList(f, "reads"));
  const frozenWhen = $derived(stringField(f, "frozenWhen"));
  const releasedHtml = $derived.by(() => {
    const v = f.releasedBy;
    if (!v) return "";
    if (typeof v === "string") return refLink(book, v);
    const reason = isRecord(v) ? stringField(v, "reason") : undefined;
    return "Never" + (reason ? ", because " + esc(reason) : "");
  });
</script>

<PageSection blockKey="ref" heading="refusal">
  {#if rf.info}
    <div class="bg-layer p-4">
      <div class="text-xs tracking-[0.32px] text-text-secondary">{book.fieldLabel("refusal")}</div>
      <div class="mt-1 text-[28px] font-light leading-9"><RecordLink id={refusalLink} /></div>
      {#if rf.info.code}<p class="mt-3 mb-0">{@html codeOf(rf.info.code)}</p>{/if}
      {#if rf.info.meaning}<p class="mt-4 mb-0 text-sm text-text-secondary">{rf.info.meaning}</p>{/if}
    </div>
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
  <PageSection blockKey="released" heading="releasedBy">{@html releasedHtml}</PageSection>
{/if}
{#if frozenWhen}
  <PageSection blockKey="frozen" heading="frozenWhen">{frozenWhen}</PageSection>
{/if}
