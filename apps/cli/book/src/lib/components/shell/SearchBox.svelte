<script lang="ts">
  import { onDestroy } from "svelte";
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Input } from "$lib/components/ui/input";
  import { Kbd } from "$lib/components/ui/kbd";

  // The filter box: after 120 ms of quiet the query reaches the store, and Enter
  // opens the best hit. The box follows the store when the query clears elsewhere.
  let { input = $bindable<HTMLInputElement | null>(null) }: { input?: HTMLInputElement | null } = $props();
  let timer: ReturnType<typeof setTimeout> | null = null;
  onDestroy(() => {
    if (timer) clearTimeout(timer);
  });

  $effect(() => {
    const q = book.query;
    if (input && document.activeElement !== input && input.value !== q) input.value = q;
  });

  function changed(value: string) {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      book.setQuery(value);
      void book.ensureSearch();
    }, 120);
  }

  async function keyed(e: KeyboardEvent) {
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
      const hit = book.bestHit();
      if (hit) {
        await book.select(hit);
        book.focusNavTick++;
      }
    } else if (e.key === "ArrowDown") {
      book.focusNavTick++;
      e.preventDefault();
    }
  }
</script>

<div class="relative ml-3 min-w-0 flex-[0_1_460px]">
  <Icon name="search" class="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
  <Input
    bind:ref={input}
    type="search"
    placeholder="Find a record by title or text"
    aria-label="Filter the navigation"
    autocomplete="off"
    class="pr-10 pl-8"
    onfocus={() => void book.ensureSearch()}
    oninput={(e) => changed((e.target as HTMLInputElement).value)}
    onkeydown={keyed}
  />
  <Kbd class="absolute top-1/2 right-2 -translate-y-1/2" aria-hidden="true">/</Kbd>
</div>
