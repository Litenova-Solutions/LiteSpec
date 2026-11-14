<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { esc } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { tdHtml } from "../page-html";
  import { objectField, objectList, stringField, stringList } from "../page-values";

  // A term: its labels, its values, the words it rejects, and its neighbors.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const LANG: Record<string, string> = { nl: "Dutch", de: "German", fr: "French" };
  const labels = $derived(objectField(f, "labels"));
  const term = $derived(stringField(f, "term") ?? "");
  const values = $derived(objectList(f, "values"));
  const rejected = $derived(objectList(f, "rejected"));
  const broader = $derived(stringList(f, "broader"));
  const narrower = $derived(stringList(f, "narrower"));
</script>

{#if labels}
  <PageSection blockKey="lab" heading="labels">
    <DataTable
      head={["Language", "Word"]}
      rows={[
        [tdHtml("English", "mut"), tdHtml(`<b>${esc(term)}</b>`)],
        ...Object.keys(labels).map((k) => [
          tdHtml(esc(LANG[k] ?? k), "mut"),
          tdHtml(esc(String(labels[k] ?? ""))),
        ]),
      ]}
    />
    <p class="mt-2 text-xs text-muted-foreground">
      Adding a language changes no other record.
    </p>
  </PageSection>
{/if}
{#if values.length}
  <PageSection blockKey="vals" heading="values" count={values.length}>
    <DataTable
      head={["Code", "Label"]}
      rows={values.map((v) => [tdHtml(esc(stringField(v, "code") ?? ""), "nm"), tdHtml(esc(stringField(v, "label") ?? ""))])}
    />
  </PageSection>
{/if}
{#if rejected.length}
  <PageSection blockKey="rej" heading="rejected" count={rejected.length}>
    <div class="border-t border-border">
      {#each rejected as x, i (String(x.word ?? i))}
        <div class="grid grid-cols-[minmax(0,1fr)] gap-x-3 gap-y-1 sm:grid-cols-[150px_minmax(0,1fr)] border-b border-border py-2.5 text-[0.84375rem]">
          <s class="font-medium text-warn-foreground [text-decoration-thickness:1.5px]">{String(x.word ?? "")}</s>
          <div><p class="m-0 text-subtle-foreground">{String(x.reason ?? "")}</p></div>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if broader.length}
  <PageSection blockKey="broader" heading="broader" count={broader.length}><RecordList ids={broader} /></PageSection>
{/if}
{#if narrower.length}
  <PageSection blockKey="narrower" heading="narrower" count={narrower.length}><RecordList ids={narrower} /></PageSection>
{/if}
