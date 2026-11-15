<script lang="ts">
  import { book } from "$lib/frame";

  // The "On this record" section nav, shown only when the page has enough sections.
  // The pills are buttons that scroll within the record; they never write the
  // hash, so the open record stays put.
  type Props = { items: { key: string; label: string; count: number | boolean }[] };
  let { items }: Props = $props();
  const shown = $derived((items ?? []).filter((x) => x.count));

  function jump(key: string): void {
    const section = document.getElementById("b-" + key);
    section?.scrollIntoView();
    section?.querySelector("h2")?.focus({ preventScroll: true });
  }
</script>

{#if shown.length > 2}
  <nav class="mb-6 flex flex-wrap gap-2" aria-label="On this record">
    {#each shown as x (x.key)}
      <button
        type="button"
        onclick={() => jump(x.key)}
        class="inline-flex h-7 items-center gap-2 rounded-md border border-slate-300 bg-white px-2.5 text-xs text-slate-600 hover:bg-slate-100 hover:text-slate-900"
      >
        {book.fieldLabel(x.label)}
        {#if typeof x.count === "number"}<span class="tabular-nums">{x.count}</span>{/if}
      </button>
    {/each}
  </nav>
{/if}
