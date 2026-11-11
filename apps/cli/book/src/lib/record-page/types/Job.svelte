<script lang="ts">
  import type { BookRecord } from "$lib/frame";
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
  import { idemTileOf, isRecord, jobFields, stringField } from "../page-values";

  // A job: when it runs, what it does, and what it does when it fails.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const jf = $derived(jobFields(f));
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const idem = $derived(idemTileOf(f));
  const lease = $derived(isRecord(jf.lease) ? jf.lease : {});
  const cursor = $derived(isRecord(jf.cursor) ? jf.cursor : {});
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
<PageSection blockKey="run" heading="schedule">
  <FactGrid>
    {#if jf.kind === "manual"}
      <FactCell k="manual">Runs by hand</FactCell>
    {:else if jf.schedule}
      <FactCell k="schedule">
        {#if jf.schedule.every}
          Every {duration(String(jf.schedule.every))}
        {:else if jf.schedule.cron}
          <span class={codeCls}>{String(jf.schedule.cron)}</span>
        {:else if jf.schedule.at === "startup"}
          At startup
        {/if}
      </FactCell>
    {/if}
    {#if jf.guard}<FactCell k="guard">{jf.guard}</FactCell>{/if}
    {#if jf.batchSize}<FactCell k="batchSize" big>{jf.batchSize}</FactCell>{/if}
    {#if jf.lease}
      <FactCell k="lease">
        {typeof jf.lease === "string"
          ? jf.lease
          : String(stringField(lease, "name") ?? "") + " for " + duration(stringField(lease, "duration") ?? "")}
      </FactCell>
    {/if}
    {#if jf.runsWhere}<FactCell k="runsWhere">{jf.runsWhere}</FactCell>{/if}
    {#if jf.cursor}<FactCell k="cursor">{typeof jf.cursor === "string" ? jf.cursor : String(stringField(cursor, "name") ?? "")}</FactCell>{/if}
    {#if idem}
      <FactCell k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </FactGrid>
  <DefinitionList>
    {#if jf.selection}<DefinitionRow label="Selection">{jf.selection}</DefinitionRow>{/if}
    {#if jf.serviceLevel}<DefinitionRow label="Service level">{jf.serviceLevel}</DefinitionRow>{/if}
    {#if jf.evidence}<DefinitionRow label="Evidence">{jf.evidence}</DefinitionRow>{/if}
    {#if jf.runbook}<DefinitionRow label="Runbook"><RecordLink id={jf.runbook} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
<EffectsBlock {f} />
<FailureBlock {f} />
<DataTouched {f} />
