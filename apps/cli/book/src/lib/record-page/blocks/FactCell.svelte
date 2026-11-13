<script lang="ts">
  import { book } from "$lib/frame";
  import type { Snippet } from "svelte";

  // One fact in the grid: a label, a value, and an optional hint below it.
  type Props = {
    k: string;
    html?: string | null;
    hint?: string;
    big?: boolean;
    children?: Snippet;
  };
  let { k, html = null, hint = "", big = false, children }: Props = $props();
</script>

<div class="contents">
  <div class="pt-2 text-sm text-muted-foreground sm:border-b sm:border-border sm:pb-2">{book.fieldLabel(k)}</div>
  <div class="border-b border-border pt-0.5 pb-2 text-[0.90625rem] sm:pt-2 leading-snug {big ? 'text-base font-semibold tabular-nums' : ''}">
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
    {#if hint}<div class="mt-1 text-xs text-muted-foreground">{@html hint}</div>{/if}
  </div>
</div>
