<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import * as DropdownMenu from "$lib/ui/dropdown-menu";
  import { openRecord } from "./record-navigation";
  import RunWidgets from "./RunWidgets.svelte";
  import ShortcutKeys from "./ShortcutKeys.svelte";
  import ShortcutsDialog from "./ShortcutsDialog.svelte";
  import RecordSearch from "./RecordSearch.svelte";

  // The top bar: the brand, the build state, the search box, the run and
  // completion widgets and the actions. App mounts it above the columns; it
  // reads the store directly. Desktop only: every part always shows.
  type Props = { onOpenIntentTree: () => void; onRevealRequest: () => void };
  let { onOpenIntentTree, onRevealRequest }: Props = $props();
  let keysOpen = $state(false);
  let searchInput = $state<HTMLInputElement | null>(null);

  const projName = $derived.by(() => {
    void book.rev;
    return book.man?.project?.title || book.man?.project?.id || "Book";
  });
</script>

<header class="sticky top-0 z-30 flex h-14 items-center gap-4 border-b border-slate-300 bg-white px-4">
  <div class="flex shrink-0 items-center gap-2.5 whitespace-nowrap">
    <svg class="shrink-0" width="24" height="24" viewBox="0 0 240 240" aria-hidden="true"><path fill="var(--mark-ink)" fill-rule="evenodd" d="M0 0H240V240H0ZM14 14V226H226V14Z" /><path fill="var(--mark-ink)" d="M43.68 60L57.68 60L57.68 166L83.68 166L83.68 180L43.68 180Z" /><path fill="var(--mark-symbol)" d="M97.68 60L196.32 60L196.32 74L97.68 74ZM97.68 113L196.32 113L196.32 127L97.68 127ZM97.68 166L196.32 166L196.32 180L97.68 180Z" /></svg>
    <b class="max-w-[18ch] truncate text-sm font-semibold tracking-tight text-slate-900" title={projName}>{projName}</b><span class="text-slate-300">/</span><span class="font-medium text-slate-600">Book</span>
  </div>
  <p class="flex shrink-0 items-center gap-2 text-xs whitespace-nowrap text-slate-600" role="status">
    {#if book.building}Rebuilding{:else}{book.builtLabel()}{/if}
  </p>
  <RecordSearch bind:input={searchInput} {onRevealRequest} />
  <div class="flex-1"></div>
  <div class="flex items-center gap-4"><RunWidgets /></div>
  <Button id="intent-tree-open" variant="outline" aria-label="Intent tree" onclick={onOpenIntentTree}
    ><RecordIcon name="tree" /><span>Intent tree</span></Button
  >
  <DropdownMenu.Root>
    <DropdownMenu.Trigger>
      {#snippet child({ props })}
        <Button {...props} variant="outline" aria-label="Recent"><RecordIcon name="clock" /><span>Recent</span></Button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end" class="w-[340px]">
      <DropdownMenu.Label>Recently opened</DropdownMenu.Label>
      {#if book.recent.length}
        {#each book.recent as id (id)}
          <DropdownMenu.Item onSelect={() => openRecord(id)}>
            <RecordIcon name={book.typeOf(id)} class="text-slate-600" />
            <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
            <span class="text-xs whitespace-nowrap text-slate-600">{book.typeTag(book.typeOf(id))}</span>
          </DropdownMenu.Item>
        {/each}
      {:else}
        <p class="px-1.5 py-1 text-sm text-slate-600">Nothing yet.</p>
      {/if}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
  <Button variant="ghost" size="icon" aria-label="Keyboard shortcuts" onclick={() => (keysOpen = true)}>?</Button>
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
