<script lang="ts">
  import { onDestroy } from "svelte";
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Input } from "$lib/ui/input";
  import { Kbd } from "$lib/ui/kbd";

  // The filter box: after 120 ms of quiet the query reaches the store, and Enter
  // opens the best hit. The box shows the store query, so it follows the store
  // when the query clears elsewhere.
  type Props = { input?: HTMLInputElement | null; onRevealRequest: () => void };
  let { input = $bindable<HTMLInputElement | null>(null), onRevealRequest }: Props = $props();
  let timer: ReturnType<typeof setTimeout> | null = null;
  onDestroy(() => {
    if (timer) clearTimeout(timer);
  });

  function changed(value: string): void {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      book.setQuery(value);
      void book.ensureSearch();
    }, 120);
  }

  async function keyed(e: KeyboardEvent): Promise<void> {
    const el = e.target as HTMLInputElement;
    if (e.key === "Escape") {
      if (timer) clearTimeout(timer);
      book.setQuery("");
      el.value = "";
      el.blur();
    } else if (e.key === "Enter") {
      if (timer) clearTimeout(timer);
      book.setQuery(el.value);
      await book.ensureSearch();
      const hit = book.bestSearchHit();
      if (hit) {
        await book.select(hit);
        onRevealRequest();
      }
    } else if (e.key === "ArrowDown") {
      onRevealRequest();
      e.preventDefault();
    }
  }
</script>

<div class="relative min-w-0 flex-[1_1_460px] sm:ml-3 sm:flex-[0_1_460px]">
  <RecordIcon name="search" class="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
  <Input
    bind:ref={input}
    type="search"
    placeholder="Find a record"
    aria-label="Filter the navigation"
    autocomplete="off"
    class="pl-8 sm:pr-10"
    value={book.query}
    onfocus={() => void book.ensureSearch()}
    oninput={(e) => changed((e.target as HTMLInputElement).value)}
    onkeydown={keyed}
  />
  <Kbd class="absolute top-1/2 right-2 hidden -translate-y-1/2 sm:inline-flex" aria-hidden="true">/</Kbd>
</div>
