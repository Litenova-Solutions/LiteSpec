<script lang="ts">
  import { book, hoverSection } from "$lib/frame";
  import { Hover } from "$lib/record";
  import type { Snippet } from "svelte";

  // One row of a composed definition list: a labeled value, as HTML or live content.
  type Props = {
    label: string;
    html?: string | null;
    children?: Snippet;
  };
  let { label, html = null, children }: Props = $props();
</script>

<dt class="border-b border-border-subtle py-3 pr-2 font-semibold"><Hover hover={hoverSection(book, label, "")}>{book.fieldLabel(label)}</Hover></dt>
<dd class="m-0 min-w-0 border-b border-border-subtle py-3">
  {#if html !== null}
    {@html html}
  {:else}
    {@render children?.()}
  {/if}
</dd>
