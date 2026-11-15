<script lang="ts">
  import { onDestroy } from "svelte";
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Input } from "$lib/ui/input";
  import { Kbd } from "$lib/ui/kbd";
  import { openRecord } from "./record-navigation";

  // The filter box as a combobox over the outline: after 120 ms of quiet the
  // query reaches the store, Enter opens the best hit, and ArrowDown opens
  // the first direct match. Both land with focus in the outline on the opened
  // row. The box shows the store query, so it follows the store when the query
  // clears elsewhere.
  type Props = { input?: HTMLInputElement | null; onRevealRequest: () => void };
  let { input = $bindable<HTMLInputElement | null>(null), onRevealRequest }: Props = $props();
  const DEBOUNCE_MS = 120;
  let timer: ReturnType<typeof setTimeout> | null = null;
  // The latest value waiting for the index; a stale timer never reaches the store.
  let pending = "";
  onDestroy(() => {
    if (timer) clearTimeout(timer);
  });

  function changed(value: string): void {
    pending = value;
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      timer = null;
      void applyQuery(value);
    }, DEBOUNCE_MS);
  }

  // The index builds once, in chunks; the query waits for it, so results never
  // flicker between the substring fallback and full-text matches.
  async function applyQuery(value: string): Promise<void> {
    await book.ensureSearch();
    if (pending !== value) return;
    book.setQuery(value);
  }

  // The pending keystrokes reach the store now. False when the reader typed on
  // while the index built; the debounce owns the newer value then.
  async function settleQuery(el: HTMLInputElement): Promise<boolean> {
    if (timer) {
      clearTimeout(timer);
      timer = null;
    }
    const value = el.value;
    pending = value;
    await book.ensureSearch();
    if (pending !== value) return false;
    book.setQuery(value);
    return true;
  }

  // Reveal now, and again on the next frame: the record page focuses the title
  // in the same flush, and the second reveal lands after it, on the row.
  function revealInOutline(): void {
    onRevealRequest();
    requestAnimationFrame(() => onRevealRequest());
  }

  async function keyed(e: KeyboardEvent): Promise<void> {
    const el = e.target as HTMLInputElement;
    if (e.key === "Escape") {
      if (timer) {
        clearTimeout(timer);
        timer = null;
      }
      pending = "";
      book.setQuery("");
      el.value = "";
      el.blur();
    } else if (e.key === "Enter") {
      if (!(await settleQuery(el))) return;
      const hit = book.bestSearchHit();
      if (hit) {
        await openRecord(hit);
        revealInOutline();
      }
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      if (!(await settleQuery(el))) return;
      if (!book.query) {
        onRevealRequest();
        return;
      }
      const match = book.firstMatch() ?? book.bestSearchHit();
      if (!match) {
        onRevealRequest();
        return;
      }
      await openRecord(match);
      revealInOutline();
    }
  }
</script>

<div class="relative min-w-0 flex-[0_1_460px]">
  <RecordIcon name="search" class="pointer-events-none absolute top-1/2 left-2 size-4 -translate-y-1/2 text-slate-600" />
  <Input
    bind:ref={input}
    type="search"
    placeholder="Find a record"
    aria-label="Filter the navigation"
    role="combobox"
    aria-expanded={book.query !== ""}
    aria-controls="book-nav"
    aria-autocomplete="list"
    autocomplete="off"
    class="pr-10 pl-8"
    value={book.query}
    onfocus={() => void book.ensureSearch()}
    oninput={(e) => changed((e.target as HTMLInputElement).value)}
    onkeydown={keyed}
  />
  <Kbd class="absolute top-1/2 right-2 -translate-y-1/2" aria-hidden="true">/</Kbd>
</div>
