<script lang="ts">
  import { book } from "$lib/book";
  import type { Snippet } from "svelte";

  // One row of a composed definition list: a labeled value, as HTML or live content.
  let {
    label,
    html = null,
    children,
  }: {
    label: string;
    html?: string | null;
    children?: Snippet;
  } = $props();
</script>

<dt class="text-muted-foreground">{book.fieldLabel(label)}</dt>
<dd class="m-0 min-w-0">
  {#if html !== null}
    {@html html}
  {:else}
    {@render children?.()}
  {/if}
</dd>
