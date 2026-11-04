<script lang="ts">
  import { book, n0 } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { cn } from "$lib/utils";
  import { securityItems } from "./rows";

  // The filter panel, the active filter chips and the shown line with the
  // row-mark legend beneath the navigation tools.
  interface FacetVal { value: string; name: string; count: number; on: boolean }
  interface FacetModel { key: string; label: string; vals: FacetVal[] }
  const facets = $derived.by((): FacetModel[] => {
    if (!book.facetsOpen) return [];
    void book.rev;
    const out: FacetModel[] = [];
    for (const f of book.facets) {
      if (f.key === "layer" && book.view === "outline") continue;
      const counts: Record<string, number> = {};
      for (const id of book.order) {
        if (!book.passes(id, f.key)) continue;
        for (const v of f.get(id)) counts[v] = (counts[v] ?? 0) + 1;
      }
      const sel = book.active[f.key] ?? [];
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
    return out;
  });
  const chips = $derived.by(() => {
    void book.rev;
    const out: { key: string; value: string; label: string; name: string }[] = [];
    for (const f of book.facets) for (const v of book.active[f.key] ?? []) out.push({ key: f.key, value: v, label: f.label, name: f.name(v) });
    return out;
  });
  const securityCount = $derived.by(() => {
    if (book.view !== "security") return 0;
    void book.rev;
    return securityItems(book).filter((x) => book.passes(x.id)).length;
  });
  const outlineCounts = $derived.by(() => {
    void book.rev;
    const n = book.order.filter((id) => book.isReal(id) && book.passes(id)).length;
    let total = Object.values(book.man?.totals ?? {}).reduce((a, b) => a + b, 0);
    if (!total) total = book.order.filter((id) => book.isReal(id)).length;
    return { n, total };
  });
</script>

{#if book.facetsOpen}
  <div id="facetPanel" role="group" aria-label="Filters" class="mt-2 max-h-[52vh] overflow-auto rounded-md border bg-background p-2.5">
    {#each facets as f (f.key)}
      <div class="mb-2.5 last:mb-0">
        <h4 class="mb-1.5 text-[11px] font-medium tracking-[0.08em] text-muted-foreground uppercase">{f.label}</h4>
        <div class="flex flex-wrap gap-1">
          {#each f.vals as v (v.value)}
            <button
              type="button"
              aria-pressed={v.on}
              disabled={!v.count && !v.on}
              onclick={() => book.toggleFacet(f.key, v.value)}
              class={cn(
                "inline-flex h-[26px] cursor-pointer items-center gap-1.5 rounded-full border px-2.5 text-xs disabled:cursor-default disabled:opacity-40",
                v.on ? "border-primary bg-primary text-primary-foreground" : "border-border bg-background text-subtle-foreground hover:bg-muted",
              )}
            >
              {v.name}
              <span class={cn("font-mono text-[10.5px]", v.on ? "text-primary-foreground opacity-75" : "text-muted-foreground")}>{v.count}</span>
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
      <span class="inline-flex h-6 items-center gap-1 rounded-full border border-rule-border bg-rule pr-1 pl-2.5 text-xs text-foreground">
        {c.label}: {c.name}
        <button type="button" aria-label="Remove {c.name}" onclick={() => book.toggleFacet(c.key, c.value)} class="grid size-[18px] cursor-pointer place-items-center rounded-full text-muted-foreground hover:bg-highlight-soft hover:text-foreground">
          <Icon name="x" class="size-3.5" />
        </button>
      </span>
    {/each}
    {#if book.query}
      <span class="inline-flex h-6 items-center gap-1 rounded-full border border-rule-border bg-rule pr-1 pl-2.5 text-xs text-foreground">
        Text: {book.query}
        <button type="button" aria-label="Clear the text filter" onclick={() => book.setQuery("")} class="grid size-[18px] cursor-pointer place-items-center rounded-full text-muted-foreground hover:bg-highlight-soft hover:text-foreground">
          <Icon name="x" class="size-3.5" />
        </button>
      </span>
    {/if}
    <button type="button" onclick={() => book.clearFilters()} class="inline-flex h-6 cursor-pointer items-center px-1.5 text-xs text-highlight-foreground">Clear all</button>
  </div>
{/if}
<div class="mt-2 text-xs text-muted-foreground">
  {#if book.view === "security"}
    <b class="font-medium text-foreground tabular-nums">{securityCount}</b> security items
  {:else if book.anyFilter()}
    <b class="font-medium text-foreground tabular-nums">{n0(outlineCounts.n)}</b> of {n0(outlineCounts.total)} records match
  {:else}
    <b class="font-medium text-foreground tabular-nums">{n0(outlineCounts.total)}</b> records in this book.
  {/if}
</div>
{#if book.view === "outline"}
  <div class="mt-2 flex flex-wrap gap-x-3 gap-y-1 border-t pt-3 text-xs text-muted-foreground">
    <span class="inline-flex items-center gap-[5px]"><span class="grid text-warn-foreground"><Icon name="missing" class="size-3.5" /></span>Not implemented or not tested</span>
    <span class="inline-flex items-center gap-[5px]"><span class="grid text-warn-foreground"><Icon name="flask" class="size-3.5" /></span>A test fails</span>
  </div>
{/if}
