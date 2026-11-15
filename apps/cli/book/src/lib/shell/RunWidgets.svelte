<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import { RecordIcon } from "$lib/record";

  // The test run and completion widgets. Both read rev so they update as shards load.
  // The completion total is the manifest total, the same number the outline shows,
  // so it never changes with the route; done counts the loaded records the store
  // confirms complete.
  const run = $derived.by(() => {
    void book.rev;
    return book.man?.testRun ?? null;
  });
  const complete = $derived.by(() => {
    void book.rev;
    const real = [...new Set(book.order)].filter((id) => book.isReal(id));
    const done = real.filter((id) => book.get(id) !== undefined && book.completeOf(id)).length;
    const manifest = Object.values(book.man?.totals ?? {}).reduce((a, b) => a + b, 0);
    const total = manifest > 0 ? manifest : real.length;
    const percent = total === 0 ? 100 : Math.round((done / total) * 100);
    return { done, total, percent };
  });
</script>

<button
  type="button"
  data-explain="norun"
  class="inline-flex cursor-help items-center gap-2 text-xs whitespace-nowrap text-slate-600"
>
  <RecordIcon name="flask" class="size-3.5" />
  {#if run && run.date}
    <span
      >Test run {book.fmtDate(run.date)}: <b class="font-medium text-slate-900 tabular-nums">{formatCount(run.passed ?? 0)}</b> passed{#if run.failed},
        <b class="font-medium text-destructive tabular-nums">{formatCount(run.failed)}</b> failed{/if}</span
    >
  {:else}
    <span>No test run recorded.</span>
  {/if}
</button>
<span class="inline-flex items-center gap-2 text-xs whitespace-nowrap text-slate-600">
  <span><b class="font-semibold text-slate-900 tabular-nums">{formatCount(complete.done)}</b> of {formatCount(complete.total)} complete</span>
  <span class="h-1 w-24 overflow-hidden rounded-full border border-slate-200 bg-slate-100"
    ><i class="block h-full bg-convention-foreground" style="width: {complete.percent}%"></i></span
  >
</span>
