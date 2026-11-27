<script lang="ts">
  import { book, capitalize } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { objectField, stringField } from "../page-values";

  // A setting: its default, its type, and who sets and reads it.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const readers = $derived(book.ins(id, "reads"));
  const value = $derived(objectField(f, "value"));
  const fallback = $derived(
    f.secret
      ? "Hidden"
      : value?.default !== undefined
        ? String(value.default) + (value.unit ? " " + String(value.unit) : "")
        : "None",
  );
  const valueType = $derived(stringField(value, "type") ?? "");
  const range = $derived(stringField(value, "range"));
  const setBy = $derived(stringField(f, "setBy") ?? "");
  const atBound = $derived(stringField(value, "outOfRange"));
  const runbook = $derived(stringField(f, "runbook"));
</script>

<PageSection blockKey="val" heading="default">
  <FactGrid>
    <FactCell k="default">{fallback}</FactCell>
    <FactCell k="type">{capitalize(valueType)}</FactCell>
    {#if range}<FactCell k="range">{range}</FactCell>{/if}
    <FactCell k="setBy">{capitalize(setBy)}</FactCell>
  </FactGrid>
  <DefinitionList>
    {#if atBound}<DefinitionRow label="outOfRange">{atBound}</DefinitionRow>{/if}
    {#if readers.length}
      <DefinitionRow label="Read by">
        {#each readers as x, i (x)}{#if i > 0}{", "}{/if}<RecordLink id={x} />{/each}
      </DefinitionRow>
    {/if}
    {#if runbook}<DefinitionRow label="runbook"><RecordLink id={runbook} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
