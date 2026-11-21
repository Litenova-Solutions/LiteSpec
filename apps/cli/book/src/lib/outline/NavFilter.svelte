<script lang="ts">
  import { RecordIcon } from "$lib/record";
  import { outlineState } from "./outline-state.svelte";

  // The navigation filter box: while it holds text the outline tree shows
  // only the rows whose title or identifier holds the text, with their
  // ancestors. The clear button and Escape empty it and restore the tree.
  function keyed(e: KeyboardEvent): void {
    if (e.key === "Escape") outlineState.navFilter = "";
  }
</script>

<div class="relative mt-2">
  <RecordIcon name="search" class="pointer-events-none absolute top-1/2 left-2 size-4 -translate-y-1/2 text-text-secondary" />
  <input
    type="search"
    placeholder="Filter navigation"
    aria-label="Filter navigation"
    value={outlineState.navFilter}
    oninput={(e) => { outlineState.navFilter = (e.target as HTMLInputElement).value; }}
    onkeydown={keyed}
    class="h-8 w-full border-b border-border-strong bg-field-on-layer pr-8 pl-8 text-sm text-foreground outline-none placeholder:text-text-helper focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-focus [&::-webkit-search-cancel-button]:appearance-none"
  />
  {#if outlineState.navFilter}
    <button
      type="button"
      aria-label="Clear the navigation filter"
      onclick={() => { outlineState.navFilter = ""; }}
      class="absolute top-1/2 right-1 grid size-6 -translate-y-1/2 cursor-pointer place-items-center text-text-secondary hover:text-foreground"
    >
      <RecordIcon name="x" class="size-4" />
    </button>
  {/if}
</div>
