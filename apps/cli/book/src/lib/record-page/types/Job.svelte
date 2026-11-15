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
  import { codeOf, idemHint } from "../shared/record-cells";
  import { proseOf } from "../page-prose";
  import { idemTileOf, isRecord, jobFields, objectField, stringField } from "../page-values";

  // A job: when it runs, what it does, and what it does when it fails.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const jf = $derived(jobFields(f));
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const idem = $derived(idemTileOf(f));
  const idemHintHtml = $derived.by(() => {
    const i = objectField(f, "idempotency");
    return i ? idemHint(i) : "";
  });
  const lease = $derived(isRecord(jf.lease) ? jf.lease : {});
  const cursor = $derived(isRecord(jf.cursor) ? jf.cursor : {});
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
<PageSection blockKey="run" heading="schedule">
  <FactGrid>
    {#if jf.schedule}
      <FactCell k="schedule">
        {#if jf.schedule.every}
          Every {duration(String(jf.schedule.every))}
        {:else if jf.schedule.cron}
          {@html codeOf(String(jf.schedule.cron))}
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
    {#if jf.runsWhen}<FactCell k="runsWhen">{jf.runsWhen}</FactCell>{/if}
    {#if jf.cursor}<FactCell k="cursor">{typeof jf.cursor === "string" ? jf.cursor : String(stringField(cursor, "name") ?? "")}</FactCell>{/if}
    {#if idem}
      <FactCell k="idempotency" html={idem.value} hint={idemHintHtml} />
    {/if}
  </FactGrid>
  <DefinitionList>
    {#if jf.selection}<DefinitionRow label="Selection">{jf.selection}</DefinitionRow>{/if}
    {#if jf.serviceLevel}<DefinitionRow label="Service level">{jf.serviceLevel}</DefinitionRow>{/if}
    {#if jf.evidence}<DefinitionRow label="Evidence">{jf.evidence}</DefinitionRow>{/if}
    {#if jf.runbook}<DefinitionRow label="Runbook"><RecordLink id={jf.runbook} /></DefinitionRow>{/if}
  </DefinitionList>
</PageSection>
<EffectsBlock {f} caption={`Effects of ${r.title}`} />
<FailureBlock {f} />
<DataTouched {f} />
