<script lang="ts">
  import { base, book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Table from "../blocks/Table.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import { tdHtml } from "../helpers";

  // A requirement: its departures, its measure, and the values it takes over time.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const exc = $derived(((f.except as Fields[] | undefined) ?? []) as Fields[]);
  const vals = $derived(((f.values as Fields[] | undefined) ?? []) as Fields[]);
</script>

{#if exc.length}
  <Block blockKey="exc" heading="except" count={exc.length}>
    {#each exc as x}
      {@const fb = base(String(x.fallback ?? ""))}
      {@const fx = book.get(fb)}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <Icon name="alert" class="size-3.5" />Except {String(x.condition ?? "")}
        </div>
        <p class="mt-1 mb-0 text-sm text-subtle-foreground">
          Then the fallback holds: <InlineRef id={String(x.fallback ?? "")} />{fx ? ", " + String(fx.fields?.statement ?? "") : ""}
        </p>
      </div>
    {/each}
  </Block>
{/if}
{#if f.measure}
  <Block blockKey="meas" heading="measure">
    <Tiles>
      <Tile k="Measure">{String(f.measure)}</Tile>
      {#if f.target}<Tile k="target" big>{String(f.target)}</Tile>{/if}
      {#if f.dueBy}
        <Tile k="dueBy" hint={book.dateHint(String(f.dueBy))}>{book.fmtDate(String(f.dueBy))}</Tile>
      {/if}
    </Tiles>
    <Kv>
      {#if f.check}<KvRow label="Check">{String(f.check)}</KvRow>{/if}
      {#if f.runsOn}<KvRow label="Runs on">{String(f.runsOn)}</KvRow>{/if}
      {#if f.evidence}<KvRow label="Evidence">{String(f.evidence)}</KvRow>{/if}
    </Kv>
  </Block>
{/if}
{#if vals.length}
  <Block blockKey="vals" heading="values">
    <Table
      head={["Value", "Effective from"]}
      rows={vals.map((v) => [
        tdHtml(esc(v.value)),
        tdHtml(book.fmtDate(String(v.effectiveFrom ?? "")) + " &middot; " + book.dateHint(String(v.effectiveFrom ?? "")), "mut"),
      ])}
    />
  </Block>
{/if}
