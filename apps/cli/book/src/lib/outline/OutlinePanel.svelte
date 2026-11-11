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
  import { buildListRows, buildOutlineRows, ROW_HEIGHT, securityGroups, type OutlineRow as OutlineRowData } from "./outline-rows";

  // The book's left navigation: view tabs, tools and filters over the
  // virtualized outline or record list, or the security groups.
  const outlinesReady = $derived.by(() => (book.man?.layers ?? []).every((l) => book.isLayerLoaded(l.key)));
  const outlineLoading = $derived(book.anyFilter() && !outlinesReady);
  const rows = $derived.by((): OutlineRowData[] => {
    if (outlineLoading) return [];
    if (book.view === "outline") return buildOutlineRows(book);
    if (book.view === "list") return buildListRows(book);
    return [];
  });
  const models = $derived.by(() => ({
    security: !outlineLoading && book.view === "security" ? securityGroups(book) : [],
  }));
  const navKeys = $derived.by((): string[] => {
    if (book.view === "outline" || book.view === "list")
      return rows.flatMap((r) => (r.kind === "record" || r.kind === "listrecord" ? [r.id] : []));
    return models.security.flatMap((g) => g.items.map((i) => i.id));
  });

  let loadingOutlines = false;
  $effect(() => {
    if (book.anyFilter() && !outlinesReady && !loadingOutlines) {
      loadingOutlines = true;
      void book.ensureAllOutlines().finally(() => { loadingOutlines = false; });
    }
  });

  $effect(() => {
    book.navIds = navKeys;
  });

  let scrollEl = $state<HTMLDivElement | null>(null);
  const virtStore = createVirtualizer<HTMLDivElement, HTMLElement>({
    count: 0,
    getScrollElement: () => scrollEl,
    estimateSize: () => 28,
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

  // Layer descriptions size to their text; every other row keeps its height.
  function measure(node: HTMLElement, enabled: boolean) {
    let on = false;
    const apply = (v: boolean) => {
      if (!virt) return;
      if (v && !on) {
        virt.measureElement(node);
        on = true;
      } else if (!v && on) {
        virt.measureElement(null);
        on = false;
      }
    };
    apply(enabled);
    return { update: apply, destroy: () => { if (on) virt?.measureElement(null); } };
  }

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
    if ((book.view === "outline" || book.view === "list") && virt) {
      const idx = untrack(() => rows).findIndex((r) => (r.kind === "record" || r.kind === "listrecord") && r.id === key);
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

  let revealTick = $state(0);
  $effect(() => {
    void book.view;
    if (scrollEl) scrollEl.scrollTop = 0;
  });
  let lastTick = 0;
  $effect(() => {
    const sel = book.selected;
    const tick = book.focusNavTick;
    void revealTick;
    void book.view;
    if (untrack(() => outlineLoading)) {
      lastTick = tick;
      return;
    }
    const doFocus = tick !== lastTick;
    lastTick = tick;
    if (!sel || !scrollEl) return;
    void reveal(sel, doFocus);
  });

  async function switchView(v: View) {
    if (v !== "outline" && v !== "security") return;
    if (v === book.view) return;
    await book.setView(v);
    if (v === "security") {
      const first = navKeys[0];
      if (first && first !== book.selected) void book.select(first);
    }
  }
</script>

<div class="flex h-full flex-col">
  <div class="border-b px-3 pt-3 pb-2">
    <ViewTabs onViewChange={(v) => void switchView(v)} />
    <OutlineTools onRevealRequest={() => revealTick++} />
    <FilterPanel />
  </div>
  <div bind:this={scrollEl} class="min-h-0 flex-1 overflow-y-auto px-2.5 py-1.5">
    {#if outlineLoading}
      <p class="mx-2 my-2 text-xs text-muted-foreground">Loading the outline.</p>
    {:else if book.view === "outline" || book.view === "list"}
      {@const items = $virtStore.getVirtualItems()}
      <div role="tree" aria-label={book.view === "list" ? "Records" : "Outline"} class="relative w-full" style="height: {$virtStore.getTotalSize()}px">
        {#each items as item (item.key)}
          {@const row = rows[item.index]}
          {#if row}
            <div
              data-index={item.index}
              use:measure={row.kind === "desc"}
              style="position: absolute; top: 0; left: 0; width: 100%; height: {item.size}px; transform: translateY({item.start}px);"
            >
              <OutlineRow {row} />
            </div>
          {/if}
        {/each}
      </div>
    {:else}
      <SecurityView sgroups={models.security} />
    {/if}
  </div>
</div>
