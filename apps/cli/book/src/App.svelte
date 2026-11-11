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

  // The shell: the top bar over three columns, the navigation, the record page
  // and the rail. The Intent tree takes the page and the rail.
  let wide = $state(false);

  const load = $derived(book.load);
  const announcement = $derived(book.selected ? `Opened ${book.title(book.selected)}` : "");

  book.boot(decodeURIComponent(location.hash.slice(1)));

  // Any record link opens its record: Svelte buttons and links inside rendered
  // prose alike carry data-go.
  function onPageClick(event: MouseEvent): void {
    const id = delegatedRecordId(event);
    if (!id) return;
    event.preventDefault();
    wide = false;
    openRecord(id);
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

  $effect(() => {
    void book.selected;
    scrollToTop();
  });

  $effect(() => {
    syncDocumentTitle(book.selected);
  });
</script>

<svelte:window onpopstate={(e) => reassertFromHistory(e.state)} onhashchange={reassertFromHash} />

<Tooltip.Provider delayDuration={300}>
  <!-- The page handles record-link clicks by delegation: the links inside are real buttons carrying data-go, so they stay keyboard-operable without handlers here. -->
  <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
  <div class="min-h-screen bg-background text-foreground" onclick={onPageClick}>
    <p role="status" class="sr-only">{announcement}</p>
    <TopBar onOpenIntentTree={openIntentTree} onRevealRequest={revealSelected} />
    {#if load.kind === "failed"}
      <p class="p-10 text-sm text-muted-foreground">The book could not be loaded ({load.error}).</p>
    {:else if load.kind === "loading"}
      <p class="p-10 text-sm text-muted-foreground">Loading the book.</p>
    {:else}
      <div class="grid grid-cols-[minmax(300px,400px)_minmax(0,1fr)] xl:grid-cols-[minmax(300px,400px)_minmax(0,1fr)_minmax(280px,350px)]">
        <aside aria-label="Navigation" class="sticky top-14 h-[calc(100vh-3.5rem)] overflow-hidden border-r">
          <OutlinePanel />
        </aside>
        {#if wide}
          <section class="xl:col-span-2" aria-label="Intent tree"><IntentTree onClose={closeIntentTree} /></section>
        {:else}
          <main class="flex min-h-[calc(100vh-3.5rem)] min-w-0 flex-col">
            <article class="mx-auto w-full max-w-[800px] flex-1 px-6 py-8 md:px-12">
              <RecordPage />
            </article>
          </main>
          <aside aria-label="Connections" class="sticky top-14 hidden h-[calc(100vh-3.5rem)] overflow-y-auto border-l px-6 py-7 xl:block">
            <RecordRail />
          </aside>
        {/if}
      </div>
    {/if}
  </div>
  <ExplainLayer />
</Tooltip.Provider>
