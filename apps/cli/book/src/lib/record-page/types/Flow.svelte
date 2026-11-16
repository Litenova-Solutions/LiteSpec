<script lang="ts">
  import { book, hoverState, stripStep } from "$lib/frame";
  import type { BookRecord, ReachClaim, RecordFields } from "$lib/frame";
  import { Hover, RecordIcon, RecordLink, TypeTag } from "$lib/record";
  import { Wireframe } from "$lib/wireframe";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { flowFields, serverLineInfo, stringField } from "../page-values";

  // A flow: when it starts, each step with what serves it and the branches it
  // leaves from, the storyboard of its steps' screens, and when it finishes.
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
  // Each step's position by its id, so a branch names the step a reader sees.
  const positionOf = $derived.by(() => {
    const at = new Map<string, number>();
    jf.steps.forEach((s, i) => {
      const key = String(s.id ?? "");
      if (key && !at.has(key)) at.set(key, i + 1);
    });
    return (id: string): number => at.get(id) ?? 0;
  });
  // The branches leaving each step, and the steps each step is also reached from, by position.
  const branchesFrom = $derived.by(() => {
    const out = new Map<number, RecordFields[]>();
    for (const b of jf.branches) {
      const from = positionOf(String(b.step ?? ""));
      if (!from) continue;
      const list = out.get(from) ?? [];
      list.push(b);
      out.set(from, list);
    }
    return out;
  });
  const joinedTo = $derived.by(() => {
    const out = new Map<number, number[]>();
    for (const b of jf.branches) {
      const from = positionOf(String(b.step ?? ""));
      const to = positionOf(String(b.nextStep ?? ""));
      if (!from || !to) continue;
      const list = out.get(to) ?? [];
      list.push(from);
      out.set(to, list);
    }
    return out;
  });
  // Whether every record reaching a step stands verified.
  function reachVerified(claims: ReachClaim[], ids: string[]): boolean {
    return ids.length > 0 && ids.every((id) => claims.some((claim) => claim.id === id && claim.state === "verified"));
  }
</script>

{#if jf.startedWhen}
  <Note kind="info" icon="info"><b>{book.fieldLabel("startedWhen")}</b> {jf.startedWhen}</Note>
{/if}
{#if jf.finishedWhen}
  <Note kind="good" icon="passing"><b>{book.fieldLabel("finishedWhen")}</b> {jf.finishedWhen}</Note>
{/if}
{#if jf.steps.length}
  <PageSection blockKey="steps" heading="steps" count={jf.steps.length}>
    <ol class="m-0 list-none p-0">
      {#each jf.steps as s, i (s.id ?? i)}
        {@const by = (srv[String(s.id ?? "")] ?? []).map(String)}
        {@const scr = (screens[String(s.id ?? "")] ?? []).map(String)}
        {@const sys = !!s.system}
        {@const reach = reachOf(s.id)}
        {@const joined = joinedTo.get(i + 1) ?? []}
        {@const leaving = branchesFrom.get(i + 1) ?? []}
        {@const scrType = scr.length ? book.typeOf(stripStep(scr[0] ?? "")) : ""}
        <li id={"b-step-" + (i + 1)} class="relative grid scroll-mt-20 grid-cols-[32px_minmax(0,1fr)] gap-4 pb-6 last:pb-0">
          {#if i < jf.steps.length - 1}
            <span aria-hidden="true" class="absolute top-7 bottom-1 left-[11px] w-px bg-border-subtle"></span>
          {/if}
          <span aria-hidden="true" class="grid size-6 place-items-center rounded-full text-xs ring-1 ring-interactive ring-inset text-interactive">
            {i + 1}
          </span>
          <div class="min-w-0 text-base">
            <div>{String(s.statement ?? "")}</div>
            {#each joined as from, k (k)}
              <div class="mt-2 flex items-center gap-1 text-xs tracking-[0.32px] text-text-secondary">
                <RecordIcon name="branch" class="size-4" />Also reached from step {from}
              </div>
            {/each}
            <div class="mt-2 grid gap-1 text-sm">
              {#if s.actor}
                <div class="flex flex-wrap items-center gap-2">
                  <span class="text-text-secondary">{book.fieldLabel("actor")}:</span>
                  <RecordLink id={String(s.actor)} />
                </div>
              {/if}
              {#if s.system}
                <div class="flex flex-wrap items-center gap-2">
                  <span class="text-text-secondary">{book.fieldLabel("system")}:</span>
                  <RecordLink id={String(s.system)} />
                </div>
              {/if}
              {#if by.length}
                {#each by as b (b)}
                  {@const info = serverInfo[b] ?? null}
                  <div class="flex flex-wrap items-center gap-3">
                    <TypeTag type={book.typeOf(stripStep(b))} />
                    <RecordLink id={b} />
                    {#if info}
                      {#if info.built}
                        <Hover hover={hoverState(book, "coded")}>
                          <span
                            data-explain="coded"
                            class="inline-flex h-[18px] items-center gap-1 rounded-full bg-tag-green px-2 text-xs tracking-[0.32px] text-tag-green-foreground"
                          >
                            Coded
                          </span>
                        </Hover>
                      {:else}
                        <span
                          class="inline-flex h-[18px] items-center gap-1 rounded-full bg-tag-warm px-2 text-xs tracking-[0.32px] text-tag-warm-foreground"
                        >
                          Not coded
                        </span>
                      {/if}
                      <span>{info.scenarios ? `${info.verified} of ${info.scenarios} scenarios confirmed` : "no scenarios"}</span>
                    {/if}
                  </div>
                {/each}
              {:else}
                <div class="flex flex-wrap items-center gap-2 text-text-secondary">
                  <RecordIcon name="alert" class="size-4 text-warning-ink" />
                  <span>Nothing serves this step yet</span>
                </div>
              {/if}
              {#if scr.length}
                <div class="flex flex-wrap items-center gap-3">
                  <TypeTag type={scrType} />
                  <span>
                    {#each scr as x, j (x)}{#if j > 0}{", "}{/if}<RecordLink id={x} />{/each}
                  </span>
                </div>
              {:else if !sys && by.length}
                <div class="flex flex-wrap items-center gap-2"><span>No screen carries this step</span></div>
              {/if}
              {#if reach.ids.length}
                <div class="flex flex-wrap items-center gap-2">
                  {#if reachVerified(reach.claims, reach.ids)}
                    <Hover hover={hoverState(book, "reach-verified")}>
                      <span
                        data-explain="reach-verified"
                        class="inline-flex h-[18px] items-center gap-1 rounded-full bg-tag-teal px-2 text-xs tracking-[0.32px] text-tag-teal-foreground"
                        >Reach verified</span
                      >
                    </Hover>
                  {:else}
                    <Hover hover={hoverState(book, "reach-claimed")}>
                      <span
                        data-explain="reach-claimed"
                        class="inline-flex h-[18px] items-center gap-1 rounded-full bg-tag-warm px-2 text-xs tracking-[0.32px] text-tag-warm-foreground"
                        >Reach claimed</span
                      >
                    </Hover>
                  {/if}
                </div>
              {/if}
              {#if s.failure}
                <Note kind="warn" icon="alert"><b>{book.fieldLabel("failure")}</b> {String(s.failure)}</Note>
              {/if}
            </div>
            {#each leaving as b, k (k)}
              {@const next = String(b.nextStep ?? "")}
              {@const target = next === "end" ? 0 : positionOf(next)}
              <div class="mt-3 grid grid-cols-[16px_minmax(0,1fr)] gap-2 bg-layer px-3 py-2 text-sm">
                <RecordIcon name="branch" class="mt-0.5 size-4 text-text-secondary" />
                <div>
                  <b>Branch</b>{" "}when {String(b.condition ?? "")}:{" "}
                  {#if next === "end"}the flow ends{:else if target}<a href={"#b-step-" + target} class="text-link hover:underline">continues at step {target}</a>{:else}continues at step {next}{/if}
                </div>
              </div>
            {/each}
          </div>
        </li>
      {/each}
    </ol>
  </PageSection>
{/if}
{#if story.length}
  <PageSection blockKey="story" heading="Storyboard" count={story.length}>
    {#each story as cell, i (cell.screen + "#" + i)}
      <div class="mt-6 first:mt-0">
        <h3 class="mb-2 text-sm font-semibold">Step {cell.n} · {cell.statement}</h3>
        <Wireframe text={cell.wire} title={cell.title} />
      </div>
    {/each}
  </PageSection>
{/if}
