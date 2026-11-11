<script lang="ts">
  import { book } from "$lib/frame";
  import type { Snippet } from "svelte";

  // One page section: a heading from the field label, an optional count, and an
  // optional aside. Blocks that only list linked records stay off the page; the
  // rail shows them. A zero count or empty HTML hides the block.
  let {
    blockKey,
    heading,
    count = null,
    aside = "",
    html = null,
    children,
  }: {
    blockKey: string;
    heading: string;
    count?: number | null;
    aside?: string;
    html?: string | null;
    children?: Snippet;
  } = $props();

  const LINKS_IN_RAIL = new Set([
    "based on", "depends on", "governs", "raised by", "fulfilled by", "governed by", "fulfills",
    "fulfills and serves", "solves and advances", "basedOn", "addresses", "actor", "dependsOn",
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
</script>

{#if !hidden}
  <section class="mb-8 scroll-mt-20" id={"b-" + blockKey}>
    <h2 class="mb-3 flex items-center gap-2.5 text-base font-semibold tracking-tight">
      {book.fieldLabel(heading)}
      {#if count != null}<span class="font-mono text-xs font-normal text-muted-foreground">{count}</span>{/if}
      <span class="h-px flex-1 bg-border" aria-hidden="true"></span>
      {#if aside}<span class="text-xs font-normal text-muted-foreground">{aside}</span>{/if}
    </h2>
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
  </section>
{/if}
