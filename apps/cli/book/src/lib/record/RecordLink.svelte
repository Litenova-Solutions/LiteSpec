<script lang="ts">
  import { book, stripStep } from "$lib/frame";

  // A record named inside a sentence: its title as a link, or the bare
  // identifier. It renders the same markup as inlineRefHtml in page-html.ts.
  type Props = { id: string };
  let { id }: Props = $props();
  const b = $derived(stripStep(id) ?? "");
</script>

{#if book.has(b)}
  <button type="button" class="underline decoration-strong underline-offset-2 hover:decoration-foreground" data-go={b}>{book.title(b)}</button>
{:else}
  <span class="font-mono text-[0.9em] text-muted-foreground" title="No record with this identifier is in the book">{b}</span>
{/if}
