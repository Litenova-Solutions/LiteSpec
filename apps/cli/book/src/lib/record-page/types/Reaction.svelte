<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, RenderedProse, TypeTag } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import FailureBlock from "../shared/FailureBlock.svelte";
  import { idemHint } from "../shared/record-cells";
  import { proseOf } from "../page-prose";
  import { idemTileOf, objectField, reactionFields } from "../page-values";

  // A reaction: what triggers it, which use case it undoes, what it does, and what it does when it fails.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const rf = $derived(reactionFields(f));
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const triggers = $derived.by(() => {
    const t = rf.triggeredBy;
    if (!t) return [];
    return (Array.isArray(t) ? t : [t]).map(String);
  });
  const compensates = $derived(rf.compensates);
  const idem = $derived(idemTileOf(f));
  const idemHintHtml = $derived.by(() => {
    const i = objectField(f, "idempotency");
    return i ? idemHint(i) : "";
  });
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if triggers.length || rf.skippedWhen}
  <PageSection blockKey="trig" heading="triggeredBy">
    {#each triggers as t (t)}
      <p class="mt-0 mb-2 flex items-center gap-2 last:mb-0">
        <TypeTag type={book.typeOf(stripStep(t))} /><RecordLink id={t} />
      </p>
    {/each}
    {#if rf.skippedWhen}<p class="mt-1 mb-0 text-sm">Skipped when {rf.skippedWhen}</p>{/if}
  </PageSection>
{/if}
{#if compensates}
  <PageSection blockKey="comp" heading="compensates">
    <p class="mt-0 mb-2 flex items-center gap-2 last:mb-0">
      <TypeTag type={book.typeOf(stripStep(compensates))} /><RecordLink id={compensates} />
    </p>
  </PageSection>
{/if}
<EffectsBlock {f} caption={`Effects of ${r.title}`} />
{#if idem}
  <PageSection blockKey="guar" heading="idempotency">
    <DefinitionList>
      <DefinitionRow label="Idempotency">
        {@html idem.value}
        {#if idemHintHtml}<span class="mt-1 block text-xs tracking-[0.32px] text-text-helper">{@html idemHintHtml}</span>{/if}
      </DefinitionRow>
    </DefinitionList>
  </PageSection>
{/if}
<FailureBlock {f} />
<DataTouched {f} carrier={r.type} />
