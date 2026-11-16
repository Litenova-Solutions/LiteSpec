<script lang="ts">
  import { applyTheme, book, currentTheme, type BookTheme } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";
  import * as DropdownMenu from "$lib/ui/dropdown-menu";
  import { openRecord } from "./record-navigation";
  import RunWidgets from "./RunWidgets.svelte";
  import ShortcutKeys from "./ShortcutKeys.svelte";
  import RecordSearch from "./RecordSearch.svelte";

  // The top bar: the brand, the build state, the search box, the completion
  // meter and the Intent tree, Recent, theme and About actions. App mounts it
  // above the columns; it reads the store directly.
  type Props = { onOpenIntentTree: () => void; onOpenAbout: () => void; onRevealRequest: () => void };
  let { onOpenIntentTree, onOpenAbout, onRevealRequest }: Props = $props();
  let searchInput = $state<HTMLInputElement | null>(null);
  let theme = $state<BookTheme>(currentTheme());

  function toggleTheme(): void {
    const next: BookTheme = theme === "dark" ? "light" : "dark";
    theme = next;
    applyTheme(next);
  }

  const projName = $derived.by(() => {
    void book.rev;
    return book.man?.project?.title || book.man?.project?.id || "Book";
  });
</script>

<header class="sticky top-0 z-30 flex h-12 items-stretch border-b border-shell-border bg-shell text-shell-foreground">
  <div class="flex shrink-0 items-center gap-2 pr-6 pl-4 text-sm whitespace-nowrap">
    <svg class="shrink-0" width="24" height="24" viewBox="0 0 240 240" aria-hidden="true"><path fill="var(--shell-foreground)" fill-rule="evenodd" d="M0 0H240V240H0ZM14 14V226H226V14Z" /><path fill="var(--shell-foreground)" d="M43.68 60L57.68 60L57.68 166L83.68 166L83.68 180L43.68 180Z" /><path fill="var(--mark-symbol)" d="M97.68 60L196.32 60L196.32 74L97.68 74ZM97.68 113L196.32 113L196.32 127L97.68 127ZM97.68 166L196.32 166L196.32 180L97.68 180Z" /></svg>
    <span class="flex items-baseline gap-1">
      <span class="max-w-[18ch] truncate font-normal">{projName}</span><b class="font-semibold">Book</b>
    </span>
  </div>
  <p class="flex shrink-0 items-center pr-4 text-xs whitespace-nowrap text-shell-secondary" role="status">
    {#if book.building}Rebuilding{:else}{book.builtLabel()}{/if}
  </p>
  <RecordSearch bind:input={searchInput} {onRevealRequest} />
  <div class="flex-1"></div>
  <RunWidgets />
  <button id="intent-tree-open" type="button" aria-label="Intent tree" onclick={onOpenIntentTree} class="grid size-12 shrink-0 place-items-center hover:bg-shell-hover">
    <RecordIcon name="tree" class="size-5" />
  </button>
  <DropdownMenu.Root>
    <DropdownMenu.Trigger>
      {#snippet child({ props })}
        <button {...props} type="button" aria-label="Recent" class="grid size-12 shrink-0 place-items-center hover:bg-shell-hover">
          <RecordIcon name="clock" class="size-5" />
        </button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end" class="w-[340px] rounded-none p-0 ring-0">
      <DropdownMenu.Label class="px-5 py-3 text-sm font-semibold text-text-secondary">Recently opened</DropdownMenu.Label>
      {#if book.recent.length}
        {#each book.recent as id (id)}
          <DropdownMenu.Item class="h-10 gap-3 rounded-none px-5 py-0 text-sm text-text-secondary" onSelect={() => openRecord(id)}>
            <TypeTag type={book.typeOf(id)} />
            <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
          </DropdownMenu.Item>
        {/each}
      {:else}
        <p class="px-5 py-3 text-sm text-text-helper">Nothing yet.</p>
      {/if}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
  <button type="button" aria-label={theme === "dark" ? "Switch to light mode" : "Switch to dark mode"} onclick={toggleTheme} class="grid size-12 shrink-0 place-items-center hover:bg-shell-hover">
    <RecordIcon name={theme === "dark" ? "sun" : "moon"} class="size-5" />
  </button>
  <button type="button" aria-label="About this book" onclick={onOpenAbout} class="grid size-12 shrink-0 place-items-center hover:bg-shell-hover">
    <RecordIcon name="info" class="size-5" />
  </button>
</header>

<ShortcutKeys
  onFocusSearchRequest={(select) => {
    searchInput?.focus();
    if (select) searchInput?.select();
  }}
  {onRevealRequest}
/>
