<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { tdHtml } from "../page-html";

  // A requirement: its departures, its measure, and the values it takes over time.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const exc = $derived(((f.except as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const vals = $derived(((f.values as RecordFields[] | undefined) ?? []) as RecordFields[]);
</script>

{#if exc.length}
  <PageSection blockKey="exc" heading="except" count={exc.length}>
    {#each exc as x}
      {@const fb = stripStep(String(x.fallback ?? ""))}
      {@const fx = book.get(fb)}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <RecordIcon name="alert" class="size-3.5" />Except {String(x.condition ?? "")}
        </div>
        <p class="mt-1 mb-0 text-sm text-subtle-foreground">
          Then the fallback holds: <RecordLink id={String(x.fallback ?? "")} />{fx ? ", " + String(fx.fields?.statement ?? "") : ""}
        </p>
      </div>
    {/each}
  </PageSection>
{/if}
{#if f.measure}
  <PageSection blockKey="meas" heading="measure">
    <FactGrid>
      <FactCell k="Measure">{String(f.measure)}</FactCell>
      {#if f.target}<FactCell k="target" big>{String(f.target)}</FactCell>{/if}
      {#if f.dueBy}
        <FactCell k="dueBy" hint={book.dateHint(String(f.dueBy))}>{book.fmtDate(String(f.dueBy))}</FactCell>
      {/if}
    </FactGrid>
    <DefinitionList>
      {#if f.check}<DefinitionRow label="Check">{String(f.check)}</DefinitionRow>{/if}
      {#if f.runsOn}<DefinitionRow label="Runs on">{String(f.runsOn)}</DefinitionRow>{/if}
      {#if f.evidence}<DefinitionRow label="Evidence">{String(f.evidence)}</DefinitionRow>{/if}
    </DefinitionList>
  </PageSection>
{/if}
{#if vals.length}
  <PageSection blockKey="vals" heading="values">
    <DataTable
      head={["Value", "Effective from"]}
      rows={vals.map((v) => [
        tdHtml(esc(v.value)),
        tdHtml(book.fmtDate(String(v.effectiveFrom ?? "")) + " &middot; " + book.dateHint(String(v.effectiveFrom ?? "")), "mut"),
      ])}
    />
  </PageSection>
{/if}
