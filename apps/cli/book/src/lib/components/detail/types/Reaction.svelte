<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import FailureBlock from "../shared/FailureBlock.svelte";
  import { idemTileOf, proseOf, triggeredByHtml } from "../helpers";

  // A reaction: what triggers it, what it does, and what it does when it fails.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const trig = $derived(triggeredByHtml(f.triggeredBy));
  const idem = $derived(idemTileOf(f));
</script>

{#if whatHappens}
  <Block blockKey="wh" heading="whatHappens"><Prose html={whatHappens} /></Block>
{/if}
{#if trig || f.skippedWhen}
  <Block blockKey="trig" heading="triggeredBy">
    {@html trig}
    {#if f.skippedWhen}<div class="text-sm">Skipped when {String(f.skippedWhen)}</div>{/if}
  </Block>
{/if}
<EffectsBlock {f} />
<Block blockKey="guar" heading="idempotency">
  <Tiles>
    {#if idem}
      <Tile k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </Tiles>
</Block>
<FailureBlock {f} />
<DataTouched {f} />
