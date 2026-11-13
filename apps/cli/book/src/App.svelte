<script lang="ts">
  import { tick } from "svelte";
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
  // and the rail. The Intent tree takes the page and the rail. Below the lg
  // breakpoint the navigation is a drawer the top bar opens, and the rail folds
  // away; above it the columns stand side by side.
  const NARROW = "(max-width: 1023.98px)";
  let wide = $state(false);
  // The drawer stays open only while the record it was opened on stays selected,
  // so choosing a record anywhere closes it.
  let navOpenedOn = $state<string | null | undefined>(undefined);
  const navOpen = $derived(navOpenedOn !== undefined && navOpenedOn === book.selected);
  let narrow = $state(typeof matchMedia === "function" && matchMedia(NARROW).matches);
  let navEl = $state<HTMLElement | null>(null);
  const drawer = $derived(narrow && navOpen);

  $effect(() => {
    if (typeof matchMedia !== "function") return;
    const query = matchMedia(NARROW);
    const changed = () => {
      narrow = query.matches;
      if (!query.matches) navOpenedOn = undefined;
    };
    query.addEventListener("change", changed);
    return () => query.removeEventListener("change", changed);
  });

  function openNav(): void {
    navOpenedOn = book.selected;
    void tick().then(() => navEl?.focus());
  }

  function closeNav(returnFocus: boolean): void {
    if (!navOpen) return;
    navOpenedOn = undefined;
    if (returnFocus) document.getElementById("nav-toggle")?.focus();
  }

  function onWindowKey(event: KeyboardEvent): void {
    if (event.key === "Escape" && drawer) {
      event.preventDefault();
      closeNav(true);
    }
  }

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

<svelte:window onpopstate={(e) => reassertFromHistory(e.state)} onhashchange={reassertFromHash} onkeydown={onWindowKey} />

<Tooltip.Provider delayDuration={300}>
  <!-- The page handles record-link clicks by delegation: the links inside are real buttons carrying data-go, so they stay keyboard-operable without handlers here. -->
  <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
  <div class="min-h-screen bg-background text-foreground" onclick={onPageClick}>
    <p role="status" class="sr-only">{announcement}</p>
    <TopBar
      onOpenIntentTree={openIntentTree}
      onRevealRequest={revealSelected}
      {navOpen}
      onToggleNav={() => (navOpen ? closeNav(true) : openNav())}
    />
    {#if load.kind === "failed"}
      <p class="p-10 text-sm text-muted-foreground">The book could not be loaded ({load.error}).</p>
    {:else if load.kind === "loading"}
      <p class="p-10 text-sm text-muted-foreground">Loading the book.</p>
    {:else}
      <div class="grid grid-cols-[minmax(0,1fr)] lg:grid-cols-[minmax(300px,400px)_minmax(0,1fr)] xl:grid-cols-[minmax(300px,400px)_minmax(0,1fr)_minmax(280px,350px)]">
        {#if drawer}
          <button
            type="button"
            tabindex="-1"
            aria-label="Close the navigation"
            class="fixed inset-x-0 top-14 bottom-0 z-30 bg-foreground/30 lg:hidden"
            onclick={() => closeNav(true)}
          ></button>
        {/if}
        <aside
          id="book-nav"
          bind:this={navEl}
          tabindex="-1"
          aria-label="Navigation"
          class="fixed top-14 bottom-0 left-0 z-40 w-[min(400px,88vw)] overflow-hidden border-r bg-background outline-none duration-200 motion-reduce:transition-none {navOpen
            ? 'visible translate-x-0 shadow-xl transition-[translate]'
            : 'invisible -translate-x-full transition-[translate,visibility]'} lg:visible lg:sticky lg:z-auto lg:h-[calc(100vh-3.5rem)] lg:w-auto lg:translate-x-0 lg:shadow-none lg:transition-none"
        >
          <OutlinePanel />
        </aside>
        {#if wide}
          <section class="min-w-0 xl:col-span-2" aria-label="Intent tree" inert={drawer}><IntentTree onClose={closeIntentTree} /></section>
        {:else}
          <main class="flex min-h-[calc(100vh-3.5rem)] min-w-0 flex-col" inert={drawer}>
            <article class="mx-auto w-full max-w-[800px] flex-1 px-4 py-6 [overflow-wrap:anywhere] sm:px-6 sm:py-8 md:px-12">
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
