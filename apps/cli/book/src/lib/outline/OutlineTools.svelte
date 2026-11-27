<script lang="ts">
  import { book } from "$lib/frame";
  import type { SortKey } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import * as Select from "$lib/ui/select";
  import { outlineState } from "./outline-state.svelte";

  // The navigation tools: the Filter button and the Sort select. The record
  // count and the expand, collapse and reveal buttons sit in the filter panel
  // beneath, beside the counts they act on.
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
</script>

<div class="mt-2 flex items-center gap-2">
  <button
    type="button"
    aria-label="Filter"
    aria-expanded={outlineState.facetsOpen}
    aria-controls="facetPanel"
    onclick={toggleFacets}
    class="grid size-8 flex-none cursor-pointer place-items-center text-foreground hover:bg-hover-wash"
  >
    <RecordIcon name="filter" class="size-4" />
  </button>
  {#if book.view === "outline"}
    <label class="sr-only" for="sortSel">Sort</label>
    <Select.Root type="single" value={book.sortKey} onValueChange={setSortKey}>
      <Select.Trigger
        id="sortSel"
        aria-label="Sort"
        class="h-8 min-w-0 flex-1 gap-3 rounded-none border-0 border-b border-border-strong bg-field-on-layer py-0 pr-2 pl-4 text-sm focus-visible:ring-0"
      >
        <span class="flex-none text-text-secondary">Sort</span>
        <span class="min-w-0 flex-1 truncate text-left">{book.sorts[book.sortKey]?.label ?? book.sortKey}</span>
      </Select.Trigger>
      <Select.Content class="rounded-none">
        {#each sortKeys as k (k)}
          <Select.Item value={k} class="h-10 rounded-none py-0 pr-10 pl-4">{book.sorts[k]?.label ?? k}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  {/if}
</div>
