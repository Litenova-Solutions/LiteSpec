<script lang="ts">
  import { base, book } from "$lib/book";
  import { srcHref } from "$lib/book/html";
  import Icon from "./Icon.svelte";
  import TypeTag from "./TypeTag.svelte";

  // A record as one line: type icon, title and type tag. An identifier that
  // names no record in the book reads as itself, never as a link.
  let { id, sub = "" }: { id: string; sub?: string } = $props();
  const b = $derived(base(id));
  const t = $derived(book.typeOf(b));
  const r = $derived(book.get(b));
  const href = $derived(r?.src && !r.src.litespec ? srcHref(book, r.src) : null);
</script>

{#if book.has(b)}
  <span class="inline-flex max-w-full items-center gap-1">
    <button type="button" data-go={b} class="inline-flex min-w-0 items-center gap-1.5 rounded-sm text-left hover:underline">
      <Icon name={t} class="size-3.5 text-muted-foreground" />
      <span class="truncate">{book.title(b)}{#if sub}<span class="text-muted-foreground"> {sub}</span>{/if}</span>
      <TypeTag type={t} />
    </button>
    {#if href}
      <a {href} class="text-muted-foreground hover:text-foreground" title={"Open " + r?.src?.path + " in the editor"} aria-label="Open the source"><Icon name="ext" class="size-3.5" /></a>
    {/if}
  </span>
{:else}
  <span class="inline-flex items-center gap-1.5 text-muted-foreground" title="No record with this identifier is in the book">
    <Icon name={t} class="size-3.5" /><span>{b}</span><TypeTag type={t} />
  </span>
{/if}
