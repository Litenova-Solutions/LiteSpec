<script lang="ts">
  import { book } from "$lib/frame";
  import { OutlinePanel, outlineState } from "$lib/outline";
  import { ExplainLayer } from "$lib/record";
  import { RecordPage } from "$lib/record-page";
  import { IntentTree, RecordRail } from "$lib/trace";
  import {
    delegatedRecordId,
    openRecord,
    reassertFromHash,
    reassertFromHistory,
    scrollToTop,
    syncDocumentTitle,
    TopBar,
  } from "$lib/shell";
  import * as Tooltip from "$lib/ui/tooltip";

  // The shell: the top bar over three desktop columns, the navigation, the
  // record page and the rail. The Intent tree takes the page and the rail.
  // History runs through popstate alone, so one Back press selects exactly
  // once; the App restores each record's scroll offset itself.
  history.scrollRestoration = "manual";

  let wide = $state(false);
  let railEl = $state<HTMLElement | null>(null);
  const load = $derived(book.load);

  // The one announcement path, at most one text per 300 ms: selections that
  // keep focus in the outline announce the record, and selections that move
  // focus to the title clear the live region instead of announcing twice.
  let announcement = $state("");
  let announceTimer: ReturnType<typeof setTimeout> | null = null;
  let lastAnnouncedAt = 0;
  let lastRevealTick = 0;

  // Each record keeps its window offset; Back returns to it, a new move goes
  // to the top. The rail always restarts at the top.
  const scrollOffsets = new Map<string, number>();
  let prevSelected: string | null = null;
  let historyNav = false;

  book.boot(decodeURIComponent(location.hash.slice(1)));

  // Any record link opens its record: data-go buttons and links, and the
  // prose anchors the CLI renders as plain hashes, all by delegation.
  function onPageClick(event: MouseEvent): void {
    const id = delegatedRecordId(event);
    if (!id) return;
    event.preventDefault();
    wide = false;
    void openRecord(id);
  }

  function openIntentTree(): void {
    wide = true;
    scrollToTop();
  }

  function closeIntentTree(): void {
    wide = false;
  }

  function revealSelected(): void {
    outlineState.requestReveal();
  }

  /** Whether a history entry names its record in state, as the book's own entries do. */
  function hasHistoryId(state: unknown): boolean {
    return typeof state === "object" && state !== null && "id" in state;
  }

  // The one history path: entries the book wrote reassert from state, and
  // thinner entries (the initial load, file://) reassert from the hash.
  // Restoration arms only when the target differs from the open record.
  function onHistory(state: unknown): void {
    const before = book.selected;
    const target = hasHistoryId(state) ? reassertFromHistory(state) : reassertFromHash();
    historyNav = target !== null && target !== before;
  }

  /** Whether focus sits inside the outline tree, as the record page's own focus rule reads it. */
  function treeHasFocus(): boolean {
    const active = document.activeElement;
    return active instanceof HTMLElement && active.closest('[role="tree"]') !== null;
  }

  $effect(() => {
    const id = book.selected;
    const tick = outlineState.revealRequest;
    const rail = railEl;
    if (id === prevSelected) {
      lastRevealTick = tick;
      return;
    }
    if (prevSelected !== null) scrollOffsets.set(prevSelected, window.scrollY);
    prevSelected = id;
    if (rail) rail.scrollTop = 0;
    if (id === null) scrollToTop();
    else if (historyNav) {
      historyNav = false;
      window.scrollTo(0, scrollOffsets.get(id) ?? 0);
    } else scrollToTop();
    const keepTreeFocus = tick !== lastRevealTick || treeHasFocus();
    lastRevealTick = tick;
    if (announceTimer) {
      clearTimeout(announceTimer);
      announceTimer = null;
    }
    if (id === null || !keepTreeFocus) {
      announcement = "";
      return;
    }
    const text = `Opened ${book.title(id)}`;
    const wait = 300 - (Date.now() - lastAnnouncedAt);
    if (wait <= 0) {
      announcement = text;
      lastAnnouncedAt = Date.now();
    } else {
      announceTimer = setTimeout(() => {
        announcement = text;
        lastAnnouncedAt = Date.now();
      }, wait);
    }
  });

  $effect(() => {
    syncDocumentTitle(book.selected);
  });
</script>

<svelte:window onpopstate={(e) => onHistory(e.state)} />

<Tooltip.Provider delayDuration={300}>
  <!-- The page handles record-link clicks by delegation: data-go buttons and prose anchors alike open their record. -->
  <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
  <div class="min-h-screen bg-slate-100 text-slate-900" onclick={onPageClick}>
    <p role="status" class="sr-only">{announcement}</p>
    <TopBar onOpenIntentTree={openIntentTree} onRevealRequest={revealSelected} />
    {#if book.buildError}
      <div role="alert" class="flex items-start gap-3 border-b border-warn-border bg-warn px-4 py-2 text-sm text-warn-foreground">
        <span class="min-w-0 flex-1 break-words">The last build failed: {book.buildError}</span>
        <button type="button" class="shrink-0 underline underline-offset-2 hover:no-underline" onclick={() => (book.buildError = null)}>Dismiss</button>
      </div>
    {/if}
    {#if book.notice}
      <p role="status" class="border-b border-slate-200 px-4 py-2 text-sm text-slate-600">{book.notice}</p>
    {/if}
    {#if load.kind === "failed"}
      <p class="p-8 text-sm text-slate-600">The book could not be loaded ({load.error}).</p>
    {:else if load.kind === "loading"}
      <p class="p-8 text-sm text-slate-600">Loading the book.</p>
    {:else}
      <div class="grid grid-cols-[minmax(300px,400px)_minmax(0,1fr)_minmax(280px,350px)]">
        <aside id="book-nav" aria-label="Navigation" class="sticky top-14 h-[calc(100vh-3.5rem)] overflow-hidden border-r border-slate-300 bg-white">
          <OutlinePanel />
        </aside>
        {#if wide}
          <section class="col-span-2 min-w-0" aria-label="Intent tree"><IntentTree onClose={closeIntentTree} /></section>
        {:else}
          <main class="flex min-h-[calc(100vh-3.5rem)] min-w-0 flex-col">
            <article class="mx-auto w-full max-w-[800px] flex-1 px-8 py-8 [overflow-wrap:anywhere]">
              <RecordPage />
            </article>
          </main>
          <aside aria-label="Connections" bind:this={railEl} class="sticky top-14 h-[calc(100vh-3.5rem)] overflow-y-auto border-l border-slate-300 bg-white px-4 py-6">
            <RecordRail />
          </aside>
        {/if}
      </div>
    {/if}
  </div>
  <ExplainLayer />
</Tooltip.Provider>
