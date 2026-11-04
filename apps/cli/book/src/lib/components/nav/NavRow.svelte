<script lang="ts">
  import { book, n0 } from "$lib/book";
  import { esc, inline } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import { LAYER_ICON } from "$lib/components/book/icons";
  import { Button } from "$lib/components/ui/button";
  import { cn } from "$lib/utils";
  import TreeRow from "./TreeRow.svelte";
  import { indentClass, type Row } from "./rows";

  // One virtual row in the outline or list view: a layer header, a layer
  // description, a group header, a record, a note or the Show all button.
  let { row }: { row: Row } = $props();

  const stats = $derived.by(() => {
    void book.rev;
    if (row.kind !== "layer") return null;
    const ids = book.order.filter((id) => book.layerOf[id] === row.layer && book.isReal(id));
    return {
      total: ids.length,
      shown: book.anyFilter() ? ids.filter((id) => book.matches(id)).length : 0,
      done: ids.filter((id) => book.completeOf(id)).length,
      all: book.order.filter((id) => book.layerOf[id] === row.layer).length,
    };
  });
  const desc = $derived.by(() => {
    if (row.kind !== "desc") return "";
    const meta = book.layerByKey[row.layer];
    return inline(book, meta?.question ?? "") + (meta?.writes ? " Written by " + esc(meta.writes) + "." : "");
  });

  function toggleLayer(key: string) {
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
    class="flex h-[34px] w-full cursor-pointer items-center gap-2 rounded-sm bg-background px-2 text-left text-[13px] font-semibold hover:bg-muted"
  >
    <span class={cn("grid flex-none text-muted-foreground", here && "text-highlight-foreground")}>
      <Icon name={LAYER_ICON[row.layer] ?? "core"} class="size-4" />
    </span>
    <span class="flex-none">{meta?.label ?? row.layer}</span>
    {#if loaded && stats}
      <span class="ml-auto font-mono text-[11px] font-normal whitespace-nowrap text-muted-foreground" title="{stats.done} of {stats.total} complete">
        {book.anyFilter() ? `${stats.shown} shown` : n0(stats.total || stats.all)}
      </span>
    {/if}
    <Icon name="chev" class={cn("size-3.5 flex-none text-muted-foreground transition-transform duration-150", open && "rotate-90")} />
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
    class={cn("flex h-[26px] w-full cursor-pointer items-center gap-1.5 rounded-sm pr-1 text-[11.5px] font-medium tracking-[0.02em] text-muted-foreground", indentClass(row.depth))}
  >
    <Icon name="chev" class={cn("size-3.5 flex-none transition-transform duration-150", row.open && "rotate-90")} />
    <span class="min-w-0 flex-1 overflow-hidden text-left text-ellipsis whitespace-nowrap">{row.label}</span>
    {#if row.completion}<span class="flex-none font-normal whitespace-nowrap">{row.completion}</span>{/if}
    <span class="flex-none font-mono text-[11px]">{row.count}</span>
  </button>
{:else if row.kind === "record"}
  <TreeRow id={row.id} depth={row.depth} level={row.level} kids={row.kids} open={row.open} dim={row.dim} />
{:else if row.kind === "listgroup"}
  <div class="flex h-[30px] items-center gap-1.5 px-1.5 text-[11.5px] font-semibold text-subtle-foreground">
    {#if row.icon}<span class="grid flex-none text-muted-foreground"><Icon name={row.icon} class="size-3.5" /></span>{/if}
    <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap">{row.label}</span>
    {#if row.completion}<span class="flex-none font-normal text-muted-foreground">{row.completion}</span>{/if}
    <span class="flex-none font-mono text-[11px] font-normal text-muted-foreground">{row.count}</span>
  </div>
{:else if row.kind === "listrecord"}
  <TreeRow id={row.id} variant="list" />
{:else if row.kind === "note"}
  <p class="mx-2 flex h-7 items-center text-xs text-muted-foreground">{row.text}</p>
{:else}
  <div class="flex h-[50px] items-center px-2">
    <Button variant="outline" class="w-full" onclick={() => { book.listShowAll = true; }}>Show all {row.total} records</Button>
  </div>
{/if}
