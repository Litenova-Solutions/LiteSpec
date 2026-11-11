<script lang="ts">
  import { book, stripStep } from "$lib/frame";

  // A record named inside a sentence: its title as a link, or the bare identifier.
  let { id }: { id: string } = $props();
  const b = $derived(stripStep(id));
</script>

{#if book.has(b)}
  <button type="button" data-go={b} class="underline decoration-strong underline-offset-2 hover:decoration-foreground">{book.title(b)}</button>
{:else}
  <span class="font-mono text-[0.9em] text-muted-foreground" title="No record with this identifier is in the book">{b}</span>
{/if}
