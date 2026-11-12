<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The test run and completion widgets. Both read rev so they update as shards load.
  const run = $derived.by(() => {
    void book.rev;
    return book.man?.testRun ?? null;
  });
  const complete = $derived.by(() => {
    void book.rev;
    const real = book.order.filter((id) => book.isReal(id));
    return book.completionSummary(real);
  });
</script>

<button
  type="button"
  data-explain="norun"
  class="inline-flex cursor-help items-center gap-2 text-xs whitespace-nowrap text-muted-foreground"
>
  <RecordIcon name="flask" class="size-3.5" />
  {#if run && run.date}
    <span
      >Test run {book.fmtDate(run.date)}: <b class="font-medium text-foreground tabular-nums">{formatCount(run.passed ?? 0)}</b> passed{#if run.failed},
        <b class="font-medium text-warn-foreground tabular-nums">{run.failed}</b> failed{/if}</span
    >
  {:else}
    <span>No test run in the model</span>
  {/if}
</button>
<span class="inline-flex items-center gap-2 text-xs whitespace-nowrap text-muted-foreground">
  <span><b class="font-semibold text-foreground tabular-nums">{formatCount(complete.done)}</b> of {formatCount(complete.total)} complete</span>
  <span class="h-1 w-24 overflow-hidden rounded-full border border-border bg-muted"
    ><i class="block h-full bg-convention-foreground" style="width: {complete.percent}%"></i></span
  >
</span>
