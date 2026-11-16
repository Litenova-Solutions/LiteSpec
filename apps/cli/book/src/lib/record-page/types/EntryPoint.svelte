<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import NavigationMap from "../shared/NavigationMap.svelte";
  import { codeOf, refLink, td, thRow } from "../shared/record-cells";
  import { objectList, screenFields, stringField, stringList } from "../page-values";

  // An entry point: the content it serves with the requirement each entry
  // fulfills, and the navigation map of its screens.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const served = $derived(objectList(f, "servedContent"));
  // The screens of this entry point in outline order: the screens placed
  // below it, and the screens naming it as their entry point.
  const screens = $derived.by(() => {
    const kids = new Set(book.kids(id));
    return book.order.filter((x) => {
      if (book.typeOf(x) !== "screen") return false;
      if (kids.has(x)) return true;
      return stripStep(stringField(book.get(x)?.fields, "entryPoint") ?? "") === id;
    });
  });
  const nodes = $derived(screens.map((x) => ({ id: x, title: book.title(x) })));
  const leads = $derived.by(() => {
    const out: Record<string, string[]> = {};
    for (const x of screens) out[x] = stringList(book.get(x)?.fields, "leadsTo").map((t) => stripStep(t));
    return out;
  });
  // The start screen is the one at the root address; the map marks it, and
  // shows no marker until the data carries that screen.
  const start = $derived.by(() => {
    for (const x of screens) {
      if (screenFields(book.get(x)?.fields ?? {}).address === "/") return x;
    }
    return null;
  });

  function fulfillsCell(entry: RecordFields): string {
    const ids = stringList(entry, "fulfills");
    return td(ids.length ? ids.map((x) => refLink(book, x)).join(", ") : "");
  }
</script>

{#if served.length}
  <PageSection blockKey="served" heading="servedContent" count={served.length}>
    <DataTable
      head={["Path", "Content type", "Cache control", "Versioning", "Fulfills"]}
      caption={`Content the ${r.title} entry point serves`}
      rows={served.map((entry) => [
        thRow(codeOf(stringField(entry, "path") ?? "")),
        td(esc(stringField(entry, "contentType") ?? "")),
        td(esc(stringField(entry, "cacheControl") ?? "")),
        td(esc(stringField(entry, "versioning") ?? "")),
        fulfillsCell(entry),
      ])}
    />
  </PageSection>
{/if}
<PageSection blockKey="map" heading="Navigation map" count={screens.length > 0 ? screens.length : null}>
  {#if screens.length === 0}
    <p class="text-sm text-text-secondary">No screens.</p>
  {:else}
    <NavigationMap screens={nodes} {leads} {start} />
  {/if}
</PageSection>
