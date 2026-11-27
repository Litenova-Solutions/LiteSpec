<script lang="ts">
  // The settings floorplan of the __PROJECT__ shared UI package: the
  // section header, the sub navigation, and the panel. The route fills the
  // navigation slot with the links of one domain area and the panel slot
  // with its form.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    navigation?: Snippet;
    children: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    navigation,
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
  <div class="grid gap-6 md:grid-cols-4">
    {#if navigation}
      <nav aria-label="Settings sections" class="grid gap-4 md:col-span-1">
        {@render navigation()}
      </nav>
    {/if}
    <div class="grid gap-6 md:col-span-3">
      {@render children()}
    </div>
  </div>
</div>
