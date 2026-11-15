<script lang="ts">
  import { capitalize, formatLabel } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { EFFECT_VERBS, effectCondHtml, effectVerbOf, objectList } from "../page-values";

  // What a behavior does beyond its own record: runs, sends and calls.
  type Props = { f: RecordFields };
  let { f }: Props = $props();
  const list = $derived(objectList(f, "effects"));
</script>

{#if list.length}
  <PageSection blockKey="eff" heading="effects" count={list.length}>
    <div class="border-t border-slate-200">
      {#each list as e, i (i)}
        {@const k = effectVerbOf(e)}
        {#if k}
          {@const tgt = String(e[k])}
          {@const op = tgt.split("#")[1]}
          {@const cond = effectCondHtml(e)}
          <div class="grid grid-cols-[96px_minmax(0,1fr)_auto] items-center gap-2.5 border-b border-slate-200 py-2.5 text-sm">
            <span class="inline-flex items-center gap-1.5 text-sm font-medium {k === 'calls' ? 'text-warn-foreground' : ''}">
              <RecordIcon name={EFFECT_VERBS[k].icon} class="size-3.5 text-highlight-foreground" />
              {EFFECT_VERBS[k].label}
            </span>
            <span class="min-w-0">
              <RecordLink id={tgt} />
              {#if op}{" "}<span class="font-mono text-xs text-slate-600">{formatLabel(op)}</span>{/if}
              {#if cond}<span class="mt-0.5 block text-xs text-slate-600">{@html cond}</span>{/if}
            </span>
            {#if e.timing}<Fact text={capitalize(e.timing)} />{/if}
          </div>
        {/if}
      {/each}
    </div>
  </PageSection>
{/if}
