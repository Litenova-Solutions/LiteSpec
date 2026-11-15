<script lang="ts">
  import { book, cn, formatCount } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import RecordTreeRow from "./RecordTreeRow.svelte";
  import { indentClass, type OutlineRow } from "./outline-rows";

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
    class="flex h-9 w-full cursor-pointer items-center gap-2 px-4 text-left text-xs font-semibold text-text-secondary hover:bg-layer-hover"
  >
    <span class="min-w-0 flex-1 truncate">{meta?.label ?? row.layer}</span>
    {#if loaded}
      <span class="flex-none font-normal whitespace-nowrap tabular-nums">
        {book.anyFilter() ? `${row.shown} shown` : formatCount(row.total)}
      </span>
    {/if}
  </button>
{:else if row.kind === "group"}
  <button
    type="button"
    aria-expanded={row.open}
    onclick={() => { book.closedGroups[row.gk] = row.open; }}
    class={cn("flex h-8 w-full cursor-pointer items-center gap-2 pr-4 text-left text-xs text-text-secondary hover:bg-layer-hover", indentClass(row.depth))}
  >
    <RecordIcon name="chev" class={cn("size-4 flex-none motion-safe:transition-transform motion-safe:duration-150", row.open && "rotate-90")} />
    <span class="min-w-0 flex-1 truncate">{row.label}</span>
    {#if row.completion}<span class="flex-none font-normal whitespace-nowrap text-text-helper">{row.completion}</span>{/if}
    <span class="flex-none tabular-nums text-text-helper">{row.count}</span>
  </button>
{:else if row.kind === "record"}
  <RecordTreeRow id={row.id} depth={row.depth} level={row.level} kids={row.kids} open={row.open} dim={row.dim} best={bestHit !== null && row.id === bestHit} />
{:else if row.kind === "note"}
  <p class="flex h-8 items-center px-4 text-xs text-text-helper">{row.text}</p>
{/if}
