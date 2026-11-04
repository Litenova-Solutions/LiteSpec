<script lang="ts">
  import { cap } from "$lib/book";
  import type { Fields } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import { EFFECT_VERBS, effectCondHtml, effectVerbOf } from "../helpers";

  // What a behavior does beyond its own record: runs, sends, emits and calls.
  let { f }: { f: Fields } = $props();
  const list = $derived((f.effects as Fields[] | undefined) ?? []);
</script>

{#if list.length}
  <Block blockKey="eff" heading="effects" count={list.length}>
    <div class="border-t border-border">
      {#each list as e}
        {@const k = effectVerbOf(e)}
        {#if k}
          {@const tgt = String(e[k])}
          {@const op = tgt.split("#")[1]}
          {@const cond = effectCondHtml(e)}
          <div class="grid grid-cols-[96px_minmax(0,1fr)_auto] items-center gap-2.5 border-b border-border py-2.5 text-sm">
            <span class="inline-flex items-center gap-1.5 text-[13px] font-medium {k === 'calls' ? 'text-warn-foreground' : ''}">
              <Icon name={EFFECT_VERBS[k].icon} class="size-3.5 text-highlight-foreground" />
              {EFFECT_VERBS[k].label}
            </span>
            <span class="min-w-0">
              <InlineRef id={tgt} />
              {#if op}{" "}<span class="font-mono text-[11px] text-muted-foreground">{op.replace(/-/g, " ")}</span>{/if}
              {#if cond}<span class="mt-0.5 block text-xs text-muted-foreground">{@html cond}</span>{/if}
            </span>
            {#if e.timing}<FactBadge text={cap(e.timing)} />{/if}
          </div>
        {/if}
      {/each}
    </div>
  </Block>
{/if}
