<script lang="ts">
  import type { BookRecord, Fields } from "$lib/book/types";
  import { duration } from "$lib/book/html";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import FailureBlock from "../shared/FailureBlock.svelte";
  import { codeCls, idemTileOf, proseOf } from "../helpers";

  // A job: when it runs, what it does, and what it does when it fails.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const s = $derived((f.schedule as Fields | undefined) ?? null);
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const idem = $derived(idemTileOf(f));
</script>

{#if whatHappens}
  <Block blockKey="wh" heading="whatHappens"><Prose html={whatHappens} /></Block>
{/if}
<Block blockKey="run" heading="schedule">
  <Tiles>
    {#if f.kind === "manual"}
      <Tile k="manual">Runs by hand</Tile>
    {:else if s}
      <Tile k="schedule">
        {#if s.every}
          Every {duration(String(s.every))}
        {:else if s.cron}
          <span class={codeCls}>{String(s.cron)}</span>
        {:else if s.at === "startup"}
          At startup
        {/if}
      </Tile>
    {/if}
    {#if f.guard}<Tile k="guard">{String(f.guard)}</Tile>{/if}
    {#if f.batchSize}<Tile k="batchSize" big>{String(f.batchSize)}</Tile>{/if}
    {#if f.lease}
      <Tile k="lease">
        {typeof f.lease === "string" ? f.lease : String((f.lease as Fields).name ?? "") + " for " + duration(String((f.lease as Fields).duration ?? ""))}
      </Tile>
    {/if}
    {#if f.runsWhere}<Tile k="runsWhere">{String(f.runsWhere)}</Tile>{/if}
    {#if f.cursor}<Tile k="cursor">{typeof f.cursor === "string" ? f.cursor : String((f.cursor as Fields).name ?? "")}</Tile>{/if}
    {#if idem}
      <Tile k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </Tiles>
  <Kv>
    {#if f.selection}<KvRow label="Selection">{String(f.selection)}</KvRow>{/if}
    {#if f.serviceLevel}<KvRow label="Service level">{String(f.serviceLevel)}</KvRow>{/if}
    {#if f.evidence}<KvRow label="Evidence">{String(f.evidence)}</KvRow>{/if}
    {#if f.runbook}<KvRow label="Runbook"><InlineRef id={String(f.runbook)} /></KvRow>{/if}
  </Kv>
</Block>
<EffectsBlock {f} />
<FailureBlock {f} />
<DataTouched {f} />
