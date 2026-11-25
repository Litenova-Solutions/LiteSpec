<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeOf, fieldType, td, thRow } from "../shared/record-cells";
  import { isRecord, objectList, stringField } from "../page-values";

  // A module event: the domain event it translates and the fields it selects from it, each with its type there.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  // Each selected field with its own override when the entry states one: a string entry carries no override.
  const selected = $derived.by(() => {
    const list = f["fields"];
    if (!Array.isArray(list)) return [];
    const out: { name: string; override?: RecordFields }[] = [];
    for (const entry of list) {
      if (typeof entry === "string") {
        if (entry) out.push({ name: entry });
      } else if (isRecord(entry)) {
        const name = stringField(entry, "name");
        if (name) out.push({ name, override: entry });
      }
    }
    return out;
  });
  const translates = $derived(stringField(f, "translates"));
  const compatibility = $derived(stringField(f, "compatibility") ?? "");
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
      rows={selected.map((field) => {
        const carry = carryOf[field.name];
        const shown = field.override?.type ? field.override : carry;
        return [thRow(codeOf(field.name)), td(shown ? fieldType(book, shown) : "")];
      })}
    />
  </PageSection>
{/if}
