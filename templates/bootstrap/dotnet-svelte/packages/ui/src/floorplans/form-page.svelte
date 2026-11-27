<script lang="ts">
  // The form floorplan of the __PROJECT__ shared UI package: the header,
  // the sections, and the footer action bar. The route fills the sections
  // slot with field groups and the footer slot with one primary action.
  import type { Snippet } from "svelte";
  import { cn } from "../utils/cn.ts";

  type Props = {
    title: string;
    description?: string;
    actions?: Snippet;
    footer?: Snippet;
    children: Snippet;
    class?: string;
  };

  let {
    title,
    description = "",
    actions,
    footer,
    children,
    class: className = "",
  }: Props = $props();
</script>

<div class={cn("mx-auto grid w-full max-w-2xl gap-6", className)}>
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
  {@render children()}
  {#if footer}
    {@render footer()}
  {/if}
</div>
