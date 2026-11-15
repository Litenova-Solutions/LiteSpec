<script lang="ts">
  import { book, esc } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { tdHtml } from "../page-html";
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
  // The outcome, kept apart from delivery: the state, and each observation read from the world.
  const outcome = $derived(r.outcome ?? null);
</script>

{#if why}
  <PageSection blockKey="wim" heading="whyItMatters"><RenderedProse html={why} /></PageSection>
{/if}
<PageSection blockKey="m" heading="measure">
  <FactGrid>
    <FactCell k="Measure">{measure}</FactCell>
    <FactCell k="target">{target}</FactCell>
    {#if dueBy}
      <FactCell k="dueBy" hint={book.dateHint(dueBy)}>
        {book.fmtDate(dueBy)}
      </FactCell>
    {/if}
  </FactGrid>
</PageSection>
{#if outcome}
  <PageSection blockKey="outcome" heading="outcome">
    <FactGrid>
      <FactCell k="Outcome" big>{outcome.state}</FactCell>
    </FactGrid>
    {#if outcome.observations.length}
      <DataTable
        head={["Date", "Value", "Source"]}
        rows={outcome.observations.map((o) => [tdHtml(esc(book.fmtDate(o.date))), tdHtml(esc(o.value)), tdHtml(esc(o.source))])}
      />
    {/if}
  </PageSection>
{/if}
