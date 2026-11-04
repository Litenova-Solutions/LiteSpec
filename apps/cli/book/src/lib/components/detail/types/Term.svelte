<script lang="ts">
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import Block from "../blocks/Block.svelte";
  import Refs from "../blocks/Refs.svelte";
  import Table from "../blocks/Table.svelte";
  import { tdHtml } from "../helpers";

  // A term: its labels, its values, the words it rejects, and its neighbors.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const LANG: Record<string, string> = { nl: "Dutch", de: "German", fr: "French" };
  const values = $derived(((f.values as Fields[] | undefined) ?? []) as Fields[]);
  const rejected = $derived(((f.rejected as Fields[] | undefined) ?? []) as Fields[]);
  const broader = $derived(((f.broader as string[] | undefined) ?? []).map(String));
  const narrower = $derived(((f.narrower as string[] | undefined) ?? []).map(String));
</script>

{#if f.labels}
  <Block blockKey="lab" heading="labels">
    <Table
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
  </Block>
{/if}
{#if values.length}
  <Block blockKey="vals" heading="values" count={values.length}>
    <Table
      head={["Code", "Label"]}
      rows={values.map((v) => [tdHtml(esc(String(v.code ?? "")), "nm"), tdHtml(esc(String(v.label ?? "")))])}
    />
  </Block>
{/if}
{#if rejected.length}
  <Block blockKey="rej" heading="rejected" count={rejected.length}>
    <div class="border-t border-border">
      {#each rejected as x}
        <div class="grid grid-cols-[150px_minmax(0,1fr)] gap-3 border-b border-border py-2.5 text-[13.5px]">
          <s class="font-medium text-warn-foreground [text-decoration-thickness:1.5px]">{String(x.word ?? "")}</s>
          <div><p class="m-0 text-subtle-foreground">{String(x.reason ?? "")}</p></div>
        </div>
      {/each}
    </div>
  </Block>
{/if}
{#if broader.length}
  <Block blockKey="broader" heading="broader" count={broader.length}><Refs ids={broader} /></Block>
{/if}
{#if narrower.length}
  <Block blockKey="narrower" heading="narrower" count={narrower.length}><Refs ids={narrower} /></Block>
{/if}
