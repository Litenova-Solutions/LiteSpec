<script lang="ts">
  // The work floorplan of the __PROJECT__ shared UI package: the header,
  // the context pane, and the decision pane. The route fills the context
  // slot with the facts the decision needs and the main slot with the
  // decision.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    context?: Snippet;
    children: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    context,
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
  <div class="grid gap-6 lg:grid-cols-2">
    {#if context}
      {@render context()}
    {/if}
    {@render children()}
  </div>
</div>
