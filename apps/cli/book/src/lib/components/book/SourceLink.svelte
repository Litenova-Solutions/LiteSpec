<script lang="ts">
  import { book, type SourceRef } from "$lib/book";
  import { srcHref } from "$lib/book/html";
  import Icon from "./Icon.svelte";

  // Every entry links to the file and line it comes from, opened in the editor.
  let { src }: { src: SourceRef | null | undefined } = $props();
  const href = $derived(srcHref(book, src));
  const text = $derived(src ? (src.litespec ? "LiteSpec " + src.path : src.path + (src.line ? ":" + src.line : "")) : "");
  const icon = $derived(src?.litespec ? "book" : "ext");
</script>

{#if src?.path}
  {#if href}
    <a
      class="inline-flex items-center gap-1.5 font-mono text-xs text-muted-foreground hover:text-foreground hover:underline"
      {href}
      title={src.litespec ? "Open in LiteSpec" : "Open " + src.path + " in the editor"}><Icon name={icon} class="size-3.5" /><span>{text}</span></a>
  {:else}
    <span class="inline-flex items-center gap-1.5 font-mono text-xs text-muted-foreground"><Icon name={icon} class="size-3.5" /><span>{text}</span></span>
  {/if}
{/if}
