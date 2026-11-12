<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { tdHtml } from "../page-html";
  import { requirementFields, stringField } from "../page-values";

  // A requirement: its departures, its measure, and the values it takes over time.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const qf = $derived(requirementFields(r.fields ?? {}));
</script>

{#if qf.except.length}
  <PageSection blockKey="exc" heading="except" count={qf.except.length}>
    {#each qf.except as x, i (i)}
      {@const fallback = stringField(x, "fallback") ?? ""}
      {@const fb = stripStep(fallback)}
      {@const fx = book.get(fb)}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <RecordIcon name="alert" class="size-3.5" />Except {stringField(x, "condition") ?? ""}
        </div>
        <p class="mt-1 mb-0 text-sm text-subtle-foreground">
          Then the fallback holds: <RecordLink id={fallback} />{fx ? ", " + String(fx.fields?.statement ?? "") : ""}
        </p>
      </div>
    {/each}
  </PageSection>
{/if}
{#if qf.measure}
  <PageSection blockKey="meas" heading="measure">
    <FactGrid>
      <FactCell k="Measure">{qf.measure}</FactCell>
      {#if qf.target}<FactCell k="target" big>{qf.target}</FactCell>{/if}
      {#if qf.dueBy}
        <FactCell k="dueBy" hint={book.dateHint(qf.dueBy)}>{book.fmtDate(qf.dueBy)}</FactCell>
      {/if}
    </FactGrid>
    <DefinitionList>
      {#if qf.check}<DefinitionRow label="Check">{qf.check}</DefinitionRow>{/if}
      {#if qf.runsOn}<DefinitionRow label="Runs on">{qf.runsOn}</DefinitionRow>{/if}
      {#if qf.evidence}<DefinitionRow label="Evidence">{qf.evidence}</DefinitionRow>{/if}
    </DefinitionList>
  </PageSection>
{/if}
{#if qf.values.length}
  <PageSection blockKey="vals" heading="values">
    <DataTable
      head={["Value", "Effective from"]}
      rows={qf.values.map((v) => {
        const effectiveFrom = stringField(v, "effectiveFrom") ?? "";
        return [
          tdHtml(esc(v.value)),
          tdHtml(book.fmtDate(effectiveFrom) + " &middot; " + book.dateHint(effectiveFrom), "mut"),
        ];
      })}
    />
  </PageSection>
{/if}
