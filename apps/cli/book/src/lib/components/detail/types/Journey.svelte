<script lang="ts">
  import { base, book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Prose from "$lib/components/book/Prose.svelte";
  import Ref from "$lib/components/book/Ref.svelte";
  import Block from "../blocks/Block.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import { proseOf, serverLineInfo } from "../helpers";

  // A journey: when it counts as finished, and each step with what serves it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const steps = $derived(((f.steps as Fields[] | undefined) ?? []) as Fields[]);
  const srv = $derived(((r.stepServers as Record<string, string[]> | undefined) ?? {}) as Record<string, string[]>);
  const screens = $derived(((r.stepScreens as Record<string, string[]> | undefined) ?? {}) as Record<string, string[]>);
  const between = $derived(proseOf(r, "betweenSteps"));
</script>

{#if f.finishedWhen}
  <div class="mb-6 grid grid-cols-[20px_1fr] gap-2.5 border-l-[3px] border-convention-foreground py-0.5 pl-3.5 text-[14.5px]">
    <Icon name="passing" class="mt-1 size-3.5 text-convention-foreground" />
    <div><b>Finished when</b> {String(f.finishedWhen)}</div>
  </div>
{/if}
{#if steps.length}
  <Block blockKey="steps" heading="steps" count={steps.length}>
    {#each steps as s, i}
      {@const by = (srv[String(s.id ?? "")] ?? []).map(String)}
      {@const scr = (screens[String(s.id ?? "")] ?? []).map(String)}
      {@const sys = !!s.system}
      <div class="grid grid-cols-[34px_minmax(0,1fr)] gap-3 border-b border-border py-3 last:border-b-0">
        <span class="grid size-7 place-items-center rounded-full border border-border font-mono text-xs text-subtle-foreground">
          {i + 1}
        </span>
        <div class="min-w-0">
          <div class="text-[15px] leading-relaxed">{String(s.statement ?? "")}</div>
          {#if s.actor}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-muted-foreground">
              Actor: <InlineRef id={String(s.actor)} />
            </div>
          {/if}
          {#if s.system}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-muted-foreground">
              System: <InlineRef id={String(s.system)} />
            </div>
          {/if}
          {#if s.failure}
            <div class="mt-2 grid grid-cols-[18px_1fr] gap-1.5 rounded-md bg-rule px-2.5 py-2 text-[13.5px] text-subtle-foreground">
              <Icon name="alert" class="mt-0.5 size-3.5 text-rule-foreground" />
              <span><b>Failure</b> {String(s.failure)}</span>
            </div>
          {/if}
          <div class="mt-2 flex flex-col items-stretch gap-1">
            {#if by.length}
              {#each by as b}
                {@const info = serverLineInfo(base(b))}
                <div class="flex flex-wrap items-center gap-1.5">
                  <Ref id={b} />
                  {#if info}
                    <span class="inline-flex flex-wrap gap-1 text-xs text-subtle-foreground">
                      <FactBadge text={info.built ? "implemented" : "not implemented"} />
                      {#if info.scenarios}
                        <FactBadge text={`${info.proven} of ${info.scenarios} scenarios confirmed`} />
                      {:else}
                        <FactBadge text="no scenarios" />
                      {/if}
                    </span>
                  {/if}
                </div>
              {/each}
            {:else}
              <span class="inline-flex items-center gap-1.5 text-xs text-warn-foreground">
                <Icon name="alert" class="size-3.5" />Nothing serves this step yet
              </span>
            {/if}
            {#if scr.length}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-subtle-foreground">
                <Icon name="screen" class="size-3.5 text-muted-foreground" />
                <span>
                  screen:{" "}
                  {#each scr as x, j}{#if j > 0}, {/if}<InlineRef id={x} />{/each}
                </span>
              </div>
            {:else if !sys && by.length}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-warn-foreground">
                <Icon name="alert" class="size-3.5" /><span>No screen carries this step</span>
              </div>
            {/if}
          </div>
        </div>
      </div>
    {/each}
  </Block>
{/if}
{#if between}
  <Block blockKey="bs" heading="betweenSteps"><Prose html={between} /></Block>
{/if}
