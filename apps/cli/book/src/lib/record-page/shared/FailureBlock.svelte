<script lang="ts">
  import { capitalize, duration, pluralize } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { modeWord } from "../page-values";

  // What a reaction or job does when it fails: retries, then dead letters or surfaces.
  let { f }: { f: RecordFields } = $props();
  const o = $derived((f.onFailure as RecordFields | undefined) ?? null);
  const retry = $derived.by(() => {
    const r = (o?.retry as RecordFields | undefined) ?? null;
    if (!r) return "";
    return (
      pluralize(Number(r.maxAttempts ?? 0), "attempt") +
      (r.initialDelay ? ", from " + duration(String(r.initialDelay)) : "") +
      (r.maxDelay ? " up to " + duration(String(r.maxDelay)) : "") +
      (r.backoff ? ", " + modeWord(r.backoff) + " backoff" : "")
    );
  });
</script>

{#if o}
  <PageSection blockKey="fail" heading="onFailure">
    <FactGrid>
      <FactCell k="mode">{capitalize(modeWord(o.mode))}</FactCell>
      {#if retry}<FactCell k="retry">{retry}</FactCell>{/if}
      {#if o.then}<FactCell k="then">{modeWord(o.then)}</FactCell>{/if}
      {#if o.deadLetter}
        {@const d = o.deadLetter as RecordFields}
        <FactCell k="deadLetter" hint={d.action ? String(d.action) : ""}>After {duration(String(d.after ?? ""))}</FactCell>
      {/if}
      {#if o.surfacesIn}<FactCell k="surfacesIn"><RecordLink id={String(o.surfacesIn)} /></FactCell>{/if}
      {#if o.runbook}<FactCell k="runbook"><RecordLink id={String(o.runbook)} /></FactCell>{/if}
    </FactGrid>
  </PageSection>
{/if}
