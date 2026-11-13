<script lang="ts">
  import { book, theme } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import * as DropdownMenu from "$lib/ui/dropdown-menu";
  import { openRecord } from "./record-navigation";
  import RunWidgets from "./RunWidgets.svelte";
  import ShortcutKeys from "./ShortcutKeys.svelte";
  import ShortcutsDialog from "./ShortcutsDialog.svelte";
  import RecordSearch from "./RecordSearch.svelte";

  // The top bar: the brand, the search box, the run and completion widgets and
  // the actions. App mounts it above the columns; it reads the store directly.
  // Below lg it leads with the button that opens the navigation drawer, and the
  // secondary parts give way as the width narrows: the widgets, then the button
  // labels, the book label and the shortcut hints.
  type Props = { onOpenIntentTree: () => void; onRevealRequest: () => void; navOpen: boolean; onToggleNav: () => void };
  let { onOpenIntentTree, onRevealRequest, navOpen, onToggleNav }: Props = $props();
  let keysOpen = $state(false);
  let searchInput = $state<HTMLInputElement | null>(null);

  const projName = $derived.by(() => {
    void book.rev;
    return book.man?.project?.title || book.man?.project?.id || "Book";
  });
</script>

<header class="sticky top-0 z-30 flex h-14 items-center gap-2 border-b bg-background px-3 sm:gap-4 sm:px-5">
  <Button
    id="nav-toggle"
    variant="ghost"
    size="icon"
    class="-ml-1 lg:hidden"
    aria-label={navOpen ? "Close the navigation" : "Open the navigation"}
    aria-expanded={navOpen}
    aria-controls="book-nav"
    onclick={onToggleNav}><RecordIcon name={navOpen ? "x" : "menu"} /></Button
  >
  <div class="flex shrink-0 items-center gap-2.5 whitespace-nowrap text-foreground">
    <svg class="shrink-0" width="24" height="24" viewBox="0 0 240 240" aria-hidden="true"><path fill="var(--mark-ink)" fill-rule="evenodd" d="M0 0H240V240H0ZM14 14V226H226V14Z" /><path fill="var(--mark-ink)" d="M43.68 60L57.68 60L57.68 166L83.68 166L83.68 180L43.68 180Z" /><path fill="var(--mark-symbol)" d="M97.68 60L196.32 60L196.32 74L97.68 74ZM97.68 113L196.32 113L196.32 127L97.68 127ZM97.68 166L196.32 166L196.32 180L97.68 180Z" /></svg>
    <b class="hidden max-w-[18ch] truncate text-[0.9375rem] font-semibold tracking-tight sm:inline" title={projName}>{projName}</b><span class="hidden text-strong md:inline">/</span><span class="hidden font-medium text-muted-foreground md:inline">Book</span>
  </div>
  <RecordSearch bind:input={searchInput} {onRevealRequest} />
  <div class="hidden flex-1 sm:block"></div>
  <div class="hidden items-center gap-4 xl:flex"><RunWidgets /></div>
  <Button variant="outline" aria-label="Intent tree" title="Intent tree" class="max-md:size-9 max-md:px-0" onclick={onOpenIntentTree}
    ><RecordIcon name="tree" class="md:hidden" /><span class="max-md:sr-only">Intent tree</span></Button
  >
  <DropdownMenu.Root>
    <DropdownMenu.Trigger>
      {#snippet child({ props })}
        <Button {...props} variant="outline" aria-label="Recent" title="Recent" class="max-md:size-9 max-md:px-0"
          ><RecordIcon name="clock" class="md:hidden" /><span class="max-md:sr-only">Recent</span></Button
        >
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end" class="w-[min(340px,calc(100vw-1.5rem))]">
      <DropdownMenu.Label>Recently opened</DropdownMenu.Label>
      {#if book.recent.length}
        {#each book.recent as id (id)}
          <DropdownMenu.Item onSelect={() => openRecord(id)}>
            <RecordIcon name={book.typeOf(id)} class="text-muted-foreground" />
            <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
            <span class="font-mono text-[0.6875rem] whitespace-nowrap text-muted-foreground">{book.typeTag(book.typeOf(id))}</span>
          </DropdownMenu.Item>
        {/each}
      {:else}
        <p class="px-1.5 py-1 text-sm text-muted-foreground">Nothing yet.</p>
      {/if}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
  <Button variant="ghost" size="icon" class="max-md:hidden" aria-label="Keyboard shortcuts" onclick={() => (keysOpen = true)}>?</Button>
  <Button
    variant="ghost"
    size="icon"
    aria-label={theme.dark ? "Switch to light theme" : "Switch to dark theme"}
    onclick={() => theme.toggle()}><RecordIcon name={theme.dark ? "sun" : "moon"} /></Button
  >
</header>

<ShortcutsDialog bind:open={keysOpen} />
<ShortcutKeys
  onFocusSearchRequest={(select) => {
    searchInput?.focus();
    if (select) searchInput?.select();
  }}
  onOpenShortcuts={() => (keysOpen = true)}
  {onRevealRequest}
/>
