<script lang="ts">
  import { book, cn, esc, formatCount, inline, layerStats } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import RecordTreeRow from "./RecordTreeRow.svelte";
  import { indentClass, layerIconOf, type OutlineRow } from "./outline-rows";

  // One virtual row in the outline: a layer header, a layer description, a
  // group header, a record or a note.
  type Props = { row: OutlineRow };
  let { row }: Props = $props();

  const stats = $derived.by(() => {
    void book.rev;
    if (row.kind !== "layer") return null;
    const { ids } = layerStats(book, row.layer);
    return {
      total: ids.length,
      shown: book.anyFilter() ? ids.filter((id) => book.isDirectMatch(id)).length : 0,
      done: ids.filter((id) => book.completeOf(id)).length,
      all: book.order.filter((id) => book.layerOf[id] === row.layer).length,
    };
  });
  const desc = $derived.by(() => {
    if (row.kind !== "desc") return "";
    const meta = book.layerByKey[row.layer];
    return inline(book, meta?.question ?? "") + (meta?.writes ? " Written by " + esc(meta.writes) + "." : "");
  });

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
    class="flex h-[34px] w-full cursor-pointer items-center gap-2 rounded-sm bg-background px-2 text-left text-[0.8125rem] font-semibold hover:bg-muted"
  >
    <span class={cn("grid flex-none text-muted-foreground", here && "text-highlight-foreground")}>
      <RecordIcon name={layerIconOf(row.layer)} class="size-4" />
    </span>
    <span class="flex-none">{meta?.label ?? row.layer}</span>
    {#if loaded && stats}
      <span class="ml-auto font-mono text-[0.6875rem] font-normal whitespace-nowrap text-muted-foreground" title="{stats.done} of {stats.total} complete">
        {book.anyFilter() ? `${stats.shown} shown` : formatCount(stats.total || stats.all)}
      </span>
    {/if}
    <RecordIcon name="chev" class={cn("size-3.5 flex-none text-muted-foreground motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
  </button>
{:else if row.kind === "desc"}
  <p class="mr-2 ml-8 pb-1.5 text-xs leading-relaxed text-muted-foreground">
    {@html desc + " "}<button type="button" data-go={"layer/" + row.layer} class="cursor-pointer text-foreground underline decoration-dotted underline-offset-2">Open</button>
  </p>
{:else if row.kind === "group"}
  <button
    type="button"
    aria-expanded={row.open}
    onclick={() => { book.closedGroups[row.gk] = row.open; }}
    class={cn("flex h-[26px] w-full cursor-pointer items-center gap-1.5 rounded-sm pr-1 text-[0.71875rem] font-medium tracking-[0.02em] text-muted-foreground", indentClass(row.depth))}
  >
    <RecordIcon name="chev" class={cn("size-3.5 flex-none motion-safe:transition-transform motion-safe:duration-150", row.open && "rotate-90")} />
    <span class="min-w-0 flex-1 overflow-hidden text-left text-ellipsis whitespace-nowrap">{row.label}</span>
    {#if row.completion}<span class="flex-none font-normal whitespace-nowrap">{row.completion}</span>{/if}
    <span class="flex-none font-mono text-[0.6875rem]">{row.count}</span>
  </button>
{:else if row.kind === "record"}
  <RecordTreeRow id={row.id} depth={row.depth} level={row.level} kids={row.kids} open={row.open} dim={row.dim} />
{:else if row.kind === "note"}
  <p class="mx-2 flex h-7 items-center text-xs text-muted-foreground">{row.text}</p>
{/if}
