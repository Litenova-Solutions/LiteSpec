<script lang="ts">
  // The home floorplan of the __PROJECT__ shared UI package: the header,
  // the waiting work list, and the metrics row. The route fills the work
  // slot with what waits and the metrics slot with the period summary.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    work?: Snippet;
    metrics?: Snippet;
    children?: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    work,
    metrics,
    children,
    class: className = "",
  }: Props = $props();
</script>

<div class={cn("grid gap-6", className)}>
  <header class="grid gap-4">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h1 class="text-2xl font-medium">{title}</h1>
      {#if actions}
        {@render actions()}
      {/if}
    </div>
    {#if description !== ""}
      <p class="text-sm text-muted-foreground">{description}</p>
    {/if}
  </header>
  {#if children}
    {@render children()}
  {/if}
  {#if work}
    {@render work()}
  {/if}
  {#if metrics}
    {@render metrics()}
  {/if}
</div>
