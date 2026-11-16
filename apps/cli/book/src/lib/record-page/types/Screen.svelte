<script lang="ts">
  import { book, esc, inline, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import { Wireframe } from "$lib/wireframe";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { codeOf, mutedInline, refLink, td, thRow } from "../shared/record-cells";
  import { objectField, objectList, routeFileOf, screenFields, stringField, stringList } from "../page-values";

  // A screen: the task the person completes, where it lives, its states, its wireframe, the use cases it offers, and what comes next.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const task = $derived(stringField(f, "task") ?? "");
  const taskHtml = $derived(task ? inline(book, task, r.src ?? null) : "");
  const states = $derived(objectList(f, "states"));
  const sf = $derived(screenFields(f));
  const off = $derived(sf.offers.map((x) => stripStep(x)));
  const calls = $derived(r.calls ?? []);
  const routePath = $derived(routeFileOf(r.route));
  const frame = $derived(objectField(f, "pageFrame"));
  const pattern = $derived(stringField(frame, "pattern"));
  const option = $derived(stringField(frame, "option"));
  const wire = $derived(stringField(f, "wireframe"));
  const leads = $derived(stringList(f, "leadsTo").map((x) => stripStep(x)));
  const fulfills = $derived(stringList(f, "fulfills").map((x) => stripStep(x)));
  const route = $derived(sf.route ?? sf.host ?? "");
  const pageFrameHtml = $derived(
    pattern ? codeOf(pattern) + (option ? " / " + esc(option) : "") : esc(option ?? ""),
  );

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

{#if task}
  <PageSection blockKey="task" heading="task"><p class="m-0 text-sm">{@html taskHtml}</p></PageSection>
{/if}
<PageSection blockKey="where" heading="route">
  <DefinitionList>
    <DefinitionRow label="route" html={codeOf(route)} />
    {#if sf.entryPoint}
      <DefinitionRow label="entryPoint"><RecordLink id={stripStep(sf.entryPoint)} /></DefinitionRow>
    {/if}
    {#if pattern || option}
      <DefinitionRow label="pageFrame" html={pageFrameHtml} />
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
<PageSection blockKey="wire" heading="wireframe">
  <figure class="m-0 border border-border-subtle bg-field-on-layer">
    <div class="flex h-8 items-center gap-2 overflow-hidden border-b border-border-subtle bg-layer px-3 font-mono text-xs text-text-secondary">{route}</div>
    <Wireframe text={wire} title={r.title} />
  </figure>
</PageSection>
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
