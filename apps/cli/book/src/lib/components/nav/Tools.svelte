<script lang="ts">
  import { book, GROUPS, type GroupKey } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Button, buttonVariants } from "$lib/components/ui/button";
  import * as Select from "$lib/components/ui/select";
  import * as Tooltip from "$lib/components/ui/tooltip";

  // The navigation tools: the Filter button, the Sort and Group selects, and
  // the expand, collapse and reveal buttons.
  let { pingReveal }: { pingReveal: () => void } = $props();
  const nActive = $derived(Object.values(book.active).reduce((n, v) => n + (v?.length ?? 0), 0));
  const sortKeys = Object.keys(book.sorts);
  const groupKeys = Object.keys(GROUPS) as GroupKey[];
</script>

<div class="mt-2 flex flex-wrap items-center gap-1.5">
  <Button variant="outline" size="sm" aria-expanded={book.facetsOpen} aria-controls="facetPanel" onclick={() => { book.facetsOpen = !book.facetsOpen; }} class="text-xs">
    <Icon name="filter" class="size-3.5" />
    Filter
    {#if nActive > 0}
      <span class="min-w-[18px] rounded-full bg-primary px-[5px] font-mono text-[10.5px] leading-[18px] text-primary-foreground">{nActive}</span>
    {/if}
  </Button>
  {#if book.view === "outline" || book.view === "list"}
    <label class="sr-only" for="sortSel">Sort</label>
    <Select.Root type="single" value={book.sortKey} onValueChange={(v: string) => { book.sortKey = v; }}>
      <Select.Trigger id="sortSel" size="sm" title="Sort" aria-label="Sort" class="min-w-0 flex-1 text-xs">
        {book.sortKey === "outline" ? "Sort: outline" : book.sorts[book.sortKey].label}
      </Select.Trigger>
      <Select.Content>
        {#each sortKeys as k (k)}
          <Select.Item value={k}>{k === "outline" ? "Sort: outline" : book.sorts[k].label}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  {/if}
  {#if book.view === "list"}
    <label class="sr-only" for="grpSel">Group</label>
    <Select.Root type="single" value={book.groupKey} onValueChange={(v: string) => { book.groupKey = v as GroupKey; }}>
      <Select.Trigger id="grpSel" size="sm" title="Group" aria-label="Group" class="min-w-0 flex-1 text-xs">
        {GROUPS[book.groupKey]}
      </Select.Trigger>
      <Select.Content>
        {#each groupKeys as k (k)}
          <Select.Item value={k}>{k === "none" ? GROUPS[k] : "By " + GROUPS[k].toLowerCase()}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  {/if}
  <span class="flex-1" aria-hidden="true"></span>
  {#if book.view === "outline"}
    <Tooltip.Root>
      <Tooltip.Trigger aria-label="Expand all" onclick={() => { book.expandAll(); pingReveal(); }} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
        <Icon name="expand" class="size-4" />
      </Tooltip.Trigger>
      <Tooltip.Content>Expand all (e)</Tooltip.Content>
    </Tooltip.Root>
    <Tooltip.Root>
      <Tooltip.Trigger aria-label="Collapse all" onclick={() => book.collapseAll()} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
        <Icon name="collapse" class="size-4" />
      </Tooltip.Trigger>
      <Tooltip.Content>Collapse all (c)</Tooltip.Content>
    </Tooltip.Root>
  {/if}
  <Tooltip.Root>
    <Tooltip.Trigger aria-label="Show the selected record" onclick={() => { if (book.selected) book.expandTo(book.selected); pingReveal(); }} class={buttonVariants({ variant: "outline", size: "icon-sm" })}>
      <Icon name="locate" class="size-4" />
    </Tooltip.Trigger>
    <Tooltip.Content>Show the selected record</Tooltip.Content>
  </Tooltip.Root>
</div>
