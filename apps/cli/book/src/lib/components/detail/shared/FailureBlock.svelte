<script lang="ts">
  import { cap, plural } from "$lib/book";
  import type { Fields } from "$lib/book/types";
  import { duration } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import { modeWord } from "../helpers";

  // What a reaction or job does when it fails: retries, then dead letters or surfaces.
  let { f }: { f: Fields } = $props();
  const o = $derived((f.onFailure as Fields | undefined) ?? null);
  const retry = $derived.by(() => {
    const r = (o?.retry as Fields | undefined) ?? null;
    if (!r) return "";
    return (
      plural(Number(r.maxAttempts ?? 0), "attempt") +
      (r.initialDelay ? ", from " + duration(String(r.initialDelay)) : "") +
      (r.maxDelay ? " up to " + duration(String(r.maxDelay)) : "") +
      (r.backoff ? ", " + modeWord(r.backoff) + " backoff" : "")
    );
  });
</script>

{#if o}
  <Block blockKey="fail" heading="onFailure">
    <Tiles>
      <Tile k="mode">{cap(modeWord(o.mode))}</Tile>
      {#if retry}<Tile k="retry">{retry}</Tile>{/if}
      {#if o.then}<Tile k="then">{modeWord(o.then)}</Tile>{/if}
      {#if o.deadLetter}
        {@const d = o.deadLetter as Fields}
        <Tile k="deadLetter" hint={d.action ? String(d.action) : ""}>After {duration(String(d.after ?? ""))}</Tile>
      {/if}
      {#if o.surfacesIn}<Tile k="surfacesIn"><InlineRef id={String(o.surfacesIn)} /></Tile>{/if}
      {#if o.runbook}<Tile k="runbook"><InlineRef id={String(o.runbook)} /></Tile>{/if}
    </Tiles>
  </Block>
{/if}
