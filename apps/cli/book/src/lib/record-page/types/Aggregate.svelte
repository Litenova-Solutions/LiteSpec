<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import SectionJump from "../blocks/SectionJump.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { aggregateFields, refusalOf, stringField } from "../page-values";
  import { aggregateModel } from "./aggregate-model";

  // An aggregate: its states, the transitions between them, its data,
  // and the rules and events that belong to it.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const af = $derived(aggregateFields(f));
  const ruleInputs = $derived(
    book.rulesOf(id).filter((x) => book.get(x)).map((x) => {
      const qf = book.get(x)?.fields ?? {};
      const rid = book.outs(stripStep(x), "refusal")[0] ?? null;
      const rf = refusalOf(rid ? book.get(rid) : null, rid);
      return {
        id: x,
        statement: stringField(qf, "statement") ?? "",
        kind: stringField(qf, "kind") ?? "",
        refusalCode: rf.info?.code ?? null,
        refusalId: rf.id,
      };
    }),
  );
  const model = $derived(aggregateModel(book, f, r, ruleInputs));
  const evs = $derived(book.eventsOf(id));
</script>

{#if af.finishedWhen}
  <div class="mb-6 grid grid-cols-[20px_1fr] gap-2.5 border-l-[3px] border-convention-foreground py-0.5 pl-3.5 text-[0.90625rem]">
    <RecordIcon name="passing" class="mt-1 size-3.5 text-convention-foreground" />
    <div><b>Finished when</b> {af.finishedWhen}</div>
  </div>
{/if}
<SectionJump
  items={[
    { key: "states", label: "states", count: model.states.length },
    { key: "flow", label: "transitions", count: af.transitions.length },
    { key: "classes", label: "dataClasses", count: af.dataClasses.length },
    { key: "data", label: "data", count: af.data.length },
    { key: "rules", label: "Rules", count: ruleInputs.length },
    { key: "events", label: "Events", count: evs.length },
  ]}
/>
{#if model.states.length}
  <PageSection blockKey="states" heading="states" count={model.states.length}>
    <div class="border-t border-border">
      {#each model.states as s (s.name)}
        <div class="grid grid-cols-[180px_minmax(0,1fr)] gap-x-4 gap-y-1 border-b border-border py-2.5">
          <div class="flex items-start gap-2 text-sm font-semibold">
            {s.name}
            {#if s.created}
              <Fact text="created as" />
            {:else if s.unreached}
              <Fact text="not reachable" />
            {/if}
          </div>
          <p class="m-0 text-[0.84375rem] leading-snug text-subtle-foreground">{s.meaning}</p>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
{#if model.flows.length}
  <PageSection blockKey="flow" heading="transitions" count={af.transitions.length} aside="what moves the record from state to state">
    <div class="grid gap-1.5">
      {#each model.flows as flow (flow.from)}
        <div class="grid grid-cols-[150px_minmax(0,1fr)] items-start gap-2.5 border-b border-border py-2.5 last:border-b-0">
          <div class="pt-1 text-sm {flow.created ? 'font-medium text-muted-foreground' : 'font-semibold'}">
            {flow.created ? "Created as" : flow.from}
          </div>
          <div>
            {#each flow.targets as t (t.to)}
              <div class="grid grid-cols-[18px_150px_minmax(0,1fr)] items-baseline gap-2 py-1 text-[0.84375rem]">
                <span class="text-muted-foreground"><RecordIcon name="arrow" class="size-3.5" /></span>
                <span class="font-medium">{t.to}</span>
                <span class="flex flex-wrap gap-x-3.5 gap-y-1">
                  {#each t.by as b, j (j)}
                    {#if b}
                      <RecordLink id={b} />
                    {:else}
                      <span class="font-mono text-[0.6875rem] text-muted-foreground">no use case named</span>
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
{#if af.dataClasses.length}
  <PageSection blockKey="classes" heading="dataClasses" count={af.dataClasses.length} aside="what is kept, and what happens on erasure">
    <DataTable head={["Data class", "Fields", "Purpose", "Erasure"]} rows={model.classRows} />
  </PageSection>
{/if}
{#if af.data.length}
  <PageSection blockKey="data" heading="data" count={af.data.length}>
    <DataTable head={["Name", "Meaning", "Classification"]} rows={model.dataRows} />
  </PageSection>
{/if}
{#if ruleInputs.length}
  <PageSection blockKey="rules" heading="Rules" count={ruleInputs.length}>
    <DataTable head={["Rule", "Kind", "Refusal"]} rows={model.ruleRows} />
  </PageSection>
{/if}
{#if evs.length}
  <PageSection blockKey="events" heading="Events" count={evs.length}><RecordList ids={evs} /></PageSection>
{/if}
