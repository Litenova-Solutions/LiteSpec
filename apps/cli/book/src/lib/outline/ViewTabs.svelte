<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import type { View } from "$lib/frame";

  // The view tabs: the Outline and the Security view, each with its count.
  let { onViewChange }: { onViewChange: (v: View) => void } = $props();
  const tabs = $derived.by(() => {
    void book.rev;
    return [
      { key: "outline" as const, label: "Outline", count: book.order.filter((id) => book.isReal(id)).length },
      { key: "security" as const, label: "Security", count: book.securityItems().length },
    ];
  });
</script>

<div role="tablist" aria-label="Views" class="flex items-center gap-1">
  {#each tabs as t (t.key)}
    <button
      type="button"
      role="tab"
      aria-selected={book.view === t.key}
      onclick={() => onViewChange(t.key)}
      class="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium {book.view === t.key
        ? 'bg-accent text-accent-foreground'
        : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground'}"
    >
      {t.label}
      <span class="font-mono text-[11px] tabular-nums opacity-70">{formatCount(t.count)}</span>
    </button>
  {/each}
</div>
