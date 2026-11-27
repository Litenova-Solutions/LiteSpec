<script lang="ts">
  // The record floorplan of the __PROJECT__ shared UI package: the header,
  // the optional tabs, the main facts and actions, and the optional aside.
  // The route fills the main slot with the property list of one record.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    tabs?: Snippet;
    aside?: Snippet;
    children: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    tabs,
    aside,
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
  {#if tabs}
    {@render tabs()}
  {/if}
  <div class="grid gap-6 lg:grid-cols-3">
    <div class="grid gap-6 lg:col-span-2">
      {@render children()}
    </div>
    {#if aside}
      {@render aside()}
    {/if}
  </div>
</div>
