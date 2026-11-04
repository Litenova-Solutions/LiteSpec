<script lang="ts">
  import { book, cap, plural } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import StatusIcon from "$lib/components/book/StatusIcon.svelte";

  // The marks at the end of a navigation row: what the record lacks, whether
  // it links to the selection, its open questions, its risks, a failing test.
  let { id }: { id: string } = $props();
  const lacks = $derived(book.lacks(id));
  const linked = $derived(id !== book.selected && book.related.has(id));
  const questions = $derived(book.openQs(id).length);
  const risks = $derived(book.riskWords(id));
  const failing = $derived(book.proofState(id) === "failing");
</script>

{#if lacks}
  <span class="grid text-warn-foreground" title={cap(lacks)}><Icon name="missing" class="size-3.5" /></span>
{/if}
{#if linked}
  <span class="grid text-highlight-foreground" title="Linked to the selected record"><Icon name="link" class="size-3.5" /></span>
{/if}
{#if questions}
  <span class="inline-flex items-center gap-[3px] font-mono text-[11px] font-medium text-pattern-foreground" title={plural(questions, "open question")}><Icon name="question" class="size-3.5" />{questions}</span>
{/if}
{#if risks.length}
  <span class="text-[11.5px] font-semibold whitespace-nowrap text-warn-foreground" title="Risk: {risks.join(', ')}">{risks.join(", ")}</span>
{/if}
{#if failing}
  <span class="grid text-warn-foreground" title="A test fails"><Icon name="flask" class="size-3.5" /></span>
{/if}
<StatusIcon {id} />
