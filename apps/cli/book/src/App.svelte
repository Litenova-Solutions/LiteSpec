<script lang="ts">
  import { book, initTheme } from "$lib/frame";
  import { OutlinePanel, outlineState } from "$lib/outline";
  import { ExplainLayer, pageSections } from "$lib/record";
  import { RecordPage } from "$lib/record-page";
  import { IntentTree, RecordRail, completionSectionVisible, traceSectionVisible } from "$lib/trace";
  import {
    delegatedRecordId,
    openRecord,
    reassertFromHash,
    reassertFromHistory,
    scrollToTop,
    syncDocumentTitle,
    TopBar,
    AboutBook,
    PanelWidth,
  } from "$lib/shell";
  import * as Tooltip from "$lib/ui/tooltip";

  // The shell: the top bar over three desktop columns, the navigation, the
  // record page and the rail. The Intent tree and About this book take the
  // page and the rail.
  // History runs through popstate alone, so one Back press selects exactly
  // once; the App restores each record's scroll offset itself.
  history.scrollRestoration = "manual";

  let wide = $state<"intent" | "about" | null>(null);
  let railEl = $state<HTMLElement | null>(null);
  const load = $derived(book.load);

  // The rail reads only when it has anything to say: trace rows, completion,
  // or page contents. PageContents shows at two sections or more, mirrored
  // here so the rail box follows the same rule.
  const railShow = $derived.by(() => {
    const id = book.selected;
    if (!id) return false;
    if (pageSections.items.length >= 2) return true;
    return traceSectionVisible(book, id) || completionSectionVisible(book, id);
  });

  // The resizable panels: the reader drags the navigation's right border
  // or the rail's left border, or moves the focused handle with the arrow
  // keys, and the book remembers each choice under its own key.
  const nav = new PanelWidth({ key: "book-nav-width", defaultWidth: 304, min: 208, max: 560, invert: false });
  const rail = new PanelWidth({ key: "book-rail-width", defaultWidth: 360, min: 280, max: 640, invert: true });

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

  // The book opens in light mode; a reader choice kept from a past visit wins.
  initTheme();

  book.boot(decodeURIComponent(location.hash.slice(1)));

  // Any record link opens its record: data-go buttons and links, and the
  // prose anchors the CLI renders as plain hashes, all by delegation.
  function onPageClick(event: MouseEvent): void {
    const id = delegatedRecordId(event);
    if (!id) return;
    event.preventDefault();
    wide = null;
    void openRecord(id);
  }

  function openIntentTree(): void {
    wide = "intent";
    scrollToTop();
  }

  function closeIntentTree(): void {
    wide = null;
  }

  function openAbout(): void {
    wide = "about";
    scrollToTop();
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
  <div class="min-h-screen bg-background text-foreground" onclick={onPageClick}>
    <p role="status" class="sr-only">{announcement}</p>
    <TopBar onOpenIntentTree={openIntentTree} onOpenAbout={openAbout} onRevealRequest={revealSelected} />
    {#if book.buildError}
      <div role="alert" class="flex items-start gap-3 border-b border-support-error border-l-[3px] bg-note-error px-4 py-2 text-sm">
        <span class="min-w-0 flex-1 break-words">The last build failed: {book.buildError}</span>
        <button type="button" class="shrink-0 underline underline-offset-2 hover:no-underline" onclick={() => (book.buildError = null)}>Dismiss</button>
      </div>
    {/if}
    {#if book.notice}
      <p role="status" class="border-b border-border-subtle px-4 py-2 text-sm text-text-secondary">{book.notice}</p>
    {/if}
    {#if load.kind === "failed"}
      <p class="p-8 text-sm text-text-secondary">The book could not be loaded ({load.error}).</p>
    {:else if load.kind === "loading"}
      <p class="p-8 text-sm text-text-secondary">Loading the book.</p>
    {:else}
      <!-- The outline stays on the left, resizable by its right border, and the
           rail on the right edge, resizable by its left border; the record
           keeps its own width between them, so a wide screen keeps it in the
           middle of the page. The grid keeps the rail column even when the
           rail hides, so the record does not jump. -->
      <div class="grid" style="grid-template-columns: {nav.width}px minmax(0, 1fr) {rail.width}px">
        <aside id="book-nav" aria-label="Navigation" class="sticky top-12 h-[calc(100vh-3rem)] overflow-hidden border-r border-border-subtle bg-layer">
          <OutlinePanel />
          <!-- svelte-ignore a11y_no_noninteractive_tabindex, a11y_no_noninteractive_element_interactions -->
          <!-- A focusable separator is the ARIA window splitter: arrow keys resize the navigation. -->
          <div
            role="separator"
            tabindex={0}
            aria-orientation="vertical"
            aria-label="Navigation width"
            aria-valuemin={nav.min}
            aria-valuemax={nav.max}
            aria-valuenow={nav.width}
            aria-controls="book-nav"
            onpointerdown={(e) => nav.dragStart(e)}
            onkeydown={(e) => nav.keyDown(e)}
            onpointermove={(e) => nav.dragMove(e)}
            onpointerup={() => nav.dragEnd()}
            onpointercancel={() => nav.dragEnd()}
            class="group absolute top-0 right-0 bottom-0 z-10 w-3 cursor-col-resize touch-none"
          >
            <span
              aria-hidden="true"
              class={"absolute top-0 right-0 bottom-0 w-[3px] bg-interactive transition-opacity " +
                (nav.dragging ? "opacity-100" : "opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100")}
            ></span>
          </div>
        </aside>
        {#if wide === "intent"}
          <section class="col-span-2 min-w-0" aria-label="Intent tree"><IntentTree onClose={closeIntentTree} /></section>
        {:else if wide === "about"}
          <main class="col-span-2 min-w-0"><div class="mx-auto w-full max-w-[52rem]"><AboutBook onClose={closeIntentTree} /></div></main>
        {:else}
          <main class="flex min-h-[calc(100vh-3rem)] min-w-0 flex-col">
            <article class="mx-auto w-full max-w-[48rem] flex-1 px-8 pt-6 pb-16 [overflow-wrap:anywhere]">
              <RecordPage />
            </article>
          </main>
          {#if railShow}
            <aside id="book-rail" aria-label="Trace and contents" class="sticky top-12 h-[calc(100vh-3rem)] overflow-hidden border-l border-border-subtle bg-background">
              <div bind:this={railEl} class="h-full overflow-y-auto px-4 py-6">
                <RecordRail />
              </div>
              <!-- svelte-ignore a11y_no_noninteractive_tabindex, a11y_no_noninteractive_element_interactions -->
              <!-- A focusable separator is the ARIA window splitter: arrow keys resize the rail. -->
              <div
                role="separator"
                tabindex={0}
                aria-orientation="vertical"
                aria-label="Rail width"
                aria-valuemin={rail.min}
                aria-valuemax={rail.max}
                aria-valuenow={rail.width}
                aria-controls="book-rail"
                onpointerdown={(e) => rail.dragStart(e)}
                onkeydown={(e) => rail.keyDown(e)}
                onpointermove={(e) => rail.dragMove(e)}
                onpointerup={() => rail.dragEnd()}
                onpointercancel={() => rail.dragEnd()}
                class="group absolute top-0 bottom-0 left-0 z-10 w-3 cursor-col-resize touch-none"
              >
                <span
                  aria-hidden="true"
                  class={"absolute top-0 bottom-0 left-0 w-[3px] bg-interactive transition-opacity " +
                    (rail.dragging ? "opacity-100" : "opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100")}
                ></span>
              </div>
            </aside>
          {/if}
        {/if}
      </div>
    {/if}
  </div>
  <ExplainLayer />
</Tooltip.Provider>
