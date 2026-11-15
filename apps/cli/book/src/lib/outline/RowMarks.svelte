<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The marks at the end of a navigation row: what the record lacks, whether
  // it links to the selection, and a failing test. Each keeps its meaning as
  // its accessible name, so the panel needs no legend.
  type Props = { id: string };
  let { id }: Props = $props();
  const lacks = $derived(book.lacks(id));
  const linked = $derived(id !== book.selected && book.related.has(id));
  const failing = $derived(book.hasFailingProof(id));
</script>

{#if lacks}
  <span role="img" aria-label={lacks.charAt(0).toUpperCase() + lacks.slice(1)} class="grid text-warning-ink"><RecordIcon name="missing" class="size-3.5" /></span>
{/if}
{#if linked}
  <span role="img" aria-label="Linked to the selected record" class="grid text-link"><RecordIcon name="link" class="size-3.5" /></span>
{/if}
{#if failing}
  <span role="img" aria-label="A test fails" class="grid text-support-error"><RecordIcon name="flask" class="size-3.5" /></span>
{/if}
