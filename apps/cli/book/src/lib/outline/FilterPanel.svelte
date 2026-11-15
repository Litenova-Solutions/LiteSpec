<script lang="ts">
  import { book, cn, formatCount, stripStep } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { outlineState } from "./outline-state.svelte";
  import { cachedSecurityItems, totalRecords } from "./outline-rows";

  // The filter panel, the active filter tags and the count line with the
  // expand, collapse and reveal buttons beneath the navigation tools. One pass
  // over the records caches each record's facet values, and every facet count
  // reads the cache instead of walking the store again.
  type Props = { onRevealRequest: () => void };
  let { onRevealRequest }: Props = $props();

  interface FacetVal { value: string; name: string; count: number; on: boolean }
  interface FacetModel { key: string; label: string; vals: FacetVal[] }
  interface FilterModel { facets: FacetModel[]; shown: number; total: number }
  interface CachedRow { real: boolean; values: Record<string, string[]>; queryOk: boolean }

  const model = $derived.by((): FilterModel => {
    const facetsOpen = outlineState.facetsOpen;
    const view = book.view;
    void book.rev;
    const total = totalRecords(book);
    const active = book.active;
    const query = book.query;
    const hits = book.hits;
    const rows: CachedRow[] = [];
    for (const id of book.orderWithProduct()) {
      const values: Record<string, string[]> = {};
      for (const f of book.facets) values[f.key] = f.get(id);
      let queryOk = true;
      if (query) {
        const hit = hits?.has(stripStep(id));
        queryOk = hit ?? book.title(id).toLowerCase().includes(query.toLowerCase());
      }
      rows.push({ real: book.isReal(id), values, queryOk });
    }
    // Whether a cached row passes the active facets, skipping one facet for
    // its own counts, and the search query. This matches the store's filter
    // reading facet for facet.
    const passes = (row: CachedRow, skip?: string): boolean => {
      for (const f of book.facets) {
        if (f.key === skip) continue;
        const selected = active[f.key];
        if (!selected?.length) continue;
        const values = row.values[f.key] ?? [];
        if (values.length && !selected.some((v) => values.includes(v))) return false;
      }
      return row.queryOk;
    };
    const out: FacetModel[] = [];
    if (facetsOpen) {
      for (const f of book.facets) {
        if (f.key === "layer" && view === "outline") continue;
        const counts: Record<string, number> = {};
        for (const row of rows) {
          if (!passes(row, f.key)) continue;
          for (const v of row.values[f.key] ?? []) counts[v] = (counts[v] ?? 0) + 1;
        }
        const sel = active[f.key] ?? [];
        const vals = Object.keys(counts);
        for (const v of sel) if (!vals.includes(v)) vals.push(v);
        if (!vals.length) continue;
        if (f.key === "type") {
          const order = Object.keys(book.lex.types ?? {});
          vals.sort((a, b) => order.indexOf(a) - order.indexOf(b));
        } else {
          vals.sort((a, b) => (counts[b] ?? 0) - (counts[a] ?? 0));
        }
        out.push({ key: f.key, label: f.label, vals: vals.map((v) => ({ value: v, name: f.name(v), count: counts[v] ?? 0, on: sel.includes(v) })) });
      }
    }
    const shown = rows.filter((row) => row.real && passes(row)).length;
    return { facets: out, shown, total };
  });
  const chips = $derived.by(() => {
    void book.rev;
    const out: { key: string; value: string; label: string; name: string }[] = [];
    for (const f of book.facets) for (const v of book.active[f.key] ?? []) out.push({ key: f.key, value: v, label: f.label, name: f.name(v) });
    return out;
  });
  const security = $derived.by(() => {
    if (book.view !== "security") return { shown: 0, total: 0, loaded: true };
    void book.rev;
    const items = cachedSecurityItems(book);
    return {
      shown: items.filter((x) => book.passesFilters(x.id)).length,
      total: items.length,
      loaded: book.security !== null,
    };
  });
  const nActive = $derived(chips.length + (book.query ? 1 : 0));

  function expandAndReveal(): void {
    book.expandAll();
    onRevealRequest();
  }

  function revealSelected(): void {
    if (book.selected) outlineState.expandChain(book.selected);
    onRevealRequest();
  }
</script>

{#if outlineState.facetsOpen}
  <div id="facetPanel" role="group" aria-label="Filters" class="mt-2 grid max-h-[52vh] gap-4 overflow-auto">
    <div class="flex items-center justify-between">
      <span class="font-semibold text-foreground">Filters</span>
      <span class="text-sm text-text-secondary">{nActive} active</span>
    </div>
    {#each model.facets as f (f.key)}
      <div class="grid gap-2">
        <h4 class="text-xs text-text-secondary">{f.label}</h4>
        <div class="grid gap-2">
          {#each f.vals as v (v.value)}
            {@const off = !v.count && !v.on}
            <label class={cn("flex min-h-6 cursor-pointer items-center gap-3 text-sm text-foreground", off && "cursor-default opacity-40")}>
              <input
                type="checkbox"
                class="peer sr-only"
                checked={v.on}
                disabled={off}
                onchange={() => book.toggleFacet(f.key, v.value)}
              />
              <span
                aria-hidden="true"
                class={cn(
                  "grid size-4 flex-none place-items-center border border-foreground peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-focus",
                  v.on && "bg-foreground text-background",
                )}
              >
                {#if v.on}
                  <svg viewBox="0 0 16 16" class="size-3" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <path d="M3.5 8.5l3.5 3.5 6-8" stroke-linecap="round" stroke-linejoin="round" />
                  </svg>
                {/if}
              </span>
              <span class="min-w-0 flex-1 truncate">{v.name}</span>
              <span class="flex-none text-xs tracking-[0.32px] text-text-helper tabular-nums">{v.count}</span>
            </label>
          {/each}
        </div>
      </div>
    {/each}
  </div>
{/if}
{#if chips.length || book.query}
  <div class="mt-2 flex flex-wrap items-center gap-1">
    {#each chips as c (c.key + "|" + c.value)}
      <span class="inline-flex h-6 items-center gap-1 rounded-full bg-tag-blue py-0 pr-0 pl-3 text-xs tracking-[0.32px] text-tag-blue-foreground">
        {c.label}: {c.name}
        <button type="button" aria-label="Remove {c.name}" onclick={() => book.toggleFacet(c.key, c.value)} class="grid size-6 flex-none cursor-pointer place-items-center rounded-full hover:bg-hover-wash">
          <RecordIcon name="x" class="size-3" />
        </button>
      </span>
    {/each}
    {#if book.query}
      <span class="inline-flex h-6 items-center gap-1 rounded-full bg-tag-blue py-0 pr-0 pl-3 text-xs tracking-[0.32px] text-tag-blue-foreground">
        Text: {book.query}
        <button type="button" aria-label="Clear the text filter" onclick={() => book.setQuery("")} class="grid size-6 flex-none cursor-pointer place-items-center rounded-full hover:bg-hover-wash">
          <RecordIcon name="x" class="size-3" />
        </button>
      </span>
    {/if}
    <button type="button" onclick={() => book.clearFilters()} class="inline-flex h-6 cursor-pointer items-center px-2 text-sm text-link hover:underline">Clear all</button>
  </div>
{/if}
<div class="mt-2 flex items-center gap-2">
  <p class="min-w-0 flex-1 text-xs tracking-[0.32px] text-text-helper">
    {#if book.view === "security"}
      {#if !security.loaded}
        Loading the security items.
      {:else if book.anyFilter()}
        <b class="font-semibold text-foreground tabular-nums">{formatCount(security.shown)}</b> of {formatCount(security.total)} security items match
      {:else}
        <b class="font-semibold text-foreground tabular-nums">{formatCount(security.total)}</b> security items
      {/if}
    {:else if book.anyFilter()}
      <b class="font-semibold text-foreground tabular-nums">{formatCount(model.shown)}</b> of {formatCount(model.total)} records match
    {:else}
      <b class="font-semibold text-foreground tabular-nums">{formatCount(model.total)}</b> records in this book.
    {/if}
  </p>
  {#if book.view === "outline"}
    <button type="button" aria-label="Expand all" onclick={expandAndReveal} class="grid size-8 flex-none cursor-pointer place-items-center text-foreground hover:bg-hover-wash">
      <RecordIcon name="expand" class="size-4" />
    </button>
    <button type="button" aria-label="Collapse all" onclick={() => book.collapseAll()} class="grid size-8 flex-none cursor-pointer place-items-center text-foreground hover:bg-hover-wash">
      <RecordIcon name="collapse" class="size-4" />
    </button>
  {/if}
  <button type="button" aria-label="Show the selected record" onclick={revealSelected} class="grid size-8 flex-none cursor-pointer place-items-center text-foreground hover:bg-hover-wash">
    <RecordIcon name="locate" class="size-4" />
  </button>
</div>
