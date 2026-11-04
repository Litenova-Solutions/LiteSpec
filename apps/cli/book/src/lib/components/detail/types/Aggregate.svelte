<script lang="ts">
  import { book, cap, uniq } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import Block from "../blocks/Block.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import Jump from "../blocks/Jump.svelte";
  import Refs from "../blocks/Refs.svelte";
  import Table from "../blocks/Table.svelte";
  import DrivenBy from "../shared/DrivenBy.svelte";
  import {
    clsBadgeHtml, codeCls, compensationHtml, inlineRefHtml, periodSuffixHtml, refusalOf, tdHtml,
  } from "../helpers";

  // An aggregate: its states, the transitions between them, its saga, its data,
  // and the rules and events that belong to it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const states = $derived(((f.states as Fields[] | undefined) ?? []) as Fields[]);
  const tr = $derived(((f.transitions as Fields[] | undefined) ?? []) as Fields[]);
  const reach = $derived(((r.reachable as string[] | undefined) ?? []).map(String));
  const starts = $derived(new Set(tr.filter((t) => t.from === "-").map((t) => String(t.to))));
  const froms = $derived(uniq(tr.map((t) => String(t.from))));
  const steps = $derived(((f.steps as Fields[] | undefined) ?? []) as Fields[]);
  const exits = $derived(((f.exits as unknown[] | undefined) ?? []) as (string | Fields)[]);
  const classes = $derived(((f.dataClasses as Fields[] | undefined) ?? []) as Fields[]);
  const data = $derived(((f.data as Fields[] | undefined) ?? []) as Fields[]);
  const rules = $derived(book.rulesOf(id).filter((x) => book.get(x)));
  const evs = $derived(book.eventsOf(id));

  const classRows = $derived(
    classes.map((c) => [
      tdHtml(`<b>${esc(cap(String(c.name ?? "").replace(/-/g, " ")))}</b><div class="mt-1">${clsBadgeHtml(c.classification)}</div>`),
      tdHtml(`<div class="flex flex-wrap gap-1">${((c.fields as string[] | undefined) ?? []).map((x) => `<span class="${codeCls}">${esc(x)}</span>`).join("")}</div>`),
      tdHtml(esc(c.purpose ?? "")),
      tdHtml(
        `<div class="text-xs"><b>${esc(cap(String(c.onErasure ?? "")))}</b>${periodSuffixHtml(c.period)}</div>` +
          (c.basis || c.reason ? `<div class="mt-0.5 text-[11px] text-muted-foreground">${esc((c.basis ?? c.reason) as string)}</div>` : ""),
      ),
    ]),
  );

  const ruleRows = $derived(
    rules.map((x) => {
      const q = book.get(x);
      const qf = q?.fields ?? {};
      const rf = refusalOf(x);
      return [
        tdHtml(inlineRefHtml(x) + `<div class="mt-0.5 text-[13px] text-muted-foreground">${esc(qf.statement ?? "")}</div>`),
        tdHtml(`<span class="font-medium text-foreground">${esc(cap(String(qf.kind ?? "")))}</span>`),
        tdHtml(
          rf.info?.code
            ? `<span class="${codeCls}">${esc(rf.info.code)}</span>`
            : rf.id
              ? inlineRefHtml(rf.id)
              : '<span class="font-mono text-[11px] text-muted-foreground">none</span>',
        ),
      ];
    }),
  );
</script>

{#if f.finishedWhen}
  <div class="mb-6 grid grid-cols-[20px_1fr] gap-2.5 border-l-[3px] border-convention-foreground py-0.5 pl-3.5 text-[14.5px]">
    <Icon name="passing" class="mt-1 size-3.5 text-convention-foreground" />
    <div><b>Finished when</b> {String(f.finishedWhen)}</div>
  </div>
{/if}
<Jump
  items={[
    { key: "states", label: "states", count: states.length },
    { key: "flow", label: "transitions", count: tr.length },
    { key: "steps", label: "steps", count: steps.length },
    { key: "classes", label: "dataClasses", count: classes.length },
    { key: "data", label: "data", count: data.length },
    { key: "rules", label: "Rules", count: rules.length },
    { key: "events", label: "Events", count: evs.length },
  ]}
/>
{#if states.length}
  <Block blockKey="states" heading="states" count={states.length}>
    <div class="border-t border-border">
      {#each states as s}
        {@const created = starts.has(String(s.name))}
        {@const unreached = reach.length > 0 && !reach.includes(String(s.name))}
        <div class="grid grid-cols-[180px_minmax(0,1fr)] gap-x-4 gap-y-1 border-b border-border py-2.5">
          <div class="flex items-start gap-2 text-sm font-semibold">
            {String(s.name ?? "")}
            {#if created}
              <FactBadge text="created as" />
            {:else if unreached}
              <FactBadge text="not reachable" />
            {/if}
          </div>
          <p class="m-0 text-[13.5px] leading-snug text-subtle-foreground">{String(s.meaning ?? "")}</p>
        </div>
      {/each}
    </div>
  </Block>
{/if}
{#if tr.length}
  <Block blockKey="flow" heading="transitions" count={tr.length} aside="what moves the record from state to state">
    <div class="grid gap-1.5">
      {#each froms as fr}
        {@const tos = uniq(tr.filter((t) => String(t.from) === fr).map((t) => String(t.to)))}
        <div class="grid grid-cols-[150px_minmax(0,1fr)] items-start gap-2.5 border-b border-border py-2.5 last:border-b-0">
          <div class="pt-1 text-sm {fr === '-' ? 'font-medium text-muted-foreground' : 'font-semibold'}">
            {fr === "-" ? "Created as" : fr}
          </div>
          <div>
            {#each tos as to}
              <div class="grid grid-cols-[18px_150px_minmax(0,1fr)] items-baseline gap-2 py-1 text-[13.5px]">
                <span class="text-muted-foreground"><Icon name="arrow" class="size-3.5" /></span>
                <span class="font-medium">{to}</span>
                <span class="flex flex-wrap gap-x-3.5 gap-y-1">
                  {#each tr.filter((t) => String(t.from) === fr && String(t.to) === to) as t}
                    {#if t.by}
                      <InlineRef id={String(t.by)} />
                    {:else}
                      <span class="font-mono text-[11px] text-muted-foreground">no use case named</span>
                    {/if}
                  {/each}
                </span>
              </div>
            {/each}
          </div>
        </div>
      {/each}
    </div>
  </Block>
{/if}
{#if steps.length}
  <Block blockKey="steps" heading="steps" count={steps.length}>
    <ol class="m-0 list-none p-0">
      {#each steps as s, i}
        {@const label = String(s.id ?? s.name ?? "")}
        <li class="relative grid grid-cols-[34px_minmax(0,1fr)_auto] items-center gap-3 py-2">
          {#if i > 0}<span class="absolute top-[-10px] left-[13px] h-[22px] w-px bg-border" aria-hidden="true"></span>{/if}
          <span class="relative z-[1] grid size-[26px] place-items-center rounded-full border border-border bg-background font-mono text-xs font-medium text-subtle-foreground">
            {i + 1}
          </span>
          <div class="min-w-0">
            <b>{cap(label.replace(/-/g, " "))}</b>
            {#if s.runs}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-muted-foreground">Runs <InlineRef id={String(s.runs)} /></div>
            {/if}
            {#if s.guard}
              <div class="mt-0.5 flex items-center gap-1.5 text-xs text-muted-foreground">Guard {String(s.guard)}</div>
            {/if}
            <div class="mt-0.5 flex items-center gap-1.5 text-xs text-muted-foreground">
              Compensation: {@html compensationHtml(s.compensation as Fields | undefined)}
            </div>
          </div>
          <span></span>
        </li>
      {/each}
    </ol>
    {#if exits.length}
      <Table
        head={["Exit"]}
        rows={exits.map((x) => [tdHtml(esc(cap(String(typeof x === "string" ? x : (x.name ?? "")))), "nm")])}
      />
    {/if}
  </Block>
{/if}
<DrivenBy f={f} />
{#if classes.length}
  <Block blockKey="classes" heading="dataClasses" count={classes.length} aside="what is kept, and what happens on erasure">
    <Table head={["Data class", "Fields", "Purpose", "Erasure"]} rows={classRows} />
  </Block>
{/if}
{#if data.length}
  <Block blockKey="data" heading="data" count={data.length}>
    <Table
      head={["Name", "Meaning", "Classification"]}
      rows={data.map((d) => [tdHtml(esc(String(d.name ?? "")), "nm"), tdHtml(esc(String(d.meaning ?? ""))), tdHtml(clsBadgeHtml(d.classification))])}
    />
  </Block>
{/if}
{#if rules.length}
  <Block blockKey="rules" heading="Rules" count={rules.length}>
    <Table head={["Rule", "Kind", "Refusal"]} rows={ruleRows} />
  </Block>
{/if}
{#if evs.length}
  <Block blockKey="events" heading="Events" count={evs.length}><Refs ids={evs} /></Block>
{/if}
