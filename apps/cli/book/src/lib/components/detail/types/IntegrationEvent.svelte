<script lang="ts">
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Table from "../blocks/Table.svelte";
  import { clsBadgeHtml, codeCls, linkOrTextHtml, tdHtml, typedFieldHtml } from "../helpers";

  // An integration event: its identity and trust, and the payload it carries.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const payload = $derived(((f.payload as Fields[] | undefined) ?? []) as Fields[]);
</script>

<Block blockKey="id" heading="eventType">
  <Kv>
    <KvRow label="Event type" html={`<span class="${codeCls}">${esc(String(f.eventType ?? ""))}</span>`} />
    {#if f.translates}<KvRow label="Translates"><InlineRef id={String(f.translates)} /></KvRow>{/if}
    {#if f.trust}<KvRow label="Trust">{String(f.trust)}</KvRow>{/if}
    {#if f.correlation}<KvRow label="Correlation">{String(f.correlation)}</KvRow>{/if}
    {#if f.dataschema}<KvRow label="Dataschema" html={linkOrTextHtml(f.dataschema)} />{/if}
  </Kv>
</Block>
{#if payload.length}
  <Block blockKey="pay" heading="payload" count={payload.length}>
    <Table
      head={["Name", "Type", "Meaning", "Classification"]}
      rows={payload.map((p) => [
        tdHtml(esc(String(p.name ?? "")), "nm"),
        tdHtml(typedFieldHtml(p)),
        tdHtml(esc(String(p.meaning ?? ""))),
        tdHtml(clsBadgeHtml(p.classification)),
      ])}
    />
  </Block>
{/if}
