<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import FailureBlock from "../shared/FailureBlock.svelte";
  import { proseOf } from "../page-prose";
  import { idemTileOf, triggeredByHtml } from "../page-values";

  // A reaction: what triggers it, what it does, and what it does when it fails.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const trig = $derived(triggeredByHtml(f.triggeredBy));
  const idem = $derived(idemTileOf(f));
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if trig || f.skippedWhen}
  <PageSection blockKey="trig" heading="triggeredBy">
    {@html trig}
    {#if f.skippedWhen}<div class="text-sm">Skipped when {String(f.skippedWhen)}</div>{/if}
  </PageSection>
{/if}
<EffectsBlock {f} />
<PageSection blockKey="guar" heading="idempotency">
  <FactGrid>
    {#if idem}
      <FactCell k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </FactGrid>
</PageSection>
<FailureBlock {f} />
<DataTouched {f} />
