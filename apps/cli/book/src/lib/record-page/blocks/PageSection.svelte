<script lang="ts">
  import { book } from "$lib/frame";
  import { pageSections } from "$lib/record";
  import type { Snippet } from "svelte";

  // One page section: a heading from the field label, an optional count, and an
  // optional aside. Blocks that only list linked records stay off the page; the
  // rail shows them. A zero count or empty HTML hides the block. The heading
  // takes focus so the rail's On this page links can land on it.
  type Props = {
    blockKey: string;
    heading: string;
    count?: number | null;
    aside?: string;
    html?: string | null;
    children?: Snippet;
  };
  let { blockKey, heading, count = null, aside = "", html = null, children }: Props = $props();

  const LINKS_IN_RAIL = new Set([
    "based on", "depends on", "governs", "raised by", "fulfilled by", "governed by", "fulfills",
    "fulfills and serves", "advances", "basedOn", "audience", "requires", "actor", "dependsOn",
    "decides", "raises", "subject", "affects", "replaces", "implements", "calls", "proves",
    "serves", "advances", "cites",
  ]);

  const hidden = $derived.by(() => {
    if (count === 0) return true;
    if (html !== null && !html) return true;
    if (heading.indexOf("Points at this: ") === 0) return true;
    return (
      LINKS_IN_RAIL.has(heading) ||
      LINKS_IN_RAIL.has(heading.toLowerCase()) ||
      LINKS_IN_RAIL.has(book.fieldLabel(heading).toLowerCase())
    );
  });

  // A shown section lists itself in the rail's On this page.
  $effect(() => {
    if (hidden) return;
    return pageSections.add({ key: blockKey, label: book.fieldLabel(heading), count: count ?? null });
  });
</script>

{#if !hidden}
  <section class="mt-10 scroll-mt-20" id={"b-" + blockKey}>
    <h2 tabindex="-1" class="mb-2 flex items-baseline gap-3 text-xl leading-7 font-normal outline-none">
      {book.fieldLabel(heading)}
      {#if count != null}<span class="text-sm text-text-secondary tabular-nums">{count}</span>{/if}
      {#if aside}<span class="ml-auto text-xs tracking-[0.32px] text-text-secondary">{aside}</span>{/if}
    </h2>
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
  </section>
{/if}
