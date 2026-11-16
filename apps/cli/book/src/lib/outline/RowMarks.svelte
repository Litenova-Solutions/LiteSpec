<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The marks at the end of a navigation row: what the record lacks, whether
  // it links to the selection, and a failing test. Each keeps its meaning as
  // its accessible name, so the panel needs no legend. With no mark it renders
  // nothing, so the row keeps no empty column after the item text.
  type Props = { id: string };
  let { id }: Props = $props();
  const lacks = $derived(book.lacks(id));
  const linked = $derived(id !== book.selected && book.related.has(id));
  const failing = $derived(book.hasFailingProof(id));
</script>

{#if lacks || linked || failing}
  <span class="flex flex-none items-center gap-2">
    {#if lacks}
      <span role="img" aria-label={lacks.charAt(0).toUpperCase() + lacks.slice(1)} class="grid text-warning-ink"><RecordIcon name="missing" class="size-3.5" /></span>
    {/if}
    {#if linked}
      <span role="img" aria-label="Linked to the selected record" class="grid text-link"><RecordIcon name="link" class="size-3.5" /></span>
    {/if}
    {#if failing}
      <span role="img" aria-label="A test fails" class="grid text-support-error"><RecordIcon name="flask" class="size-3.5" /></span>
    {/if}
  </span>
{/if}
