<script lang="ts">
  import { book, srcHref } from "$lib/frame";
  import type { SourceRef } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // Every entry links to the file and line it comes from, as a source link; a LiteSpec file is plain text.
  type Props = { src: SourceRef | null | undefined };
  let { src }: Props = $props();
  const href = $derived(srcHref(book, src));
  const file = $derived(src?.path?.split("/").pop() ?? "");
  const text = $derived(src ? (src.litespec ? "LiteSpec " + src.path : file + (src.line ? ":" + src.line : "")) : "");
  const icon = $derived(src?.litespec ? "book" : "ext");
</script>

{#if src?.path}
  {#if href}
    <a
      class="inline-flex max-w-full items-center gap-1 text-sm text-link hover:underline"
      {href}><span class="min-w-0 break-all">{text}</span><RecordIcon name={icon} class="size-4 shrink-0" /></a>
  {:else}
    <span class="inline-flex max-w-full items-center gap-1 text-sm text-text-secondary"><span class="min-w-0 break-all">{text}</span><RecordIcon name={icon} class="size-4 shrink-0" /></span>
  {/if}
{/if}
