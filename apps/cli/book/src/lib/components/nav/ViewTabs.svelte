<script lang="ts">
  import { book, n0, type View } from "$lib/book";

  // The view tabs: the Outline and the Security view, each with its count.
  let { onSwitch }: { onSwitch: (v: View) => void } = $props();
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
      onclick={() => onSwitch(t.key)}
      class="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium {book.view === t.key
        ? 'bg-accent text-accent-foreground'
        : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground'}"
    >
      {t.label}
      <span class="font-mono text-[11px] tabular-nums opacity-70">{n0(t.count)}</span>
    </button>
  {/each}
</div>
