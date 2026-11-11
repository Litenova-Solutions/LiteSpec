<script lang="ts">
  import { book } from "$lib/frame";
  import type { SortKey } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button, buttonVariants } from "$lib/ui/button";
  import * as Select from "$lib/ui/select";
  import * as Tooltip from "$lib/ui/tooltip";
  import { outlineState } from "./outline-state.svelte";

  // The navigation tools: the Filter button, the Sort select, and the expand,
  // collapse and reveal buttons.
  type Props = { onRevealRequest: () => void };
  let { onRevealRequest }: Props = $props();

  const nActive = $derived(Object.values(book.active).reduce((n, v) => n + v.length, 0));
  function isSortKey(value: string): value is SortKey {
    return value === "outline" || value === "title" || value === "proof";
  }
  const sortKeys: SortKey[] = Object.keys(book.sorts).filter(isSortKey);

  function setSortKey(value: string): void {
    const key: SortKey | null = value === "outline" || value === "title" || value === "proof" ? value : null;
    if (key) book.sortKey = key;
  }

  function toggleFacets(): void {
    outlineState.facetsOpen = !outlineState.facetsOpen;
  }

  function expandAndReveal(): void {
    book.expandAll();
    onRevealRequest();
  }

  function revealSelected(): void {
    if (book.selected) book.expandTo(book.selected);
    onRevealRequest();
  }
</script>

<div class="mt-2 flex flex-wrap items-center gap-1.5">
  <Button variant="outline" size="sm" aria-expanded={outlineState.facetsOpen} aria-controls="facetPanel" onclick={toggleFacets} class="text-xs">
    <RecordIcon name="filter" class="size-3.5" />
    Filter
    {#if nActive > 0}
      <span class="min-w-[18px] rounded-full bg-primary px-[5px] font-mono text-[0.65625rem] leading-[18px] text-primary-foreground">{nActive}</span>
    {/if}
  </Button>
  {#if book.view === "outline"}
    <label class="sr-only" for="sortSel">Sort</label>
    <Select.Root type="single" value={book.sortKey} onValueChange={setSortKey}>
      <Select.Trigger id="sortSel" size="sm" title="Sort" aria-label="Sort" class="min-w-0 flex-1 text-xs">
        {book.sortKey === "outline" ? "Sort: outline" : (book.sorts[book.sortKey]?.label ?? book.sortKey)}
      </Select.Trigger>
      <Select.Content>
        {#each sortKeys as k (k)}
          <Select.Item value={k}>{k === "outline" ? "Sort: outline" : (book.sorts[k]?.label ?? k)}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  {/if}
  <span class="flex-1" aria-hidden="true"></span>
  {#if book.view === "outline"}
    <Tooltip.Root>
      <Tooltip.Trigger aria-label="Expand all" onclick={expandAndReveal} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
        <RecordIcon name="expand" class="size-4" />
      </Tooltip.Trigger>
      <Tooltip.Content>Expand all (e)</Tooltip.Content>
    </Tooltip.Root>
    <Tooltip.Root>
      <Tooltip.Trigger aria-label="Collapse all" onclick={() => book.collapseAll()} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
        <RecordIcon name="collapse" class="size-4" />
      </Tooltip.Trigger>
      <Tooltip.Content>Collapse all (c)</Tooltip.Content>
    </Tooltip.Root>
  {/if}
  <Tooltip.Root>
    <Tooltip.Trigger aria-label="Show the selected record" onclick={revealSelected} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
      <RecordIcon name="locate" class="size-4" />
    </Tooltip.Trigger>
    <Tooltip.Content>Show the selected record</Tooltip.Content>
  </Tooltip.Root>
</div>
