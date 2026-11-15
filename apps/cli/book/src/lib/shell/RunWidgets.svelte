<script lang="ts">
  import { book, formatCount } from "$lib/frame";

  // The completion meter in the top bar. Done counts the loaded records the
  // store confirms complete, over the manifest total, the same number the
  // outline shows, so it never changes with the route. The test run text lives
  // on About this book now.
  const complete = $derived.by(() => {
    void book.rev;
    const real = [...new Set(book.orderWithProduct())].filter((id) => book.isReal(id));
    const done = real.filter((id) => book.get(id) !== undefined && book.completeOf(id)).length;
    const manifest = Object.values(book.man?.totals ?? {}).reduce((a, b) => a + b, 0);
    const total = manifest > 0 ? manifest : real.length;
    const percent = total === 0 ? 100 : Math.round((done / total) * 100);
    return { done, total, percent };
  });
</script>

<span class="flex min-w-[220px] shrink-0 flex-col justify-center gap-1.5 border-l border-shell-border px-4 text-xs whitespace-nowrap text-shell-secondary">
  <span><b class="font-semibold text-shell-foreground tabular-nums">{formatCount(complete.done)}</b> of {formatCount(complete.total)} complete</span>
  <span class="h-1 w-full overflow-hidden bg-shell-border"
    ><i class="block h-full bg-shell-interactive" style="width: {complete.percent}%"></i></span
  >
</span>
