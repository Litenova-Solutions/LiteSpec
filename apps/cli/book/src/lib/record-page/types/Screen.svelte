<script lang="ts">
  import { book, capitalize, esc, formatLabel, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, inlineRefHtml, tdHtml } from "../page-html";

  // A screen: where it lives, and each use case it offers with whether its page calls it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const off = $derived(((f.offers as string[] | undefined) ?? []).map((x) => stripStep(String(x))));
  const calls = $derived(((r.calls as string[] | undefined) ?? []).map(String));
  const route = $derived((r.route as RecordFields | undefined) ?? null);
  const tnCls = "font-mono text-[11px] text-muted-foreground";
  const warnCls = "font-medium text-warn-foreground";
</script>

<PageSection blockKey="where" heading="address">
  <DefinitionList>
    <DefinitionRow label="address" html={`<span class="${codeCls}">${esc(String(f.address ?? f.host ?? ""))}</span>`} />
    {#if f.entryPoint}
      <DefinitionRow label="entryPoint"><RecordLink id={stripStep(String(f.entryPoint))} /></DefinitionRow>
    {/if}
    {#if f.floorplan}
      <DefinitionRow label="floorplan">{capitalize(formatLabel(f.floorplan))}</DefinitionRow>
    {/if}
    <DefinitionRow label="Route">
      {#if route}
        <SourceLink src={{ path: String(route.file ?? route.path ?? ""), line: 1 }} />
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
              built: !!((q.machine as RecordFields).coded),
              scenarios: sc.length,
              verified: sc.filter((x) => book.get(stripStep(x))?.proof?.state === "awaiting").length,
            }
          : null;
        return [
          tdHtml(inlineRefHtml(u)),
          tdHtml(!route ? `<span class="${tnCls}">no page</span>` : calls.includes(u) ? "Yes" : `<span class="${warnCls}">No</span>`),
          tdHtml(b ? (b.built ? "Yes" : `<span class="${warnCls}">No</span>`) : ""),
          tdHtml(b ? (b.scenarios ? `${b.verified} of ${b.scenarios}` : `<span class="${warnCls}">None</span>`) : ""),
        ];
      })}
    />
  </PageSection>
{/if}
