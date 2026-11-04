<script lang="ts">
  import { book, n0 } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";

  // The test run and completion widgets. Both read rev so they update as shards load.
  let run = $derived.by(() => {
    void book.rev;
    return book.man?.testRun ?? null;
  });
  let complete = $derived.by(() => {
    void book.rev;
    const real = book.order.filter((id) => book.isReal(id));
    const done = real.filter((id) => book.completeOf(id)).length;
    return { done, total: real.length, pct: Math.max(1, Math.round((done / Math.max(1, real.length)) * 100)) };
  });
</script>

<button
  type="button"
  data-explain="norun"
  class="inline-flex cursor-help items-center gap-2 text-xs whitespace-nowrap text-muted-foreground"
>
  <Icon name="flask" class="size-3.5" />
  {#if run && run.date}
    <span
      >Test run {book.fmtDate(run.date)}: <b class="font-medium text-foreground tabular-nums">{n0(run.passed ?? 0)}</b> passed{#if run.failed},
        <b class="font-medium text-warn-foreground tabular-nums">{run.failed}</b> failed{/if}</span
    >
  {:else}
    <span>No test run in the model</span>
  {/if}
</button>
<span class="inline-flex items-center gap-2 text-xs whitespace-nowrap text-muted-foreground">
  <span><b class="font-semibold text-foreground tabular-nums">{n0(complete.done)}</b> of {n0(complete.total)} complete</span>
  <span class="h-1 w-24 overflow-hidden rounded-full border border-border bg-muted"
    ><i class="block h-full bg-convention-foreground" style="width: {complete.pct}%"></i></span
  >
</span>
