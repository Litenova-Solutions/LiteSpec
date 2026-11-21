<script lang="ts">
  import { book, cn, formatCount } from "$lib/frame";
  import type { View } from "$lib/frame";
  import * as Tooltip from "$lib/ui/tooltip";
  import { recordCount, totalRecords } from "./outline-rows";

  // The view switch: the Outline and the Security view. Each tab keeps its
  // count in a tooltip and an accessible name; the tabs show none. Arrow keys
  // move between tabs and select as they move.
  type Props = { onViewChange: (v: View) => void };
  let { onViewChange }: Props = $props();
  const tabs = $derived.by(() => {
    void book.rev;
    const security = book.securityItems().length;
    return [
      { key: "outline" as const, label: "Outline", tip: "Outline, " + recordCount(totalRecords(book)) },
      { key: "security" as const, label: "Security", tip: "Security, " + formatCount(security) + " " + (security === 1 ? "security item" : "security items") },
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

<div role="tablist" aria-label="Views" tabindex="-1" onkeydown={key} class="grid h-8 grid-cols-2 divide-x divide-border-strong overflow-hidden rounded-[4px] border border-border-strong">
  {#each tabs as t (t.key)}
    <Tooltip.Root>
      <Tooltip.Trigger>
        {#snippet child({ props })}
          <button
            {...props}
            type="button"
            role="tab"
            id={"viewtab-" + t.key}
            aria-selected={book.view === t.key}
            aria-label={t.tip}
            aria-controls="viewpanel"
            tabindex={book.view === t.key ? 0 : -1}
            onclick={() => onViewChange(t.key)}
            class={cn(
              "inline-flex min-w-0 cursor-pointer items-center justify-center gap-2 px-2 text-sm",
              book.view === t.key ? "bg-foreground text-background" : "text-text-secondary hover:bg-layer-hover hover:text-foreground",
            )}
          >
            {t.label}
          </button>
        {/snippet}
      </Tooltip.Trigger>
      <Tooltip.Content side="bottom">{t.tip}</Tooltip.Content>
    </Tooltip.Root>
  {/each}
</div>
