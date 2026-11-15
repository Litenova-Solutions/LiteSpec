<script lang="ts">
  import { book } from "$lib/frame";
  import type { Snippet } from "svelte";

  // One fact tile: a label, a value, and an optional hint below it. A short
  // figure reads big; a sentence never does.
  type Props = {
    k: string;
    html?: string | null;
    hint?: string;
    big?: boolean;
    children?: Snippet;
  };
  let { k, html = null, hint = "", big = false, children }: Props = $props();
</script>

<div class="bg-background p-4">
  <div class="text-xs tracking-[0.32px] text-text-secondary">{book.fieldLabel(k)}</div>
  <div class="mt-1 min-w-0 [overflow-wrap:anywhere] {big ? 'text-[28px] leading-9 font-light tabular-nums' : 'text-sm'} [&_a]:text-link [&_a]:no-underline [&_a:hover]:underline">
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
  </div>
  {#if hint}<div class="mt-1 text-xs tracking-[0.32px] text-text-secondary">{@html hint}</div>{/if}
</div>
