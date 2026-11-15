<script lang="ts">
  import { book, cn, formatCount, stripStep } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { outlineState } from "./outline-state.svelte";
  import { cachedSecurityItems, totalRecords } from "./outline-rows";

  // The filter panel, the active filter chips and the shown line beneath the
  // navigation tools. One pass over the records caches each record's facet
  // values, and every facet count reads the cache instead of walking the
  // store again.
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
    for (const id of book.order) {
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
</script>

{#if outlineState.facetsOpen}
  <div id="facetPanel" role="group" aria-label="Filters" class="mt-2 max-h-[52vh] overflow-auto rounded-lg border border-slate-300 bg-white p-3 shadow-sm">
    {#each model.facets as f (f.key)}
      <div class="mb-3 last:mb-0">
        <h4 class="mb-1.5 text-xs font-medium tracking-wide text-slate-600 uppercase">{f.label}</h4>
        <div class="flex flex-wrap gap-1">
          {#each f.vals as v (v.value)}
            <button
              type="button"
              aria-pressed={v.on}
              disabled={!v.count && !v.on}
              onclick={() => book.toggleFacet(f.key, v.value)}
              class={cn(
                "inline-flex h-7 cursor-pointer items-center gap-2 rounded-md border px-2.5 text-xs disabled:cursor-default disabled:opacity-40",
                v.on ? "border-blue-700 bg-blue-700 text-white" : "border-slate-300 bg-white text-slate-900 hover:bg-slate-100",
              )}
            >
              {v.name}
              <span class={cn("text-xs tabular-nums", v.on ? "text-white opacity-75" : "text-slate-600")}>{v.count}</span>
            </button>
          {/each}
        </div>
      </div>
    {/each}
  </div>
{/if}
{#if chips.length || book.query}
  <div class="mt-2 flex flex-wrap gap-1">
    {#each chips as c (c.key + "|" + c.value)}
      <span class="inline-flex h-6 items-center gap-1 rounded-md border border-blue-200 bg-blue-50 pr-1 pl-2.5 text-xs text-slate-900">
        {c.label}: {c.name}
        <button type="button" aria-label="Remove {c.name}" onclick={() => book.toggleFacet(c.key, c.value)} class="grid size-6 cursor-pointer place-items-center rounded-md text-slate-600 hover:bg-blue-100 hover:text-slate-900">
          <RecordIcon name="x" class="size-3.5" />
        </button>
      </span>
    {/each}
    {#if book.query}
      <span class="inline-flex h-6 items-center gap-1 rounded-md border border-blue-200 bg-blue-50 pr-1 pl-2.5 text-xs text-slate-900">
        Text: {book.query}
        <button type="button" aria-label="Clear the text filter" onclick={() => book.setQuery("")} class="grid size-6 cursor-pointer place-items-center rounded-md text-slate-600 hover:bg-blue-100 hover:text-slate-900">
          <RecordIcon name="x" class="size-3.5" />
        </button>
      </span>
    {/if}
    <button type="button" onclick={() => book.clearFilters()} class="inline-flex h-6 cursor-pointer items-center px-1.5 text-xs text-blue-700">Clear all</button>
  </div>
{/if}
<div class="mt-2 text-xs text-slate-600">
  {#if book.view === "security"}
    {#if !security.loaded}
      Loading the security items.
    {:else if book.anyFilter()}
      <b class="font-medium text-slate-900 tabular-nums">{formatCount(security.shown)}</b> of {formatCount(security.total)} security items match
    {:else}
      <b class="font-medium text-slate-900 tabular-nums">{formatCount(security.total)}</b> security items
    {/if}
  {:else if book.anyFilter()}
    <b class="font-medium text-slate-900 tabular-nums">{formatCount(model.shown)}</b> of {formatCount(model.total)} records match
  {:else}
    <b class="font-medium text-slate-900 tabular-nums">{formatCount(model.total)}</b> records in this book.
  {/if}
</div>
