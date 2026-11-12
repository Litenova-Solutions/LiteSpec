<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book, esc } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, linkOrTextHtml, tdHtml } from "../page-html";
  import { clsBadgeHtml, objectList, stringField, typedFieldHtml } from "../page-values";

  // An integration event: its identity and trust, and the payload it carries.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const payload = $derived(objectList(f, "payload"));
  const eventType = $derived(stringField(f, "eventType") ?? "");
  const translates = $derived(stringField(f, "translates"));
  const trust = $derived(stringField(f, "trust"));
  const correlation = $derived(stringField(f, "correlation"));
  const dataschema = $derived(f.dataschema);
</script>

<PageSection blockKey="id" heading="eventType">
  <DefinitionList>
    <DefinitionRow label="Event type" html={`<span class="${codeCls}">${esc(eventType)}</span>`} />
    {#if translates}<DefinitionRow label="Translates"><RecordLink id={translates} /></DefinitionRow>{/if}
    {#if trust}<DefinitionRow label="Trust">{trust}</DefinitionRow>{/if}
    {#if correlation}<DefinitionRow label="Correlation">{correlation}</DefinitionRow>{/if}
    {#if dataschema}<DefinitionRow label="Dataschema" html={linkOrTextHtml(book, dataschema)} />{/if}
  </DefinitionList>
</PageSection>
{#if payload.length}
  <PageSection blockKey="pay" heading="payload" count={payload.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Classification"]}
      rows={payload.map((p) => [
        tdHtml(esc(stringField(p, "name") ?? ""), "nm"),
        tdHtml(typedFieldHtml(book, p)),
        tdHtml(esc(stringField(p, "meaning") ?? "")),
        tdHtml(clsBadgeHtml(p.classification)),
      ])}
    />
  </PageSection>
{/if}
