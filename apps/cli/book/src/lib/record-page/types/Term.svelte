<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { esc } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { tdHtml } from "../page-html";

  // A term: its labels, its values, the words it rejects, and its neighbors.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const LANG: Record<string, string> = { nl: "Dutch", de: "German", fr: "French" };
  const values = $derived(((f.values as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const rejected = $derived(((f.rejected as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const broader = $derived(((f.broader as string[] | undefined) ?? []).map(String));
  const narrower = $derived(((f.narrower as string[] | undefined) ?? []).map(String));
</script>

{#if f.labels}
  <PageSection blockKey="lab" heading="labels">
    <DataTable
      head={["Language", "Word"]}
      rows={[
        [tdHtml("English", "mut"), tdHtml(`<b>${esc(String(f.term ?? ""))}</b>`)],
        ...Object.keys(f.labels as Record<string, string>).map((k) => [
          tdHtml(esc(LANG[k] ?? k), "mut"),
          tdHtml(esc(String((f.labels as Record<string, string>)[k]))),
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
      rows={values.map((v) => [tdHtml(esc(String(v.code ?? "")), "nm"), tdHtml(esc(String(v.label ?? "")))])}
    />
  </PageSection>
{/if}
{#if rejected.length}
  <PageSection blockKey="rej" heading="rejected" count={rejected.length}>
    <div class="border-t border-border">
      {#each rejected as x}
        <div class="grid grid-cols-[150px_minmax(0,1fr)] gap-3 border-b border-border py-2.5 text-[13.5px]">
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
