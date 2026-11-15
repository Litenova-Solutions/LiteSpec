<script lang="ts">
  import { book, capitalize, esc } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../page-html";
  import { clsBadgeHtml, freshnessNoteHtml, objectList, stringField, stringList, typedFieldHtml } from "../page-values";

  // A read model: the rows one module keeps from another module's published facts, the
  // field each row takes from an integration event, and what removes a row.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const keyedBy = $derived(stringField(f, "keyedBy"));
  const freshness = $derived(freshnessNoteHtml(f));
  const removedBy = $derived(stringList(f, "removedBy"));
  const fields = $derived(objectList(f, "fields"));

  // A "from" value names the integration event and the payload field, as integration-event/<name>#field.
  function fromHtml(from: string | undefined): string {
    const [event = "", field = ""] = (from ?? "").split("#");
    const link = event ? inlineRefHtml(book, event) : "";
    return field ? `${link} <span class="font-mono text-xs text-slate-600">#${esc(field)}</span>` : link;
  }
</script>

<PageSection blockKey="id" heading="keyedBy">
  <DefinitionList>
    {#if keyedBy}<DefinitionRow label="keyedBy"><RecordLink id={keyedBy} /></DefinitionRow>{/if}
    {#if freshness}<DefinitionRow label="freshness" html={freshness} />{/if}
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
      rows={fields.map((x) => [
        tdHtml(esc(stringField(x, "name") ?? ""), "nm"),
        tdHtml(typedFieldHtml(book, x)),
        tdHtml(fromHtml(stringField(x, "from"))),
        tdHtml(`<span class="${codeCls}">${esc(capitalize(stringField(x, "accumulate") ?? "latest"))}</span>`),
        tdHtml(clsBadgeHtml(x.classification)),
        tdHtml(stringField(x, "erasedBy") ? inlineRefHtml(book, stringField(x, "erasedBy") ?? "") : ""),
      ])}
    />
  </PageSection>
{/if}
