<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import * as DropdownMenu from "$lib/ui/dropdown-menu";
  import RunWidgets from "./RunWidgets.svelte";
  import ShortcutKeys from "./ShortcutKeys.svelte";
  import ShortcutsDialog from "./ShortcutsDialog.svelte";
  import RecordSearch from "./RecordSearch.svelte";

  // The top bar: the brand, the search box, the run and completion widgets and
  // the actions. App mounts it above the columns; it reads the store directly.
  let keysOpen = $state(false);
  let searchInput = $state<HTMLInputElement | null>(null);
  let dark = $state(document.documentElement.getAttribute("data-theme") === "dark");

  let projName = $derived.by(() => {
    void book.rev;
    return book.man?.project?.title || book.man?.project?.id || "Book";
  });

  function toggleTheme() {
    dark = !dark;
    document.documentElement.setAttribute("data-theme", dark ? "dark" : "light");
  }
</script>

<header class="sticky top-0 z-30 flex h-14 items-center gap-4 border-b bg-background px-5">
  <div class="flex items-center gap-2.5 whitespace-nowrap text-foreground">
    <svg width="24" height="24" viewBox="0 0 240 240" aria-hidden="true"><path fill="var(--mark-ink)" fill-rule="evenodd" d="M0 0H240V240H0ZM14 14V226H226V14Z" /><path fill="var(--mark-ink)" d="M43.68 60L57.68 60L57.68 166L83.68 166L83.68 180L43.68 180Z" /><path fill="var(--mark-symbol)" d="M97.68 60L196.32 60L196.32 74L97.68 74ZM97.68 113L196.32 113L196.32 127L97.68 127ZM97.68 166L196.32 166L196.32 180L97.68 180Z" /></svg>
    <b class="text-[15px] font-semibold tracking-tight">{projName}</b><span class="text-strong">/</span><span class="font-medium text-muted-foreground">Book</span>
  </div>
  <RecordSearch bind:input={searchInput} />
  <div class="flex-1"></div>
  <RunWidgets />
  <Button
    variant="outline"
    onclick={() => {
      book.wide = true;
      window.scrollTo(0, 0);
    }}>Intent tree</Button
  >
  <DropdownMenu.Root>
    <DropdownMenu.Trigger>
      {#snippet child({ props })}
        <Button {...props} variant="outline">Recent</Button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end" class="w-[340px]">
      <DropdownMenu.Label>Recently opened</DropdownMenu.Label>
      {#if book.recent.length}
        {#each book.recent as id (id)}
          <DropdownMenu.Item onSelect={() => void book.select(id)}>
            <RecordIcon name={book.typeOf(id)} class="text-muted-foreground" />
            <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
            <span class="font-mono text-[11px] whitespace-nowrap text-muted-foreground">{book.typeTag(book.typeOf(id))}</span>
          </DropdownMenu.Item>
        {/each}
      {:else}
        <p class="px-1.5 py-1 text-sm text-muted-foreground">Nothing yet.</p>
      {/if}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
  <Button variant="ghost" size="icon" aria-label="Keyboard shortcuts" onclick={() => (keysOpen = true)}>?</Button>
  <Button
    variant="ghost"
    size="icon"
    aria-label={dark ? "Switch to light theme" : "Switch to dark theme"}
    onclick={toggleTheme}><RecordIcon name={dark ? "sun" : "moon"} /></Button
  >
</header>

<ShortcutsDialog bind:open={keysOpen} />
<ShortcutKeys
  onFocusSearchRequest={(select) => {
    searchInput?.focus();
    if (select) searchInput?.select();
  }}
  onOpenShortcuts={() => (keysOpen = true)}
/>
