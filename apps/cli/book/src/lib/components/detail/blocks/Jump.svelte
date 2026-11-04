<script lang="ts">
  import { book } from "$lib/book";

  // The "On this record" anchor nav, shown only when the page has enough sections.
  let { items }: { items: { key: string; label: string; count: number | boolean }[] } = $props();
  const shown = $derived((items ?? []).filter((x) => x.count));
</script>

{#if shown.length > 2}
  <nav class="mt-[-6px] mb-[26px] flex flex-wrap gap-1.5" aria-label="On this record">
    {#each shown as x (x.key)}
      <a
        href={"#b-" + x.key}
        class="inline-flex h-7 items-center gap-1.5 rounded-full bg-muted px-2.5 text-xs text-subtle-foreground hover:bg-border hover:text-foreground"
      >
        {book.fieldLabel(x.label)}
        {#if typeof x.count === "number"}<span class="font-mono text-[11px] text-muted-foreground">{x.count}</span>{/if}
      </a>
    {/each}
  </nav>
{/if}
