<script lang="ts">
  import { book } from "$lib/frame";
  import type { Snippet } from "svelte";

  // One fact in the grid: a label, a value, and an optional hint below it.
  let {
    k,
    html = null,
    hint = "",
    big = false,
    children,
  }: {
    k: string;
    html?: string | null;
    hint?: string;
    big?: boolean;
    children?: Snippet;
  } = $props();
</script>

<div class="contents">
  <div class="border-b border-border py-2 text-sm text-muted-foreground">{book.fieldLabel(k)}</div>
  <div class="border-b border-border py-2 text-[14.5px] leading-snug {big ? 'text-base font-semibold tabular-nums' : ''}">
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
    {#if hint}<div class="mt-1 text-xs text-muted-foreground">{@html hint}</div>{/if}
  </div>
</div>
