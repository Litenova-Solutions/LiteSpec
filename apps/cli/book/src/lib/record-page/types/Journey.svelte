<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, RecordLine, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { journeyBranchSentence, journeyFields, serverLineInfo } from "../page-values";

  // A journey: when it starts, each step with what serves it, each branch, and when it finishes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const jf = $derived(journeyFields(f));
  const srv: Record<string, string[]> = $derived(r.stepServers ?? {});
  const screens: Record<string, string[]> = $derived(r.stepScreens ?? {});
  const serverInfo = $derived.by(() => {
    const out: Record<string, { built: boolean; scenarios: number; verified: number } | null> = {};
    for (const s of jf.steps) {
      for (const b of (srv[String(s.id ?? "")] ?? []).map(String)) {
        if (b in out) continue;
        const q = book.get(stripStep(b));
        const states = book.scenariosOf(stripStep(b)).filter((x) => book.get(x)).map((x) => book.get(x)?.proof?.state);
        out[b] = serverLineInfo(q?.machine, states);
      }
    }
    return out;
  });
</script>

{#if jf.startedWhen}
  <div class="mb-6 border-l-[3px] border-border py-0.5 pl-3.5 text-[0.90625rem]">
    <div><b>Started when</b> {jf.startedWhen}</div>
  </div>
{/if}
{#if jf.finishedWhen}
  <div class="mb-6 grid grid-cols-[20px_1fr] gap-2.5 border-l-[3px] border-convention-foreground py-0.5 pl-3.5 text-[0.90625rem]">
    <RecordIcon name="passing" class="mt-1 size-3.5 text-convention-foreground" />
    <div><b>Finished when</b> {jf.finishedWhen}</div>
  </div>
{/if}
{#if jf.steps.length}
  <PageSection blockKey="steps" heading="steps" count={jf.steps.length}>
    {#each jf.steps as s, i (s.id ?? i)}
      {@const by = (srv[String(s.id ?? "")] ?? []).map(String)}
      {@const scr = (screens[String(s.id ?? "")] ?? []).map(String)}
      {@const sys = !!s.system}
      <div class="grid grid-cols-[34px_minmax(0,1fr)] gap-3 border-b border-border py-3 last:border-b-0">
        <span class="grid size-7 place-items-center rounded-full border border-border font-mono text-xs text-subtle-foreground">
          {i + 1}
        </span>
        <div class="min-w-0">
          <div class="text-[0.9375rem] leading-relaxed">{String(s.statement ?? "")}</div>
          {#if s.actor}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-muted-foreground">
              Actor: <RecordLink id={String(s.actor)} />
            </div>
          {/if}
          {#if s.system}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-muted-foreground">
              System: <RecordLink id={String(s.system)} />
            </div>
          {/if}
          {#if s.failure}
            <div class="mt-2 grid grid-cols-[18px_1fr] gap-1.5 rounded-md bg-rule px-2.5 py-2 text-[0.84375rem] text-subtle-foreground">
              <RecordIcon name="alert" class="mt-0.5 size-3.5 text-rule-foreground" />
              <span><b>Failure</b> {String(s.failure)}</span>
            </div>
          {/if}
          <div class="mt-2 flex flex-col items-stretch gap-1">
            {#if by.length}
              {#each by as b (b)}
                {@const info = serverInfo[b] ?? null}
                <div class="flex flex-wrap items-center gap-1.5">
                  <RecordLine id={b} />
                  {#if info}
                    <span class="inline-flex flex-wrap gap-1 text-xs text-subtle-foreground">
                      <Fact text={info.built ? "coded" : "not coded"} />
                      {#if info.scenarios}
                        <Fact text={`${info.verified} of ${info.scenarios} scenarios confirmed`} />
                      {:else}
                        <Fact text="no scenarios" />
                      {/if}
                    </span>
                  {/if}
                </div>
              {/each}
            {:else}
              <span class="inline-flex items-center gap-1.5 text-xs text-warn-foreground">
                <RecordIcon name="alert" class="size-3.5" />Nothing serves this step yet
              </span>
            {/if}
            {#if scr.length}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-subtle-foreground">
                <RecordIcon name="screen" class="size-3.5 text-muted-foreground" />
                <span>
                  screen:{" "}
                  {#each scr as x, j (x)}{#if j > 0}, {/if}<RecordLink id={x} />{/each}
                </span>
              </div>
            {:else if !sys && by.length}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-warn-foreground">
                <RecordIcon name="alert" class="size-3.5" /><span>No screen carries this step</span>
              </div>
            {/if}
          </div>
        </div>
      </div>
    {/each}
  </PageSection>
{/if}
{#if jf.branches.length}
  <PageSection blockKey="branches" heading="branches" count={jf.branches.length}>
    {#each jf.branches as b, i (i)}
      <div class="border-b border-border py-3 text-[0.9375rem] leading-relaxed last:border-b-0">{journeyBranchSentence(jf.steps, b)}</div>
    {/each}
  </PageSection>
{/if}
