<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { proseOf } from "../page-prose";
  import { stringField } from "../page-values";

  // A goal: the consequence at stake, and the measure with its target and due date.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const why = $derived(proseOf(r, "whyItMatters"));
  const measure = $derived(stringField(f, "measure") ?? "");
  const target = $derived(stringField(f, "target") ?? "");
  const dueBy = $derived(stringField(f, "dueBy"));
</script>

{#if why}
  <PageSection blockKey="wim" heading="whyItMatters"><RenderedProse html={why} /></PageSection>
{/if}
<PageSection blockKey="m" heading="measure">
  <FactGrid>
    <FactCell k="Measure">{measure}</FactCell>
    <FactCell k="target" big>{target}</FactCell>
    <FactCell k="dueBy" hint={dueBy ? book.dateHint(dueBy) : ""}>
      {dueBy ? book.fmtDate(dueBy) : ""}
    </FactCell>
  </FactGrid>
</PageSection>
