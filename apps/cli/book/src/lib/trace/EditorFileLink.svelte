<script lang="ts">
  import { book, editorHref } from "$lib/frame";

  // A file in the consumer checkout: its name with the directory and line below,
  // opening in the editor when the book names one.
  let { path, line = null, label = "" }: { path: string; line?: number | null; label?: string } = $props();
  const href = $derived(editorHref(book, book.man?.roots?.consumer ?? "", path, line));
  const name = $derived(label || path.split("/").pop() || path);
  const dir = $derived(path.split("/").slice(0, -1).join("/"));
  const title = $derived("Open " + path + (line ? " at line " + line : ""));
</script>

{#if href}
  <a class="block text-foreground" {href} {title}>
    <span class="block text-[13.5px] font-medium break-all hover:underline">{name}</span>
    <span class="block font-mono text-[11px] break-all text-muted-foreground">{dir}{#if line}:{line}{/if}</span>
  </a>
{:else}
  <span class="block">
    <span class="block text-[13.5px] font-medium break-all">{name}</span>
    <span class="block font-mono text-[11px] break-all text-muted-foreground">{dir}{#if line}:{line}{/if}</span>
  </span>
{/if}
