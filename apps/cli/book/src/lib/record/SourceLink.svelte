<script lang="ts">
  import { book, srcHref } from "$lib/frame";
  import type { SourceRef } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // Every entry links to the file and line it comes from, as a source link; a LiteSpec file is plain text.
  type Props = { src: SourceRef | null | undefined };
  let { src }: Props = $props();
  const href = $derived(srcHref(book, src));
  const text = $derived(src ? (src.litespec ? "LiteSpec " + src.path : src.path + (src.line ? ":" + src.line : "")) : "");
  const icon = $derived(src?.litespec ? "book" : "ext");
</script>

{#if src?.path}
  {#if href}
    <a
      class="inline-flex max-w-full items-center gap-2 font-mono text-xs text-slate-600 hover:text-slate-900 hover:underline"
      {href}
      title={"Open " + src.path}><RecordIcon name={icon} class="size-3.5 shrink-0" /><span class="min-w-0 break-all">{text}</span></a>
  {:else}
    <span class="inline-flex max-w-full items-center gap-2 font-mono text-xs text-slate-600"><RecordIcon name={icon} class="size-3.5 shrink-0" /><span class="min-w-0 break-all">{text}</span></span>
  {/if}
{/if}
