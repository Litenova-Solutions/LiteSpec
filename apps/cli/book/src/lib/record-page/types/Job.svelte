<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { duration } from "$lib/frame";
  import { RecordLink, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import FailureBlock from "../shared/FailureBlock.svelte";
  import { codeCls } from "../page-html";
  import { proseOf } from "../page-prose";
  import { idemTileOf } from "../page-values";

  // A job: when it runs, what it does, and what it does when it fails.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const s = $derived((f.schedule as RecordFields | undefined) ?? null);
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const idem = $derived(idemTileOf(f));
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
<PageSection blockKey="run" heading="schedule">
  <FactGrid>
    {#if f.kind === "manual"}
      <FactCell k="manual">Runs by hand</FactCell>
    {:else if s}
      <FactCell k="schedule">
        {#if s.every}
          Every {duration(String(s.every))}
        {:else if s.cron}
          <span class={codeCls}>{String(s.cron)}</span>
        {:else if s.at === "startup"}
          At startup
        {/if}
      </FactCell>
    {/if}
    {#if f.guard}<FactCell k="guard">{String(f.guard)}</FactCell>{/if}
    {#if f.batchSize}<FactCell k="batchSize" big>{String(f.batchSize)}</FactCell>{/if}
    {#if f.lease}
      <FactCell k="lease">
        {typeof f.lease === "string" ? f.lease : String((f.lease as RecordFields).name ?? "") + " for " + duration(String((f.lease as RecordFields).duration ?? ""))}
      </FactCell>
    {/if}
    {#if f.runsWhere}<FactCell k="runsWhere">{String(f.runsWhere)}</FactCell>{/if}
    {#if f.cursor}<FactCell k="cursor">{typeof f.cursor === "string" ? f.cursor : String((f.cursor as RecordFields).name ?? "")}</FactCell>{/if}
    {#if idem}
      <FactCell k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </FactGrid>
  <DefinitionList>
    {#if f.selection}<DefinitionRow label="Selection">{String(f.selection)}</DefinitionRow>{/if}
    {#if f.serviceLevel}<DefinitionRow label="Service level">{String(f.serviceLevel)}</DefinitionRow>{/if}
    {#if f.evidence}<DefinitionRow label="Evidence">{String(f.evidence)}</DefinitionRow>{/if}
    {#if f.runbook}<DefinitionRow label="Runbook"><RecordLink id={String(f.runbook)} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
<EffectsBlock {f} />
<FailureBlock {f} />
<DataTouched {f} />
