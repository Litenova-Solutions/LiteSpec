<script lang="ts">
  import { onDestroy, untrack } from "svelte";
  import { createVirtualizer, type SvelteVirtualizer } from "@tanstack/svelte-virtual";
  import { book } from "$lib/frame";
  import type { View } from "$lib/frame";
  import FilterPanel from "./FilterPanel.svelte";
  import OutlineRow from "./OutlineRow.svelte";
  import SecurityView from "./SecurityView.svelte";
  import OutlineTools from "./OutlineTools.svelte";
  import ViewTabs from "./ViewTabs.svelte";
  import { outlineState } from "./outline-state.svelte";
  import { buildOutlineRows, ROW_HEIGHT, securityGroups, type OutlineRow as OutlineRowData } from "./outline-rows";

  // The book's left navigation: view tabs, tools and filters over the
  // virtualized outline, or the security groups. Every row keeps one fixed
  // height per kind, so the virtualizer never measures.
  const outlinesReady = $derived.by(() => (book.man?.layers ?? []).every((l) => book.isLayerLoaded(l.key)));
  const outlineLoading = $derived(book.anyFilter() && !outlinesReady);
  const rows = $derived.by((): OutlineRowData[] => {
    if (outlineLoading) return [];
    if (book.view === "outline") return buildOutlineRows(book);
    return [];
  });
  const models = $derived.by(() => ({
    security: !outlineLoading && book.view === "security" ? securityGroups(book) : [],
  }));
  // The best search hit while a query is active, for the outline's marker.
  const bestHit = $derived(book.query ? book.bestSearchHit() : null);

  // The outline publishes its rendered record order so keyboard moves follow
  // the displayed sort, grouping and filtering.
  $effect(() => {
    if (book.view !== "outline") return;
    const list = rows;
    book.setNavOrder(list.flatMap((row) => (row.kind === "record" ? [row.id] : [])));
  });

  let loadingOutlines = false;
  $effect(() => {
    if (book.anyFilter() && !outlinesReady && !loadingOutlines) {
      loadingOutlines = true;
      void book.ensureAllOutlines().finally(() => { loadingOutlines = false; });
    }
  });

  let loadingAreas = false;
  $effect(() => {
    // The product-area grouping loads its shard when the reader switches to
    // it, and again after a rebuild forgets it.
    void book.areasRev;
    if (book.intentGrouping === "product-area" && !book.productAreas && !loadingAreas) {
      loadingAreas = true;
      void book.ensureProductAreas().finally(() => { loadingAreas = false; });
    }
  });

  let scrollEl = $state<HTMLDivElement | null>(null);
  const virtStore = createVirtualizer<HTMLDivElement, HTMLElement>({
    count: 0,
    getScrollElement: () => scrollEl,
    estimateSize: () => 32,
    overscan: 8,
  });
  let virt: SvelteVirtualizer<HTMLDivElement, HTMLElement> | null = null;
  const unsub = virtStore.subscribe((v) => { virt = v; });
  onDestroy(unsub);

  $effect(() => {
    const list = rows;
    const el = scrollEl;
    virt?.setOptions({
      count: list.length,
      getScrollElement: () => el,
      estimateSize: (i) => ROW_HEIGHT[list[i]?.kind ?? "record"],
      getItemKey: (i) => list[i]?.key ?? i,
    });
  });

  const rkSel = (key: string) => `[data-rk="${key.replace(/\\/g, "\\\\").replace(/"/g, '\\"')}"]`;
  function rowEl(key: string) {
    return scrollEl?.querySelector<HTMLElement>(rkSel(key)) ?? null;
  }
  function insideViewport(el: HTMLElement) {
    if (!scrollEl) return false;
    const r = el.getBoundingClientRect();
    const c = scrollEl.getBoundingClientRect();
    return r.top >= c.top + 4 && r.bottom <= c.bottom - 4;
  }
  async function reveal(key: string, doFocus: boolean) {
    const el0 = rowEl(key);
    if (el0 && insideViewport(el0)) {
      if (doFocus) el0.focus({ preventScroll: true });
      return;
    }
    if (book.view === "outline" && virt) {
      const idx = untrack(() => rows).findIndex((r) => r.kind === "record" && r.id === key);
      if (idx < 0) return;
      virt.scrollToIndex(idx, { align: "center" });
      for (let t = 0; t < 8; t++) {
        await new Promise((r) => requestAnimationFrame(r));
        const el = rowEl(key);
        if (el && insideViewport(el)) {
          if (doFocus) el.focus({ preventScroll: true });
          return;
        }
      }
      if (doFocus) rowEl(key)?.focus({ preventScroll: true });
    } else {
      const el = rowEl(key);
      if (!el) return;
      el.scrollIntoView({ block: "center" });
      if (doFocus) el.focus({ preventScroll: true });
    }
  }

  $effect(() => {
    void book.view;
    if (scrollEl) scrollEl.scrollTop = 0;
  });
  let lastTick = 0;
  $effect(() => {
    const sel = book.selected;
    const tick = outlineState.revealRequest;
    void book.view;
    if (untrack(() => outlineLoading)) {
      lastTick = tick;
      return;
    }
    const doFocus = tick !== lastTick;
    lastTick = tick;
    if (!sel || !scrollEl) return;
    // A selection the reader made by clicking a row is already in view; only
    // outside selections scroll the outline, and only to a row outside it.
    const fromOutline = untrack(() => outlineState.fromOutline);
    if (!doFocus && fromOutline === sel) return;
    if (fromOutline !== null) untrack(() => { outlineState.fromOutline = null; });
    void reveal(sel, doFocus);
  });

  async function switchView(v: View) {
    if (v === book.view) return;
    await book.setView(v);
  }
</script>

<div class="flex h-full flex-col">
  <div class="border-b border-border-subtle p-4">
    <ViewTabs onViewChange={(v) => void switchView(v)} />
    <OutlineTools />
    <FilterPanel onRevealRequest={() => outlineState.requestReveal()} />
  </div>
  <div
    bind:this={scrollEl}
    role="tabpanel"
    id="viewpanel"
    aria-labelledby={book.view === "outline" ? "viewtab-outline" : "viewtab-security"}
    class="min-h-0 flex-1 overflow-y-auto pt-3 pb-6"
  >
    {#if outlineLoading}
      <p class="px-4 py-2 text-xs text-text-helper">Loading the outline.</p>
    {:else if book.view === "outline"}
      {@const items = $virtStore.getVirtualItems()}
      <div role="tree" aria-label="Outline" class="relative w-full" style="height: {$virtStore.getTotalSize()}px">
        {#each items as item (item.key)}
          {@const row = rows[item.index]}
          {#if row}
            <div
              data-index={item.index}
              style="position: absolute; top: 0; left: 0; width: 100%; height: {item.size}px; transform: translateY({item.start}px);"
            >
              <OutlineRow {row} bestHit={bestHit} />
            </div>
          {/if}
        {/each}
      </div>
    {:else}
      <SecurityView sgroups={models.security} />
    {/if}
  </div>
</div>
