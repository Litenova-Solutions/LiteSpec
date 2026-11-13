<script lang="ts">
  import { book, srcHref } from "$lib/frame";
  import type { SourceRef } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // Every entry links to the file and line it comes from, opened in the editor.
  type Props = { src: SourceRef | null | undefined };
  let { src }: Props = $props();
  const href = $derived(srcHref(book, src));
  const text = $derived(src ? (src.litespec ? "LiteSpec " + src.path : src.path + (src.line ? ":" + src.line : "")) : "");
  const icon = $derived(src?.litespec ? "book" : "ext");
</script>

{#if src?.path}
  {#if href}
    <a
      class="inline-flex max-w-full items-center gap-1.5 font-mono text-xs text-muted-foreground hover:text-foreground hover:underline"
      {href}
      title={src.litespec ? "Open in LiteSpec" : "Open " + src.path + " in the editor"}><RecordIcon name={icon} class="size-3.5 shrink-0" /><span class="min-w-0 break-all">{text}</span></a>
  {:else}
    <span class="inline-flex max-w-full items-center gap-1.5 font-mono text-xs text-muted-foreground"><RecordIcon name={icon} class="size-3.5 shrink-0" /><span class="min-w-0 break-all">{text}</span></span>
  {/if}
{/if}
