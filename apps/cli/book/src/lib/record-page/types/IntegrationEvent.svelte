<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { esc } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, linkOrTextHtml, tdHtml } from "../page-html";
  import { clsBadgeHtml, typedFieldHtml } from "../page-values";

  // An integration event: its identity and trust, and the payload it carries.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const payload = $derived(((f.payload as RecordFields[] | undefined) ?? []) as RecordFields[]);
</script>

<PageSection blockKey="id" heading="eventType">
  <DefinitionList>
    <DefinitionRow label="Event type" html={`<span class="${codeCls}">${esc(String(f.eventType ?? ""))}</span>`} />
    {#if f.translates}<DefinitionRow label="Translates"><RecordLink id={String(f.translates)} /></DefinitionRow>{/if}
    {#if f.trust}<DefinitionRow label="Trust">{String(f.trust)}</DefinitionRow>{/if}
    {#if f.correlation}<DefinitionRow label="Correlation">{String(f.correlation)}</DefinitionRow>{/if}
    {#if f.dataschema}<DefinitionRow label="Dataschema" html={linkOrTextHtml(f.dataschema)} />{/if}
  </DefinitionList>
</PageSection>
{#if payload.length}
  <PageSection blockKey="pay" heading="payload" count={payload.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Classification"]}
      rows={payload.map((p) => [
        tdHtml(esc(String(p.name ?? "")), "nm"),
        tdHtml(typedFieldHtml(p)),
        tdHtml(esc(String(p.meaning ?? ""))),
        tdHtml(clsBadgeHtml(p.classification)),
      ])}
    />
  </PageSection>
{/if}
