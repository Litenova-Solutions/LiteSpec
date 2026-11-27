<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeOf, fieldType, td, thRow } from "../shared/record-cells";
  import { objectList, stringField, stringList } from "../page-values";

  // A module event: the domain event it translates and the fields it selects from it, each with its type there.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const selected = $derived(stringList(f, "fields"));
  const translates = $derived(stringField(f, "translates"));
  const compatibility = $derived(stringField(f, "compatibility") ?? "");
  // The translated domain event's fields by name: each selected field reads its type here.
  const fieldOf = $derived.by(() => {
    const byName: Record<string, RecordFields> = {};
    for (const field of objectList(book.get(translates ?? "")?.fields, "fields")) {
      const name = stringField(field, "name");
      if (name) byName[name] = field;
    }
    return byName;
  });
</script>

{#if translates || compatibility}
  <DefinitionList>
    {#if translates}<DefinitionRow label="Translates"><RecordLink id={translates} /></DefinitionRow>{/if}
    {#if compatibility}<DefinitionRow label="compatibility">{compatibility}</DefinitionRow>{/if}
  </DefinitionList>
{/if}
{#if selected.length}
  <PageSection blockKey="fields" heading="fields" count={selected.length}>
    <DataTable
      head={["Name", "Type"]}
      caption={`Fields of ${r.title}`}
      rows={selected.map((name) => {
        const field = fieldOf[name];
        return [thRow(codeOf(name)), td(field ? fieldType(book, field) : "")];
      })}
    />
  </PageSection>
{/if}
