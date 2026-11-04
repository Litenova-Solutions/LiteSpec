<script lang="ts">
  import { book } from "$lib/book";
  import ExplainLayer from "$lib/components/book/ExplainLayer.svelte";
  import Detail from "$lib/components/detail/Detail.svelte";
  import Sidebar from "$lib/components/nav/Sidebar.svelte";
  import Rail from "$lib/components/rail/Rail.svelte";
  import VerdictBar from "$lib/components/review/VerdictBar.svelte";
  import TopBar from "$lib/components/shell/TopBar.svelte";
  import IntentTree from "$lib/components/tree/IntentTree.svelte";
  import * as Tooltip from "$lib/components/ui/tooltip";

  // The shell: the top bar over three columns, the navigation, the record page with
  // its verdict bar, and the rail. The Intent tree takes the page and the rail.
  let main = $state<HTMLElement | null>(null);

  book.boot(decodeURIComponent(location.hash.slice(1)));

  // Any record link opens its record: Svelte buttons and links inside rendered
  // prose alike carry data-go.
  function go(e: MouseEvent) {
    const t = (e.target as Element | null)?.closest?.("[data-go]") as HTMLElement | null;
    if (!t?.dataset.go) return;
    e.preventDefault();
    book.wide = false;
    void book.select(t.dataset.go);
  }

  $effect(() => {
    void book.selected;
    window.scrollTo(0, 0);
  });

  $effect(() => {
    const name = book.man?.project?.title || book.man?.project?.id;
    document.title = (name || "Book") + " Book";
  });
</script>

<svelte:window
  onpopstate={(e) => {
    const id = (e.state as { id?: string } | null)?.id || decodeURIComponent(location.hash.slice(1));
    if (id) void book.select(id, { fromHistory: true });
  }} />

<Tooltip.Provider delayDuration={300}>
  <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
  <div class="min-h-screen bg-background text-foreground" onclick={go}>
    <TopBar />
    {#if book.error}
      <p class="p-10 text-sm text-muted-foreground">The book could not be loaded ({book.error}).</p>
    {:else if !book.ready}
      <p class="p-10 text-sm text-muted-foreground">Loading the book.</p>
    {:else}
      <div class="grid grid-cols-[minmax(300px,400px)_minmax(0,1fr)] xl:grid-cols-[minmax(300px,400px)_minmax(0,1fr)_minmax(280px,350px)]">
        <aside aria-label="Navigation" class="sticky top-14 h-[calc(100vh-3.5rem)] overflow-hidden border-r">
          <Sidebar />
        </aside>
        {#if book.wide}
          <section class="xl:col-span-2" aria-label="Intent tree"><IntentTree /></section>
        {:else}
          <main bind:this={main} class="flex min-h-[calc(100vh-3.5rem)] min-w-0 flex-col">
            <article class="mx-auto w-full max-w-[800px] flex-1 px-6 py-8 md:px-12">
              <Detail />
            </article>
            <div class="sticky bottom-0 border-t bg-background/95 backdrop-blur">
              <VerdictBar />
            </div>
          </main>
          <aside aria-label="Connections" class="sticky top-14 hidden h-[calc(100vh-3.5rem)] overflow-y-auto border-l px-6 py-7 xl:block">
            <Rail />
          </aside>
        {/if}
      </div>
    {/if}
  </div>
  <ExplainLayer />
</Tooltip.Provider>
