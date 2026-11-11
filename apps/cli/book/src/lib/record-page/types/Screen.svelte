<script lang="ts">
  import { book, capitalize, esc, formatLabel, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../page-html";
  import { routeFileOf, screenFields } from "../page-values";

  // A screen: where it lives, and each use case it offers with whether its page calls it.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const sf = $derived(screenFields(r.fields ?? {}));
  const off = $derived(sf.offers.map((x) => stripStep(x)));
  const calls = $derived(r.calls ?? []);
  const routePath = $derived(routeFileOf(r.route));
  const tnCls = "font-mono text-[0.6875rem] text-muted-foreground";
  const warnCls = "font-medium text-warn-foreground";
</script>

<PageSection blockKey="where" heading="address">
  <DefinitionList>
    <DefinitionRow label="address" html={`<span class="${codeCls}">${esc(sf.address ?? sf.host ?? "")}</span>`} />
    {#if sf.entryPoint}
      <DefinitionRow label="entryPoint"><RecordLink id={stripStep(sf.entryPoint)} /></DefinitionRow>
    {/if}
    {#if sf.floorplan}
      <DefinitionRow label="floorplan">{capitalize(formatLabel(sf.floorplan))}</DefinitionRow>
    {/if}
    <DefinitionRow label="Route">
      {#if r.route}
        <SourceLink src={{ path: routePath ?? "", line: 1 }} />
      {:else}
        <span class={warnCls}>No page carries this screen yet</span>
      {/if}
    </DefinitionRow>
  </DefinitionList>
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
