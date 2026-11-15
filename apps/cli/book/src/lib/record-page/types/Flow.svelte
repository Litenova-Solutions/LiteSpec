<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, RecordLine, RecordLink } from "$lib/record";
  import { Wireframe } from "$lib/wireframe";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import { flowBranchSentence, flowFields, reachStateWord, serverLineInfo, stringField } from "../page-values";

  // A flow: when it starts, each step with what serves it, the storyboard of
  // its steps' screens, each branch, and when it finishes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const jf = $derived(flowFields(f));
  const srv: Record<string, string[]> = $derived(r.stepServers ?? {});
  const screens: Record<string, string[]> = $derived(r.stepScreens ?? {});
  // The storyboard: each step's screens in step order, with the wireframe each carries.
  const story = $derived.by(() => {
    const cells: { screen: string; title: string; wire: string | null; n: number; statement: string }[] = [];
    jf.steps.forEach((s, i) => {
      const statement = String(s.statement ?? "");
      for (const x of (screens[String(s.id ?? "")] ?? []).map(String)) {
        const target = book.get(stripStep(x));
        cells.push({
          screen: x,
          title: target?.title ?? stripStep(x),
          wire: stringField(target?.fields, "wireframe") ?? null,
          n: i + 1,
          statement,
        });
      }
    });
    return cells;
  });
  // The records reaching one step, with their claim state, from the step's intent coverage.
  function reachOf(stepId: unknown): { ids: string[]; claims: { id: string; state: "verified" | "claimed" }[] } {
    const want = String(stepId ?? "");
    const hit = (r.intentCoverage?.steps ?? []).find((x) => (x.id ?? "") === want || (x.id ?? "").endsWith("#" + want));
    return { ids: hit?.reachedBy ?? [], claims: hit?.claims ?? [] };
  }
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
  <div class="mb-6 border-l-[3px] border-slate-200 py-0.5 pl-3.5 text-sm">
    <div><b>Started when</b> {jf.startedWhen}</div>
  </div>
{/if}
{#if jf.finishedWhen}
  <div class="mb-6 grid grid-cols-[20px_1fr] gap-2.5 border-l-[3px] border-convention-foreground py-0.5 pl-3.5 text-sm">
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
      {@const reach = reachOf(s.id)}
      <div class="grid grid-cols-[34px_minmax(0,1fr)] gap-3 border-b border-slate-200 py-2.5 last:border-b-0">
        <span class="grid size-7 place-items-center rounded-sm border border-slate-200 font-mono text-xs text-slate-600">
          {i + 1}
        </span>
        <div class="min-w-0">
          <div class="text-sm leading-relaxed">{String(s.statement ?? "")}</div>
          {#if reach.ids.length}
            <div class="mt-1 text-xs text-slate-600">Reached by {#each reach.ids as rid, j (rid)}{#if j > 0}, {/if}{rid}{#if reachStateWord(reach.claims, rid)} ({reachStateWord(reach.claims, rid)}){/if}{/each}</div>
          {/if}
          {#if s.actor}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-slate-600">
              Actor: <RecordLink id={String(s.actor)} />
            </div>
          {/if}
          {#if s.system}
            <div class="mt-1 inline-flex items-center gap-1.5 text-xs text-slate-600">
              System: <RecordLink id={String(s.system)} />
            </div>
          {/if}
          {#if s.failure}
            <div class="mt-2 grid grid-cols-[18px_1fr] gap-1.5 rounded-md bg-rule px-2.5 py-2 text-sm text-slate-600">
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
                    <span class="inline-flex flex-wrap gap-1 text-xs text-slate-600">
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
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-slate-600">
                <RecordIcon name="screen" class="size-3.5 text-slate-600" />
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
{#if story.length}
  <PageSection blockKey="story" heading="Storyboard" count={story.length}>
    {#each story as cell, i (cell.screen + "#" + i)}
      <div class="mt-6 first:mt-0">
        <div class="mb-2 text-sm font-semibold text-slate-600">Step {cell.n} · {cell.statement}</div>
        <Wireframe text={cell.wire} title={cell.title} />
      </div>
    {/each}
  </PageSection>
{/if}
{#if jf.branches.length}
  <PageSection blockKey="branches" heading="branches" count={jf.branches.length}>
    {#each jf.branches as b, i (i)}
      <div class="border-b border-slate-200 py-2.5 text-sm leading-relaxed last:border-b-0">{flowBranchSentence(jf.steps, b)}</div>
    {/each}
  </PageSection>
{/if}
