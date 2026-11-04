<script lang="ts">
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";

  // The marks at the end of a navigation row: what the record lacks, whether
  // it links to the selection, and a failing test.
  let { id }: { id: string } = $props();
  const lacks = $derived(book.lacks(id));
  const linked = $derived(id !== book.selected && book.related.has(id));
  const failing = $derived(book.proofState(id) === "failing");
</script>

{#if lacks}
  <span class="grid text-warn-foreground" title={lacks.charAt(0).toUpperCase() + lacks.slice(1)}><Icon name="missing" class="size-3.5" /></span>
{/if}
{#if linked}
  <span class="grid text-highlight-foreground" title="Linked to the selected record"><Icon name="link" class="size-3.5" /></span>
{/if}
{#if failing}
  <span class="grid text-warn-foreground" title="A test fails"><Icon name="flask" class="size-3.5" /></span>
{/if}
