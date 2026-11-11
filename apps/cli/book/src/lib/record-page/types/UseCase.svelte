<script lang="ts">
  import { book, capitalize, esc, inline, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import Fact from "../blocks/Fact.svelte";
  import SectionJump from "../blocks/SectionJump.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import AccessBlock from "../shared/AccessBlock.svelte";
  import BuiltBlock from "../shared/BuiltBlock.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import ScenarioBlock from "../shared/ScenarioBlock.svelte";
  import WhereBlock from "../shared/WhereBlock.svelte";
  import { tdHtml } from "../page-html";
  import { proseOf } from "../page-prose";
  import {
    browserReached, bulletsHtml, clsBadgeHtml, freshnessNoteHtml, idemTileOf,
    pagingRows, typedFieldHtml,
  } from "../page-values";

  // A use case: access, data, input and output, effects, results and scenarios.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const res = $derived(
    book.resultsOf(id).filter((x) => book.get(stripStep(x))).map((x) => {
      const q = book.get(stripStep(x));
      const qf = q?.fields ?? {};
      return {
        id: x, title: q?.title ?? x, outcome: String(qf.outcome ?? ""), meaning: String(qf.meaning ?? ""),
        code: qf.code ? String(qf.code) : null, kind: /\.(forbidden|invalid-input)$/.test(x) ? "generic" : "",
      };
    }),
  );
  const okR = $derived(res.filter((x) => x.outcome === "success"));
  const noR = $derived(res.filter((x) => x.outcome !== "success"));
  const scs = $derived(book.scenariosOf(id).filter((s) => book.get(stripStep(s))));
  const input = $derived(((f.input as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const output = $derived(f.output as RecordFields | RecordFields[] | undefined);
  const outputGroups = $derived(
    output && !Array.isArray(output) && (output as RecordFields).include
      ? Object.keys((output as RecordFields).include as Record<string, RecordFields>)
      : [],
  );
  const outputRows = $derived(Array.isArray(output) ? (output as RecordFields[]) : []);
  const hasOutput = $derived(outputGroups.length > 0 || outputRows.length > 0 || !!pagingRows(f));
  const paging = $derived(pagingRows(f));
  const idem = $derived(idemTileOf(f));
  const audit = $derived((f.audit as RecordFields | undefined) ?? null);
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const exposure = $derived(proseOf(r, "exposure"));
  const dataCount = $derived(
    (((f.changes as string[] | undefined) ?? []).length) + (((f.reads as string[] | undefined) ?? []).length),
  );
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if exposure}
  <PageSection blockKey="exp" heading="exposure"><RenderedProse html={exposure} /></PageSection>
{/if}
<SectionJump
  items={[
    { key: "acc", label: "access", count: true },
    { key: "where", label: "offers", count: book.screensOf(id).length || browserReached(r) },
    { key: "built", label: "implements", count: ((r.impl as RecordFields[] | undefined) ?? []).length > 0 },
    { key: "data", label: "data", count: dataCount },
    { key: "in", label: "input", count: input.length },
    { key: "out", label: "output", count: hasOutput },
    { key: "eff", label: "effects", count: ((f.effects as RecordFields[] | undefined) ?? []).length },
    { key: "res", label: "Results", count: res.length },
    { key: "sc", label: "Scenarios", count: scs.length },
  ]}
/>
<AccessBlock {id} {f} />
<WhereBlock {id} {r} />
<BuiltBlock {r} />
<DataTouched {f} />
<PageSection blockKey="guar" heading="idempotency">
  <FactGrid>
    {#if idem}
      <FactCell k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </FactGrid>
  {#if audit}
    <div class="mt-2.5">
      {#if audit.exempt}
        <Note kind="quiet" icon="info"><b>Not audited.</b> {String(audit.exempt)}</Note>
      {:else}
        <Note kind="info" icon="book">
          <b>Audited</b> against {String(audit.target ?? "")}. {String(audit.level ?? "")}
        </Note>
      {/if}
    </div>
  {/if}
</PageSection>
{#if f.background}
  <PageSection blockKey="bg" heading="background">
    <RenderedProse html={inline(book, String(f.background), r.src ?? null)} />
  </PageSection>
{/if}
{#if input.length}
  <PageSection blockKey="in" heading="input" count={input.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Rules"]}
      rows={input.map((x) => [
        tdHtml(esc(String(x.name ?? "")) + (x.classification ? `<div class="mt-1">${clsBadgeHtml(x.classification)}</div>` : ""), "nm"),
        tdHtml(typedFieldHtml(x)),
        tdHtml(
          esc(String(x.meaning ?? "")) +
            (x.origin ? `<div class="mt-0.5 text-[11px] text-muted-foreground">from ${esc(String(x.origin))}</div>` : ""),
        ),
        tdHtml(bulletsHtml(x.rules)),
      ])}
    />
  </PageSection>
{/if}
{#if hasOutput}
  <PageSection blockKey="out" heading="output">
    {#if outputGroups.length}
      {@const include = (output as RecordFields).include as Record<string, RecordFields>}
      {#each outputGroups as k}
        {@const g = include[k] ?? {}}
        <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">
          {capitalize(k)}{" "}
          <Fact text={capitalize(String(g.classification ?? "internal"))} />{" "}
          {#if g.freshness}<span class="font-mono text-[11px] font-normal text-muted-foreground">{@html freshnessNoteHtml(g)}</span>{/if}
        </div>
        <DataTable
          head={["Name", "Meaning"]}
          rows={((g.fields as RecordFields[] | undefined) ?? []).map((x) => [tdHtml(esc(String(x.name ?? "")), "nm"), tdHtml(esc(String(x.meaning ?? "")))])}
        />
      {/each}
    {:else if outputRows.length}
      <DataTable
        head={["Name", "Meaning"]}
        rows={outputRows.map((x) => [tdHtml(esc(String(x.name ?? "")), "nm"), tdHtml(esc(String(x.meaning ?? "")))])}
      />
    {/if}
    {#if paging}
      <DefinitionList>
        {#each paging as p}
          <DefinitionRow label={p.label} html={p.html} />
        {/each}
      </DefinitionList>
    {/if}
  </PageSection>
{/if}
<EffectsBlock {f} />
{#if res.length}
  <PageSection blockKey="res" heading="Results" count={res.length}>
    {#if okR.length}
      <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">Outcome: Success</div>
      <ResultCards items={okR} />
    {/if}
    {#if noR.length}
      <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">Outcome: Refusal</div>
      <ResultCards items={noR} />
    {/if}
  </PageSection>
{/if}
<ScenarioBlock {id} {scs} />
