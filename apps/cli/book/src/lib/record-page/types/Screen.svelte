<script lang="ts">
  import { book, esc, inline, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { codeOf, mutedInline, refLink, td, thRow } from "../shared/record-cells";
  import { objectList, routeFileOf, screenFields, stringField, stringList } from "../page-values";

  // A screen: the job the person does, where it lives, its states, the use cases it offers, and what comes next.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const job = $derived(stringField(f, "job") ?? "");
  const jobHtml = $derived(job ? inline(book, job, r.src ?? null) : "");
  const states = $derived(objectList(f, "states"));
  const archetype = $derived(stringField(f, "archetype") ?? "");
  const sf = $derived(screenFields(f));
  const off = $derived(sf.offers.map((x) => stripStep(x)));
  const calls = $derived(r.calls ?? []);
  const routePath = $derived(routeFileOf(r.route));
  const leads = $derived(stringList(f, "leadsTo").map((x) => stripStep(x)));
  const fulfills = $derived(stringList(f, "fulfills").map((x) => stripStep(x)));
  const route = $derived(sf.route ?? sf.host ?? "");
  function callsCell(u: string): string {
    if (!r.route) return td(mutedInline("no page"));
    return td(calls.includes(u) ? "Yes" : `<span class="text-support-error">No</span>`);
  }

  function codedCell(u: string): string {
    const q = book.get(stripStep(u));
    if (!q?.machine) return td("");
    return td(q.machine.coded ? "Yes" : `<span class="text-support-error">No</span>`);
  }

  function scenariosCell(u: string): string {
    const q = book.get(stripStep(u));
    if (!q?.machine) return td("");
    const sc = book.scenariosOf(u);
    if (!sc.length) return td(`<span class="text-support-error">None</span>`);
    const verified = sc.filter((x) => book.get(stripStep(x))?.proof?.state === "awaiting").length;
    return td(`${verified} of ${sc.length}`);
  }
</script>

{#if job}
  <PageSection blockKey="job" heading="job"><p class="m-0 text-sm">{@html jobHtml}</p></PageSection>
{/if}
<PageSection blockKey="where" heading="route">
  <DefinitionList>
    <DefinitionRow label="route" html={codeOf(route)} />
    {#if sf.entryPoint}
      <DefinitionRow label="entryPoint"><RecordLink id={stripStep(sf.entryPoint)} /></DefinitionRow>
    {/if}
    {#if archetype}
      <DefinitionRow label="archetype">{archetype}</DefinitionRow>
    {/if}
    <DefinitionRow label="Route">
      {#if r.route}
        <SourceLink src={{ path: routePath ?? "", line: 1 }} />
      {:else}
        <span class="text-support-error">No page carries this screen.</span>
      {/if}
    </DefinitionRow>
  </DefinitionList>
</PageSection>
{#if states.length}
  <PageSection blockKey="states" heading="screen.states" count={states.length}>
    <DataTable
      head={["State", "Shows"]}
      caption={`States of ${r.title}`}
      rows={states.map((s) => [thRow(esc(String(s.name ?? ""))), td(esc(String(s.shows ?? "")))])}
    />
  </PageSection>
{/if}
{#if off.length}
  <PageSection blockKey="offers" heading="offers" count={off.length}>
    <DataTable
      head={["Use case", "Calls", "Coded", "Scenarios"]}
      caption={`Use cases the ${r.title} screen offers`}
      rows={off.map((u) => [thRow(refLink(book, u)), callsCell(u), codedCell(u), scenariosCell(u)])}
    />
  </PageSection>
{/if}
{#if fulfills.length}
  <PageSection blockKey="reqs" heading="Requirements" count={fulfills.length}>
    <RecordList ids={fulfills} />
  </PageSection>
{/if}
{#if leads.length}
  <PageSection blockKey="leads" heading="leadsTo" count={leads.length}>
    <RecordList ids={leads} />
  </PageSection>
{/if}
