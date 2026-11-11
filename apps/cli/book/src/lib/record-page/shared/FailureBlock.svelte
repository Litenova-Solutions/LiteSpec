<script lang="ts">
  import { capitalize, duration, pluralize } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { modeWord, objectField, stringField } from "../page-values";

  // What a reaction or job does when it fails: retries, then dead letters or surfaces.
  type Props = { f: RecordFields };
  let { f }: Props = $props();
  const o = $derived(objectField(f, "onFailure"));
  const retry = $derived.by(() => {
    const r = objectField(o, "retry");
    if (!r) return "";
    return (
      pluralize(Number(r.maxAttempts ?? 0), "attempt") +
      (r.initialDelay ? ", from " + duration(String(r.initialDelay)) : "") +
      (r.maxDelay ? " up to " + duration(String(r.maxDelay)) : "") +
      (r.backoff ? ", " + modeWord(r.backoff) + " backoff" : "")
    );
  });
  const dead = $derived(objectField(o, "deadLetter"));
  const surfacesIn = $derived(stringField(o, "surfacesIn"));
  const runbook = $derived(stringField(o, "runbook"));
</script>

{#if o}
  <PageSection blockKey="fail" heading="onFailure">
    <FactGrid>
      <FactCell k="mode">{capitalize(modeWord(o.mode))}</FactCell>
      {#if retry}<FactCell k="retry">{retry}</FactCell>{/if}
      {#if o.then}<FactCell k="then">{modeWord(o.then)}</FactCell>{/if}
      {#if dead}
        <FactCell k="deadLetter" hint={dead.action ? String(dead.action) : ""}>After {duration(String(dead.after ?? ""))}</FactCell>
      {/if}
      {#if surfacesIn}<FactCell k="surfacesIn"><RecordLink id={surfacesIn} /></FactCell>{/if}
      {#if runbook}<FactCell k="runbook"><RecordLink id={runbook} /></FactCell>{/if}
    </FactGrid>
  </PageSection>
{/if}
