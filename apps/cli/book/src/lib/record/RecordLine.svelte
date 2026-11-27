<script lang="ts">
  import { book, srcHref, stripStep } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";
  import TypeTag from "./TypeTag.svelte";

  // A record as one line: the kind badge and the title. An identifier that
  // names no record in the book reads as itself, never as a link.
  type Props = { id: string; suffix?: string };
  let { id, suffix = "" }: Props = $props();
  const b = $derived(stripStep(id) ?? "");
  const t = $derived(book.typeOf(b));
  const r = $derived(book.get(b));
  const href = $derived(r?.src && !r.src.litespec ? srcHref(book, r.src) : null);
</script>

{#if book.has(b)}
  <span class="inline-flex max-w-full items-center gap-2">
    <TypeTag type={t} />
    <a href={"#" + encodeURIComponent(b)} data-go={b} class="min-w-0 truncate text-left text-link hover:underline">
      {book.title(b)}{#if suffix}<span class="text-text-secondary"> {suffix}</span>{/if}
    </a>
    {#if href}
      <a {href} class="shrink-0 text-text-secondary hover:text-foreground" aria-label="Open the source"><RecordIcon name="ext" class="size-3.5" /></a>
    {/if}
  </span>
{:else}
  <span class="inline-flex max-w-full items-center gap-2">
    <TypeTag type={t} />
    <span class="min-w-0 truncate font-mono text-xs text-text-secondary">{b}</span>
  </span>
{/if}
