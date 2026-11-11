<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";

  // A setting: its default, its type, and who sets and reads it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const readers = $derived(book.ins(id, "reads"));
  const fallback = $derived(f.secret ? "Hidden" : f.default !== undefined ? String(f.default) + (f.unit ? " " + String(f.unit) : "") : f.value !== undefined ? String(f.value) : "None");
</script>

<PageSection blockKey="val" heading="default">
  <FactGrid>
    <FactCell k="default" big>{fallback}</FactCell>
    <FactCell k="valueType">{capitalize(String(f.valueType ?? ""))}</FactCell>
    {#if f.range}<FactCell k="range">{String(f.range)}</FactCell>{/if}
    <FactCell k="setBy">{capitalize(String(f.setBy ?? ""))}</FactCell>
  </FactGrid>
  <DefinitionList>
    {#if f.atBound}<DefinitionRow label="atBound">{String(f.atBound)}</DefinitionRow>{/if}
    {#if readers.length}
      <DefinitionRow label="Read by"><RecordList ids={readers} /></DefinitionRow>
    {/if}
    {#if f.runbook}<DefinitionRow label="runbook"><RecordLink id={String(f.runbook)} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
