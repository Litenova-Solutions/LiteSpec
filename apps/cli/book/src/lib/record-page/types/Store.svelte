<script lang="ts">
  import { capitalize, esc, formatLabel } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { clsTag, codeOf, muted, td, thRow } from "../shared/record-cells";
  import { objectList, stringList } from "../page-values";
  import { periodSuffixHtml } from "../page-values";

  // A store: its data classes and data.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const dataClasses = $derived(objectList(f, "dataClasses"));
  const data = $derived(objectList(f, "data"));
  const classRows = $derived(
    dataClasses.map((c) => [
      thRow(`<b>${esc(capitalize(formatLabel(c.name)))}</b>${c.classification ? muted(clsTag(c.classification)) : ""}`),
      td(stringList(c, "fields").map(codeOf).join(" ")),
      td(esc(c.purpose ?? "")),
      td(
        `<b>${esc(capitalize(String(c.onErasure ?? "")))}</b>${periodSuffixHtml(c.period)}` +
          (c.basis || c.reason ? muted(esc(String(c.basis ?? c.reason ?? ""))) : ""),
      ),
    ]),
  );
  const dataRows = $derived(
    data.map((d) => [thRow(codeOf(String(d.name ?? ""))), td(esc(String(d.meaning ?? ""))), td(clsTag(d.classification))]),
  );
</script>

{#if dataClasses.length}
  <PageSection blockKey="classes" heading="dataClasses" count={dataClasses.length}>
    <DataTable head={["Data class", "Fields", "Purpose", "Erasure"]} caption={`Data classes of ${r.title}`} rows={classRows} />
  </PageSection>
{/if}
{#if data.length}
  <PageSection blockKey="data" heading="data" count={data.length}>
    <DataTable head={["Name", "Meaning", "Classification"]} caption={`Data of ${r.title}`} rows={dataRows} />
  </PageSection>
{/if}
