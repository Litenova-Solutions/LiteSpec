<script lang="ts">
  import { book, capitalize, esc } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { clsTag, codeOf, fieldType, refLink, td, thRow } from "../shared/record-cells";
  import { freshnessDecidedBy, freshnessNoteHtml, objectList, stringField, stringList } from "../page-values";

  // A read model: the rows one module keeps from another module's published facts, the
  // field each row takes from a module event or integration event, and what removes a row.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const keyedBy = $derived(stringField(f, "keyedBy"));
  const freshness = $derived(freshnessNoteHtml(f));
  const decidedBy = $derived(freshnessDecidedBy(f));
  const removedBy = $derived(stringList(f, "removedBy"));
  const fields = $derived(objectList(f, "fields"));

  // A "from" value names the module event or integration event and the published field, as module-event/<module>.<name>#field.
  function fromHtml(from: string | undefined): string {
    const [event = "", field = ""] = (from ?? "").split("#");
    const link = event ? refLink(book, event) : "";
    return field ? `${link} <span class="font-mono text-xs text-text-helper">#${esc(field)}</span>` : link;
  }
</script>

<PageSection blockKey="id" heading="keyedBy">
  <DefinitionList>
    {#if keyedBy}<DefinitionRow label="keyedBy"><RecordLink id={keyedBy} /></DefinitionRow>{/if}
    {#if freshness}<DefinitionRow label="freshness" html={freshness} />{/if}
    {#if decidedBy}<DefinitionRow label="decidedBy"><RecordLink id={decidedBy} /></DefinitionRow>{/if}
    {#if removedBy.length}
      <DefinitionRow label="removedBy">
        {#each removedBy as event (event)}<div><RecordLink id={event} /></div>{/each}
      </DefinitionRow>
    {/if}
  </DefinitionList>
</PageSection>
{#if fields.length}
  <PageSection blockKey="rows" heading="fields" count={fields.length}>
    <DataTable
      head={["Name", "Type", "From", "Accumulate", "Classification", "Erased by"]}
      caption={`Fields of ${r.title}`}
      rows={fields.map((x) => [
        thRow(codeOf(stringField(x, "name") ?? "")),
        td(fieldType(book, x)),
        td(fromHtml(stringField(x, "from"))),
        td(codeOf(capitalize(stringField(x, "accumulate") ?? "latest"))),
        td(clsTag(x.classification)),
        td(stringField(x, "erasedBy") ? refLink(book, stringField(x, "erasedBy") ?? "") : ""),
      ])}
    />
  </PageSection>
{/if}
