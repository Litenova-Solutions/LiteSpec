<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The marks at the end of a navigation row: what the record lacks, whether
  // it links to the selection, and a failing test. Each keeps its tooltip, so
  // the panel needs no legend.
  type Props = { id: string };
  let { id }: Props = $props();
  const lacks = $derived(book.lacks(id));
  const linked = $derived(id !== book.selected && book.related.has(id));
  const failing = $derived(book.hasFailingProof(id));
</script>

{#if lacks}
  <span class="grid text-amber-700" title={lacks.charAt(0).toUpperCase() + lacks.slice(1)}><RecordIcon name="missing" class="size-3.5" /></span>
{/if}
{#if linked}
  <span class="grid text-blue-700" title="Linked to the selected record"><RecordIcon name="link" class="size-3.5" /></span>
{/if}
{#if failing}
  <span class="grid text-red-700" title="A test fails"><RecordIcon name="flask" class="size-3.5" /></span>
{/if}
