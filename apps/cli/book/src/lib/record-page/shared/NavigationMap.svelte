<script lang="ts">
  // The navigation map of an entry point: its screens as boxes, each screen's
  // leadsTo as arrows on a shared bus. Only edges between screens on the map
  // are drawn; each box links to its screen.
  type Props = {
    screens: { id: string; title: string }[];
    leads: Record<string, string[]>;
    start: string | null;
  };
  let { screens, leads, start }: Props = $props();

  const boxW = 300;
  const boxH = 46;
  const gap = 14;
  const pad = 16;
  const busX = pad + boxW + 84;

  function short(title: string, max: number): string {
    return title.length > max ? title.slice(0, max - 1).trimEnd() + "…" : title;
  }

  const edges = $derived.by(() => {
    const seen = new Set<string>();
    const out: { from: number; to: number; self: boolean }[] = [];
    screens.forEach((s, from) => {
      for (const target of leads[s.id] ?? []) {
        const to = screens.findIndex((x) => x.id === target);
        const key = from + ">" + to;
        if (to >= 0 && !seen.has(key)) {
          seen.add(key);
          out.push({ from, to, self: from === to });
        }
      }
    });
    return out;
  });

  const height = $derived(pad * 2 + screens.length * boxH + Math.max(0, screens.length - 1) * gap);
  const rowY = (i: number) => pad + i * (boxH + gap);
  const midY = (i: number) => rowY(i) + boxH / 2;
</script>

{#if screens.length}
  <div class="overflow-hidden border border-border-subtle bg-background">
    <!-- One map per page, so the marker id stays unique. -->
    <svg viewBox="0 0 {busX + pad} {height}" class="block h-auto w-full" role="img" aria-label="Navigation map">
      <defs>
        <marker id="nav-arrow" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
          <path d="M0 0L8 4L0 8z" class="fill-border-subtle" />
        </marker>
      </defs>
      {#each edges as e (e.from + ">" + e.to)}
        {#if e.self}
          <path
            d="M{pad + boxW} {midY(e.from) - 8} h28 v16 h-28"
            class="fill-none stroke-border-subtle"
            stroke-width="1.5"
            marker-end="url(#nav-arrow)"
          />
        {:else}
          <path
            d="M{pad + boxW} {midY(e.from)} H{busX} V{midY(e.to)} H{pad + boxW + 6}"
            class="fill-none stroke-border-subtle"
            stroke-width="1.5"
            marker-end="url(#nav-arrow)"
          />
        {/if}
      {/each}
      {#each screens as s, i (s.id)}
        {@const isStart = s.id === start}
        <a href="#{s.id}">
          <title>{s.title}{isStart ? " (start)" : ""}</title>
          <rect
            x={pad}
            y={rowY(i)}
            width={boxW}
            height={boxH}
            class={isStart ? "fill-background stroke-interactive" : "fill-background stroke-border-subtle"}
            stroke-width={isStart ? 1.5 : 1}
          />
          <text x={pad + 14} y={midY(i) + 5} class="fill-foreground" font-size="14">{short(s.title, isStart ? 24 : 34)}</text>
          {#if isStart}
            <text x={pad + boxW - 14} y={midY(i) + 4} text-anchor="end" class="fill-interactive" font-size="12">Start</text>
          {/if}
        </a>
      {/each}
    </svg>
  </div>
{:else}
  <p class="m-0 text-sm text-text-secondary">No screens.</p>
{/if}
