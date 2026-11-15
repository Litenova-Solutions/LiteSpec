<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import type { View } from "$lib/frame";
  import { totalRecords } from "./outline-rows";

  // The view tabs: the Outline and the Security view, each with its count.
  // The badge counts the records in the book, the same total the shown line
  // reads. Arrow keys move between tabs and select as they move.
  type Props = { onViewChange: (v: View) => void };
  let { onViewChange }: Props = $props();
  const tabs = $derived.by(() => {
    void book.rev;
    return [
      { key: "outline" as const, label: "Outline", count: totalRecords(book) },
      { key: "security" as const, label: "Security", count: book.securityItems().length },
    ];
  });

  function key(e: KeyboardEvent): void {
    if (e.key !== "ArrowLeft" && e.key !== "ArrowRight" && e.key !== "Home" && e.key !== "End") return;
    e.preventDefault();
    const order: View[] = ["outline", "security"];
    const at = order.indexOf(book.view);
    const next = e.key === "ArrowRight"
      ? (at + 1) % order.length
      : e.key === "ArrowLeft"
        ? (at - 1 + order.length) % order.length
        : e.key === "Home" ? 0 : order.length - 1;
    const view = order[next] ?? "outline";
    onViewChange(view);
    if (e.currentTarget instanceof HTMLElement) e.currentTarget.querySelector<HTMLElement>("#viewtab-" + view)?.focus();
  }
</script>

<div role="tablist" aria-label="Views" tabindex="-1" onkeydown={key} class="flex items-center gap-1 rounded-md bg-slate-100 p-1">
  {#each tabs as t (t.key)}
    <button
      type="button"
      role="tab"
      id={"viewtab-" + t.key}
      aria-selected={book.view === t.key}
      aria-controls="viewpanel"
      tabindex={book.view === t.key ? 0 : -1}
      onclick={() => onViewChange(t.key)}
      class="inline-flex flex-1 items-center justify-center gap-2 rounded-sm px-2 py-1 text-xs font-medium {book.view === t.key
        ? 'bg-white text-slate-900 shadow-sm'
        : 'text-slate-600 hover:text-slate-900'}"
    >
      {t.label}
      <span class="text-xs tabular-nums opacity-70">{formatCount(t.count)}</span>
    </button>
  {/each}
</div>
