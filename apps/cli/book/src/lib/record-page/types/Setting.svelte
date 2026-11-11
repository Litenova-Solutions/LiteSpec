<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { stringField } from "../page-values";

  // A setting: its default, its type, and who sets and reads it.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const readers = $derived(book.ins(id, "reads"));
  const fallback = $derived(
    f.secret
      ? "Hidden"
      : f.default !== undefined
        ? String(f.default) + (f.unit ? " " + String(f.unit) : "")
        : f.value !== undefined
          ? String(f.value)
          : "None",
  );
  const valueType = $derived(stringField(f, "valueType") ?? "");
  const range = $derived(stringField(f, "range"));
  const setBy = $derived(stringField(f, "setBy") ?? "");
  const atBound = $derived(stringField(f, "atBound"));
  const runbook = $derived(stringField(f, "runbook"));
</script>

<PageSection blockKey="val" heading="default">
  <FactGrid>
    <FactCell k="default" big>{fallback}</FactCell>
    <FactCell k="valueType">{capitalize(valueType)}</FactCell>
    {#if range}<FactCell k="range">{range}</FactCell>{/if}
    <FactCell k="setBy">{capitalize(setBy)}</FactCell>
  </FactGrid>
  <DefinitionList>
    {#if atBound}<DefinitionRow label="atBound">{atBound}</DefinitionRow>{/if}
    {#if readers.length}
      <DefinitionRow label="Read by"><RecordList ids={readers} /></DefinitionRow>
    {/if}
    {#if runbook}<DefinitionRow label="runbook"><RecordLink id={runbook} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
