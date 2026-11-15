<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";

  // About this book: the project, the LiteSpec release and the build time, the
  // test run, the completion count and one row per record kind with a count.
  type Props = { onClose: () => void };
  let { onClose }: Props = $props();

  const project = $derived.by(() => {
    void book.rev;
    return book.man?.project?.title || book.man?.project?.id || "";
  });
  const release = $derived.by(() => {
    void book.rev;
    return book.man?.release || "";
  });
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

  interface KindRow {
    type: string;
    count: number;
    layer: string;
    name: string;
    layerLabel: string;
  }
  // One row per record type the manifest totals carry, in the manifest's layer
  // order, then by kind name.
  const kinds = $derived.by((): KindRow[] => {
    void book.rev;
    const order = book.man?.layers.map((layer) => layer.key) ?? [];
    const rank = (layer: string): number => {
      const at = order.indexOf(layer);
      return at < 0 ? order.length : at;
    };
    return Object.entries(book.man?.totals ?? {})
      .map(([type, count]): KindRow => {
        const layer = book.typeLayer(type);
        return { type, count, layer, name: book.typeName(type), layerLabel: book.layerByKey[layer]?.label ?? layer };
      })
      .filter((row) => row.count > 0)
      .sort((a, b) => rank(a.layer) - rank(b.layer) || a.name.localeCompare(b.name));
  });
  // The build moment in full, date and time, in the reader's locale.
  const builtAt = $derived.by(() => {
    void book.rev;
    const at = new Date(book.man?.generatedOn ?? "");
    return isNaN(+at) ? "" : at.toLocaleString([], { dateStyle: "long", timeStyle: "short" });
  });
</script>

<div class="w-full max-w-[52rem] px-8 pt-6 pb-16">
  <div class="flex items-start justify-between gap-6">
    <h1 class="text-[32px] leading-10 font-normal">About this book</h1>
    <button type="button" aria-label="Close" onclick={onClose} class="grid size-8 shrink-0 place-items-center hover:bg-hover-wash">
      <RecordIcon name="x" class="size-4" />
    </button>
  </div>
  <p class="mt-6 max-w-[44rem] text-xl leading-7 font-light">The build behind this book.</p>

  <section aria-labelledby="about-book-h">
    <h2 id="about-book-h" class="mt-10 mb-2 text-xl leading-7 font-normal">Book</h2>
    <dl class="grid grid-cols-[minmax(9rem,25%)_minmax(0,1fr)] border-t border-border-subtle">
      <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">Project</dt>
      <dd class="border-b border-border-subtle py-3">{project}</dd>
      {#if release}
        <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">LiteSpec version</dt>
        <dd class="border-b border-border-subtle py-3">{release}</dd>
      {/if}
      <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">Built</dt>
      <dd class="border-b border-border-subtle py-3">{builtAt}</dd>
    </dl>
  </section>

  <section aria-labelledby="about-proof-h">
    <h2 id="about-proof-h" class="mt-10 mb-2 text-xl leading-7 font-normal">Proof</h2>
    <dl class="grid grid-cols-[minmax(9rem,25%)_minmax(0,1fr)] border-t border-border-subtle">
      <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">Test run</dt>
      <dd class="border-b border-border-subtle py-3">
        <span data-explain="norun" class="cursor-help">
          {#if run && run.date}
            Test run {book.fmtDate(run.date)}: <b class="font-medium tabular-nums">{formatCount(run.passed ?? 0)}</b> passed{#if run.failed},
              <b class="font-medium tabular-nums">{formatCount(run.failed)}</b> failed{/if}
          {:else}
            No test run recorded.
          {/if}
        </span>
      </dd>
    </dl>
  </section>

  <section aria-labelledby="about-completion-h">
    <h2 id="about-completion-h" class="mt-10 mb-2 text-xl leading-7 font-normal">Completion</h2>
    <p><b class="font-semibold tabular-nums">{formatCount(complete.done)}</b> of {formatCount(complete.total)} complete</p>
    <div class="mt-3 h-2 bg-layer-accent" role="img" aria-label="{complete.percent}% complete"
      ><i class="block h-full bg-interactive" style="width: {complete.percent}%"></i></div
    >
  </section>

  <section aria-labelledby="about-kinds-h">
    <h2 id="about-kinds-h" class="mt-10 mb-2 text-xl leading-7 font-normal">Record kinds <span class="text-sm text-text-secondary">{formatCount(kinds.length)}</span></h2>
    <!-- The scroll region takes keyboard focus so a keyboard user can scroll a wide table. -->
    <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
    <div class="overflow-x-auto" tabindex="0" role="region" aria-labelledby="about-kinds-cap">
      <table class="w-full border-collapse text-sm">
        <caption class="sr-only" id="about-kinds-cap">Record kinds by layer, with letter codes and counts</caption>
        <thead>
          <tr>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Code</th>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Kind</th>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Layer</th>
            <th scope="col" class="h-10 bg-layer-accent px-4 text-left font-semibold">Records</th>
          </tr>
        </thead>
        <tbody>
          {#each kinds as row (row.type)}
            <tr class="group">
              <td class="border-t border-border-subtle bg-layer px-4 py-3 align-top group-hover:bg-layer-hover"><TypeTag type={row.type} /></td>
              <td class="border-t border-border-subtle bg-layer px-4 py-3 align-top group-hover:bg-layer-hover">{row.name}</td>
              <td class="border-t border-border-subtle bg-layer px-4 py-3 align-top group-hover:bg-layer-hover">{row.layerLabel}</td>
              <td class="border-t border-border-subtle bg-layer px-4 py-3 text-right align-top tabular-nums group-hover:bg-layer-hover">{formatCount(row.count)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
</div>
