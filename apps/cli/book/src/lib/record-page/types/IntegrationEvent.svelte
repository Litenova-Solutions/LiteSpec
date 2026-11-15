<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book, esc, safeHref } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { clsTag, codeOf, fieldType, muted, td, thRow } from "../shared/record-cells";
  import { objectList, stringField } from "../page-values";

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
  const dataschemaHtml = $derived.by(() => {
    if (dataschema === undefined || dataschema === null || String(dataschema) === "") return "";
    const href = safeHref(book, dataschema);
    return href
      ? `<a href="${esc(href)}" class="text-link hover:underline">${esc(dataschema)}</a>`
      : esc(dataschema);
  });

  function payloadQualifiers(p: RecordFields): string {
    const quals = [
      p.required === true ? "required" : p.required === false ? "optional" : "",
      p.many ? "many" : "",
    ].filter(Boolean);
    return quals.length ? muted(quals.map(esc).join(" &middot; ")) : "";
  }
</script>

<PageSection blockKey="id" heading="eventType">
  <DefinitionList>
    <DefinitionRow label="Event type" html={codeOf(eventType)} />
    {#if translates}<DefinitionRow label="Translates"><RecordLink id={translates} /></DefinitionRow>{/if}
    {#if trust}<DefinitionRow label="Trust">{trust}</DefinitionRow>{/if}
    {#if correlation}<DefinitionRow label="Correlation">{correlation}</DefinitionRow>{/if}
    {#if dataschemaHtml}<DefinitionRow label="Dataschema" html={dataschemaHtml} />{/if}
  </DefinitionList>
</PageSection>
{#if payload.length}
  <PageSection blockKey="pay" heading="payload" count={payload.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Classification"]}
      caption={`Payload of ${r.title}`}
      rows={payload.map((p) => [
        thRow(codeOf(stringField(p, "name") ?? "") + payloadQualifiers(p)),
        td(fieldType(book, p)),
        td(esc(stringField(p, "meaning") ?? "")),
        td(clsTag(p.classification)),
      ])}
    />
  </PageSection>
{/if}
