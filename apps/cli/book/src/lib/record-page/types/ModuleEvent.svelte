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
  // The translated domain event's carries by name: each selected field reads its type here.
  const carryOf = $derived.by(() => {
    const byName: Record<string, RecordFields> = {};
    for (const carry of objectList(book.get(translates ?? "")?.fields, "carries")) {
      const name = stringField(carry, "name");
      if (name) byName[name] = carry;
    }
    return byName;
  });
</script>

{#if translates}
  <DefinitionList>
    <DefinitionRow label="Translates"><RecordLink id={translates} /></DefinitionRow>
  </DefinitionList>
{/if}
{#if selected.length}
  <PageSection blockKey="fields" heading="fields" count={selected.length}>
    <DataTable
      head={["Name", "Type"]}
      caption={`Fields of ${r.title}`}
      rows={selected.map((name) => {
        const carry = carryOf[name];
        return [thRow(codeOf(name)), td(carry ? fieldType(book, carry) : "")];
      })}
    />
  </PageSection>
{/if}
