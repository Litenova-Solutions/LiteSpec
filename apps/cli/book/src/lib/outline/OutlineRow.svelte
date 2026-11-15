<script lang="ts">
  import { book, cn, formatCount } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import RecordTreeRow from "./RecordTreeRow.svelte";
  import { indentClass, layerIconOf, type OutlineRow } from "./outline-rows";

  // One virtual row in the outline: a layer header, a group header, a
  // record or a note. The row carries its counts; this component renders them.
  type Props = { row: OutlineRow; bestHit?: string | null };
  let { row, bestHit = null }: Props = $props();

  function toggleLayer(key: string): void {
    const opening = !book.openLayers[key];
    book.openLayers[key] = opening;
    if (opening && !book.isLayerLoaded(key)) void book.ensureLayer(key);
  }
</script>

{#if row.kind === "layer"}
  {@const meta = book.layerByKey[row.layer]}
  {@const loaded = book.isLayerLoaded(row.layer)}
  {@const open = loaded ? book.anyFilter() || !!book.openLayers[row.layer] : !!book.openLayers[row.layer]}
  {@const here = !!book.selected && book.layerOf[book.selected] === row.layer}
  <button
    type="button"
    aria-expanded={open}
    aria-current={here ? "page" : undefined}
    onclick={() => toggleLayer(row.layer)}
    class="flex h-9 w-full cursor-pointer items-center gap-2 bg-white px-2 text-left text-sm font-semibold text-slate-900 hover:bg-slate-100"
  >
    <span class="grid flex-none text-slate-600">
      <RecordIcon name={layerIconOf(row.layer)} class="size-4" />
    </span>
    <span class="flex-none">{meta?.label ?? row.layer}</span>
    {#if loaded}
      <span class="ml-auto text-xs font-normal whitespace-nowrap text-slate-600 tabular-nums" title="{row.done} of {row.total} complete">
        {book.anyFilter() ? `${row.shown} shown` : formatCount(row.total)}
      </span>
    {/if}
    <RecordIcon name="chev" class={cn("size-3.5 flex-none text-slate-600 motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
  </button>
{:else if row.kind === "group"}
  <button
    type="button"
    aria-expanded={row.open}
    onclick={() => { book.closedGroups[row.gk] = row.open; }}
    class={cn("flex h-8 w-full cursor-pointer items-center gap-2 pr-1 text-xs font-medium text-slate-600 hover:bg-slate-100", indentClass(row.depth))}
  >
    <RecordIcon name="chev" class={cn("size-3.5 flex-none motion-safe:transition-transform motion-safe:duration-150", row.open && "rotate-90")} />
    <span class="min-w-0 flex-1 overflow-hidden text-left text-ellipsis whitespace-nowrap">{row.label}</span>
    {#if row.completion}<span class="flex-none font-normal whitespace-nowrap">{row.completion}</span>{/if}
    <span class="flex-none text-xs tabular-nums">{row.count}</span>
  </button>
{:else if row.kind === "record"}
  <RecordTreeRow id={row.id} depth={row.depth} level={row.level} kids={row.kids} open={row.open} dim={row.dim} best={bestHit !== null && row.id === bestHit} />
{:else if row.kind === "note"}
  <p class="mx-2 flex h-8 items-center text-xs text-slate-600">{row.text}</p>
{/if}
