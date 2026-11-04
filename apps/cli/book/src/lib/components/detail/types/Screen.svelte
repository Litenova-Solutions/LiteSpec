<script lang="ts">
  import { base, book, cap } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import SourceLink from "$lib/components/book/SourceLink.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Table from "../blocks/Table.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../helpers";

  // A screen: where it lives, and each use case it offers with whether its page calls it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const off = $derived(((f.offers as string[] | undefined) ?? []).map((x) => base(String(x))));
  const calls = $derived(((r.calls as string[] | undefined) ?? []).map(String));
  const route = $derived((r.route as Fields | undefined) ?? null);
  const tnCls = "font-mono text-[11px] text-muted-foreground";
  const warnCls = "font-medium text-warn-foreground";
</script>

<Block blockKey="where" heading="address">
  <Kv>
    <KvRow label="address" html={`<span class="${codeCls}">${esc(String(f.address ?? f.host ?? ""))}</span>`} />
    {#if f.entryPoint}
      <KvRow label="entryPoint"><InlineRef id={base(String(f.entryPoint))} /></KvRow>
    {/if}
    {#if f.floorplan}
      <KvRow label="floorplan">{cap(String(f.floorplan).replace(/-/g, " "))}</KvRow>
    {/if}
    <KvRow label="Route">
      {#if route}
        <SourceLink src={{ path: String(route.file ?? route.path ?? ""), line: 1 }} />
      {:else}
        <span class={warnCls}>No page carries this screen yet</span>
      {/if}
    </KvRow>
  </Kv>
</Block>
{#if off.length}
  <Block blockKey="offers" heading="offers" count={off.length}>
    <Table
      head={["Use case", "Calls", "Implemented", "Scenarios"]}
      rows={off.map((u) => {
        const q = book.get(base(u));
        const sc = book.scenariosOf(u);
        const b = q?.machine
          ? {
              built: !!((q.machine as Fields).implemented),
              scenarios: sc.length,
              proven: sc.filter((x) => book.get(base(x))?.proof?.state === "awaiting").length,
            }
          : null;
        return [
          tdHtml(inlineRefHtml(u)),
          tdHtml(!route ? `<span class="${tnCls}">no page</span>` : calls.includes(u) ? "Yes" : `<span class="${warnCls}">No</span>`),
          tdHtml(b ? (b.built ? "Yes" : `<span class="${warnCls}">No</span>`) : ""),
          tdHtml(b ? (b.scenarios ? `${b.proven} of ${b.scenarios}` : `<span class="${warnCls}">None</span>`) : ""),
        ];
      })}
    />
  </Block>
{/if}
