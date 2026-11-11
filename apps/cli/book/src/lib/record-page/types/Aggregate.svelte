<script lang="ts">
  import { book, capitalize, dedupe, esc, formatLabel } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import SectionJump from "../blocks/SectionJump.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../page-html";
  import { clsBadgeHtml, periodSuffixHtml, refusalOf } from "../page-values";

  // An aggregate: its states, the transitions between them, its data,
  // and the rules and events that belong to it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const states = $derived(((f.states as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const tr = $derived(((f.transitions as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const reach = $derived(((r.reachable as string[] | undefined) ?? []).map(String));
  const starts = $derived(new Set(tr.filter((t) => t.from === "-").map((t) => String(t.to))));
  const froms = $derived(dedupe(tr.map((t) => String(t.from))));
  const classes = $derived(((f.dataClasses as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const data = $derived(((f.data as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const rules = $derived(book.rulesOf(id).filter((x) => book.get(x)));
  const evs = $derived(book.eventsOf(id));

  const classRows = $derived(
    classes.map((c) => [
      tdHtml(`<b>${esc(capitalize(formatLabel(c.name)))}</b><div class="mt-1">${clsBadgeHtml(c.classification)}</div>`),
      tdHtml(`<div class="flex flex-wrap gap-1">${((c.fields as string[] | undefined) ?? []).map((x) => `<span class="${codeCls}">${esc(x)}</span>`).join("")}</div>`),
      tdHtml(esc(c.purpose ?? "")),
      tdHtml(
        `<div class="text-xs"><b>${esc(capitalize(String(c.onErasure ?? "")))}</b>${periodSuffixHtml(c.period)}</div>` +
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
        tdHtml(`<span class="font-medium text-foreground">${esc(capitalize(String(qf.kind ?? "")))}</span>`),
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
    <RecordIcon name="passing" class="mt-1 size-3.5 text-convention-foreground" />
    <div><b>Finished when</b> {String(f.finishedWhen)}</div>
  </div>
{/if}
<SectionJump
  items={[
    { key: "states", label: "states", count: states.length },
    { key: "flow", label: "transitions", count: tr.length },
    { key: "classes", label: "dataClasses", count: classes.length },
    { key: "data", label: "data", count: data.length },
    { key: "rules", label: "Rules", count: rules.length },
    { key: "events", label: "Events", count: evs.length },
  ]}
/>
{#if states.length}
  <PageSection blockKey="states" heading="states" count={states.length}>
    <div class="border-t border-border">
      {#each states as s}
        {@const created = starts.has(String(s.name))}
        {@const unreached = reach.length > 0 && !reach.includes(String(s.name))}
        <div class="grid grid-cols-[180px_minmax(0,1fr)] gap-x-4 gap-y-1 border-b border-border py-2.5">
          <div class="flex items-start gap-2 text-sm font-semibold">
            {String(s.name ?? "")}
            {#if created}
              <Fact text="created as" />
            {:else if unreached}
              <Fact text="not reachable" />
            {/if}
          </div>
          <p class="m-0 text-[13.5px] leading-snug text-subtle-foreground">{String(s.meaning ?? "")}</p>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if tr.length}
  <PageSection blockKey="flow" heading="transitions" count={tr.length} aside="what moves the record from state to state">
    <div class="grid gap-1.5">
      {#each froms as fr}
        {@const tos = dedupe(tr.filter((t) => String(t.from) === fr).map((t) => String(t.to)))}
        <div class="grid grid-cols-[150px_minmax(0,1fr)] items-start gap-2.5 border-b border-border py-2.5 last:border-b-0">
          <div class="pt-1 text-sm {fr === '-' ? 'font-medium text-muted-foreground' : 'font-semibold'}">
            {fr === "-" ? "Created as" : fr}
          </div>
          <div>
            {#each tos as to}
              <div class="grid grid-cols-[18px_150px_minmax(0,1fr)] items-baseline gap-2 py-1 text-[13.5px]">
                <span class="text-muted-foreground"><RecordIcon name="arrow" class="size-3.5" /></span>
                <span class="font-medium">{to}</span>
                <span class="flex flex-wrap gap-x-3.5 gap-y-1">
                  {#each tr.filter((t) => String(t.from) === fr && String(t.to) === to) as t}
                    {#if t.by}
                      <RecordLink id={String(t.by)} />
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
  </PageSection>
{/if}
{#if classes.length}
  <PageSection blockKey="classes" heading="dataClasses" count={classes.length} aside="what is kept, and what happens on erasure">
    <DataTable head={["Data class", "Fields", "Purpose", "Erasure"]} rows={classRows} />
  </PageSection>
{/if}
{#if data.length}
  <PageSection blockKey="data" heading="data" count={data.length}>
    <DataTable
      head={["Name", "Meaning", "Classification"]}
      rows={data.map((d) => [tdHtml(esc(String(d.name ?? "")), "nm"), tdHtml(esc(String(d.meaning ?? ""))), tdHtml(clsBadgeHtml(d.classification))])}
    />
  </PageSection>
{/if}
{#if rules.length}
  <PageSection blockKey="rules" heading="Rules" count={rules.length}>
    <DataTable head={["Rule", "Kind", "Refusal"]} rows={ruleRows} />
  </PageSection>
{/if}
{#if evs.length}
  <PageSection blockKey="events" heading="Events" count={evs.length}><RecordList ids={evs} /></PageSection>
{/if}
