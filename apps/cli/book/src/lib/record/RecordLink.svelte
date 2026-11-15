<script lang="ts">
  import { book, stripStep } from "$lib/frame";

  // A record named inside a sentence: its title as a link, or the bare
  // identifier. It renders the same shape as inlineRefHtml in page-html.ts.
  type Props = { id: string };
  let { id }: Props = $props();
  const b = $derived(stripStep(id) ?? "");
</script>

{#if book.has(b)}
  <a href={"#" + encodeURIComponent(b)} data-go={b} class="text-blue-700 underline underline-offset-2">{book.title(b)}</a>
{:else}
  <span class="font-mono text-xs text-slate-600" title="No record with this identifier is in the book">{b}</span>
{/if}
