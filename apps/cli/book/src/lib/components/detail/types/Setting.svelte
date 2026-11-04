<script lang="ts">
  import { book, cap } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Refs from "../blocks/Refs.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";

  // A setting: its default, its type, and who sets and reads it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const readers = $derived(book.ins(id, "reads"));
  const fallback = $derived(f.secret ? "Hidden" : f.default !== undefined ? String(f.default) + (f.unit ? " " + String(f.unit) : "") : f.value !== undefined ? String(f.value) : "None");
</script>

<Block blockKey="val" heading="default">
  <Tiles>
    <Tile k="default" big>{fallback}</Tile>
    <Tile k="valueType">{cap(String(f.valueType ?? ""))}</Tile>
    {#if f.range}<Tile k="range">{String(f.range)}</Tile>{/if}
    <Tile k="setBy">{cap(String(f.setBy ?? ""))}</Tile>
  </Tiles>
  <Kv>
    {#if f.atBound}<KvRow label="atBound">{String(f.atBound)}</KvRow>{/if}
    {#if readers.length}
      <KvRow label="Read by"><Refs ids={readers} /></KvRow>
    {/if}
    {#if f.runbook}<KvRow label="runbook"><InlineRef id={String(f.runbook)} /></KvRow>{/if}
  </Kv>
</Block>
