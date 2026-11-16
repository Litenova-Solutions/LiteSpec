<script lang="ts">
  import { book, cn } from "$lib/frame";
  import type { IntentGrouping } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import * as Tooltip from "$lib/ui/tooltip";
  import FilterMatch from "./FilterMatch.svelte";
  import { outlineState } from "./outline-state.svelte";
  import RecordTreeRow from "./RecordTreeRow.svelte";
  import RowGuides from "./RowGuides.svelte";
  import { groupTip, indentClass, layerTip, type OutlineRow } from "./outline-rows";

  // One virtual row in the outline: a layer header, a group header, a
  // record, a note or the Intent grouping switch. Counts and completion sit
  // in tooltips and accessible names; the rows show none.
  type Props = { row: OutlineRow; bestHit?: string | null };
  let { row, bestHit = null }: Props = $props();
  const navFiltering = $derived(outlineState.navFilter.trim() !== "");

  function toggleLayer(key: string): void {
    const opening = !book.openLayers[key];
    book.openLayers[key] = opening;
    if (opening && !book.isLayerLoaded(key)) void book.ensureLayer(key);
  }

  // The Intent grouping switch: the outline order or one group per product area.
  const GROUP_OPTIONS = [
    { key: "outline", label: "Outline", icon: "tree" },
    { key: "product-area", label: "Product area", icon: "folder" },
  ] as const;

  // Arrow keys move between the two grouping buttons and select as they move.
  function groupKey(e: KeyboardEvent): void {
    if (e.key !== "ArrowLeft" && e.key !== "ArrowRight") return;
    e.preventDefault();
    const next: IntentGrouping = e.key === "ArrowRight" ? "product-area" : "outline";
    book.setIntentGrouping(next);
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.closest("[data-intent-grouping]")?.querySelector<HTMLElement>(`[data-grouping="${next}"]`)?.focus();
    }
  }
</script>

{#if row.kind === "layer"}
  {@const meta = book.layerByKey[row.layer]}
  {@const loaded = book.isLayerLoaded(row.layer)}
  {@const open = loaded ? book.anyFilter() || navFiltering || !!book.openLayers[row.layer] : !!book.openLayers[row.layer]}
  {@const here = !!book.selected && book.layerOf[book.selected] === row.layer}
  {@const tip = loaded ? layerTip(meta?.label ?? row.layer, row.total, row.shown, row.done, book.anyFilter()) : (meta?.label ?? row.layer)}
  <Tooltip.Root>
    <Tooltip.Trigger>
      {#snippet child({ props })}
        <button
          {...props}
          type="button"
          aria-expanded={open}
          aria-current={here ? "page" : undefined}
          aria-label={tip}
          onclick={() => toggleLayer(row.layer)}
          class="flex h-8 w-full cursor-pointer items-center gap-2 px-3 text-left text-xs font-semibold text-text-secondary hover:bg-layer-hover"
        >
          <span class="min-w-0 flex-1 truncate"><FilterMatch text={meta?.label ?? row.layer} /></span>
        </button>
      {/snippet}
    </Tooltip.Trigger>
    <Tooltip.Content side="right" sideOffset={8}>{tip}</Tooltip.Content>
  </Tooltip.Root>
{:else if row.kind === "group"}
  {@const tip = groupTip(row.label, row.count, row.completion)}
  <Tooltip.Root>
    <Tooltip.Trigger>
      {#snippet child({ props })}
        <button
          {...props}
          type="button"
          aria-expanded={row.open}
          aria-label={tip}
          onclick={() => { book.closedGroups[row.gk] = row.open; }}
          class={cn("group relative flex h-7 w-full cursor-pointer items-center gap-2 pr-2 text-left text-xs text-text-secondary hover:bg-layer-hover", indentClass(row.depth))}
        >
          <RowGuides guides={row.guides} strong={row.branch} />
          <span class="min-w-0 flex-1 truncate"><FilterMatch text={row.label} /></span>
          <span class="grid h-7 w-5 flex-none place-items-center">
            <RecordIcon name="chev" class={cn("size-4 motion-safe:transition-transform motion-safe:duration-150", row.open && "rotate-90")} />
          </span>
        </button>
      {/snippet}
    </Tooltip.Trigger>
    <Tooltip.Content side="right" sideOffset={8}>{tip}</Tooltip.Content>
  </Tooltip.Root>
{:else if row.kind === "record"}
  <RecordTreeRow id={row.id} depth={row.depth} level={row.level} kids={row.kids} open={row.open} dim={row.dim} best={bestHit !== null && row.id === bestHit} guides={row.guides} branch={row.branch} />
{:else if row.kind === "note"}
  <p class={cn("flex h-7 items-center pr-2 text-xs text-text-helper", indentClass(row.depth))}><FilterMatch text={row.text} /></p>
{:else if row.kind === "switch"}
  {@const grouping = book.intentGrouping}
  <div role="group" aria-label="Intent grouping" data-intent-grouping class={cn("flex h-7 items-center gap-1 pr-2", indentClass(1))}>
    {#each GROUP_OPTIONS as opt (opt.key)}
      <Tooltip.Root>
        <Tooltip.Trigger>
          {#snippet child({ props })}
            <button
              {...props}
              type="button"
              data-grouping={opt.key}
              aria-label={opt.label}
              aria-pressed={grouping === opt.key}
              onclick={() => book.setIntentGrouping(opt.key)}
              onkeydown={groupKey}
              class={cn(
                "grid size-6 cursor-pointer place-items-center",
                grouping === opt.key ? "bg-foreground text-background" : "text-text-secondary hover:bg-layer-hover hover:text-foreground",
              )}
            >
              <RecordIcon name={opt.icon} class="size-4" />
            </button>
          {/snippet}
        </Tooltip.Trigger>
        <Tooltip.Content side="bottom">{opt.label}</Tooltip.Content>
      </Tooltip.Root>
    {/each}
  </div>
{/if}
