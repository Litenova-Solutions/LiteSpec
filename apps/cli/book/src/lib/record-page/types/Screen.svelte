<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import { Wireframe } from "$lib/wireframe";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../page-html";
  import { objectField, routeFileOf, screenFields, stringField, stringList } from "../page-values";

  // A screen: where it lives, its wireframe, each use case it offers with
  // whether its page calls it, and the screens a person reaches next.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const sf = $derived(screenFields(f));
  const off = $derived(sf.offers.map((x) => stripStep(x)));
  const calls = $derived(r.calls ?? []);
  const routePath = $derived(routeFileOf(r.route));
  const frame = $derived(objectField(f, "pageFrame"));
  const pattern = $derived(stringField(frame, "pattern"));
  const option = $derived(stringField(frame, "option"));
  const wire = $derived(stringField(f, "wireframe"));
  const leads = $derived(stringList(f, "leadsTo").map((x) => stripStep(x)));
  const tnCls = "font-mono text-xs text-slate-600";
  const warnCls = "font-medium text-warn-foreground";
</script>

<PageSection blockKey="where" heading="address">
  <DefinitionList>
    <DefinitionRow label="address" html={`<span class="${codeCls}">${esc(sf.address ?? sf.host ?? "")}</span>`} />
    {#if sf.entryPoint}
      <DefinitionRow label="entryPoint"><RecordLink id={stripStep(sf.entryPoint)} /></DefinitionRow>
    {/if}
    {#if pattern || option}
      <DefinitionRow
        label="pageFrame"
        html={pattern ? `<span class="${codeCls}">${esc(pattern)}</span>${option ? " · " + esc(option) : ""}` : esc(option ?? "")}
      />
    {/if}
    <DefinitionRow label="Route">
      {#if r.route}
        <SourceLink src={{ path: routePath ?? "", line: 1 }} />
      {:else}
        <span class={warnCls}>No page carries this screen.</span>
      {/if}
    </DefinitionRow>
  </DefinitionList>
</PageSection>
<PageSection blockKey="wire" heading="wireframe">
  <Wireframe text={wire} title={r.title} />
</PageSection>
{#if off.length}
  <PageSection blockKey="offers" heading="offers" count={off.length}>
    <DataTable
      head={["Use case", "Calls", "Coded", "Scenarios"]}
      rows={off.map((u) => {
        const q = book.get(stripStep(u));
        const sc = book.scenariosOf(u);
        const b = q?.machine
          ? {
              built: !!q.machine.coded,
              scenarios: sc.length,
              verified: sc.filter((x) => book.get(stripStep(x))?.proof?.state === "awaiting").length,
            }
          : null;
        return [
          tdHtml(inlineRefHtml(book, u)),
          tdHtml(!r.route ? `<span class="${tnCls}">no page</span>` : calls.includes(u) ? "Yes" : `<span class="${warnCls}">No</span>`),
          tdHtml(b ? (b.built ? "Yes" : `<span class="${warnCls}">No</span>`) : ""),
          tdHtml(b ? (b.scenarios ? `${b.verified} of ${b.scenarios}` : `<span class="${warnCls}">None</span>`) : ""),
        ];
      })}
    />
  </PageSection>
{/if}
{#if leads.length}
  <PageSection blockKey="leads" heading="leadsTo" count={leads.length}>
    <RecordList ids={leads} />
  </PageSection>
{/if}
