<script lang="ts">
  // The list floorplan of the __PROJECT__ shared UI package: the header,
  // the filter bar, the table or empty state, and the pagination. The route
  // fills the table slot with the data table of one record kind.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    filters?: Snippet;
    pagination?: Snippet;
    children: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    filters,
    pagination,
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
  {#if filters}
    {@render filters()}
  {/if}
  {@render children()}
  {#if pagination}
    {@render pagination()}
  {/if}
</div>
