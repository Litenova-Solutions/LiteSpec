<script lang="ts">
  import { book, capitalize, esc, inline, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
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
  import { browserReached, bulletsHtml, clsBadgeHtml, idemTileOf, objectField, stringField, stringList, typedFieldHtml } from "../page-values";
  import { useCaseModel } from "./use-case-model";

  // A use case: access, data, input and output, effects, results and scenarios.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const resultInputs = $derived(
    book.resultsOf(id).flatMap((x) => {
      const q = book.get(stripStep(x));
      return q ? [{ id: x, record: q }] : [];
    }),
  );
  const model = $derived(useCaseModel(book, f, r, resultInputs));
  const scs = $derived(book.scenariosOf(id).filter((s) => book.get(stripStep(s))));
  const offered = $derived(
    browserReached(stringList(f, "reachedThrough"), (e) => book.kids(e).some((k) => book.typeOf(k) === "screen")),
  );
  const idem = $derived(idemTileOf(f));
  const audit = $derived(objectField(f, "audit"));
  const background = $derived(stringField(f, "background"));
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const exposure = $derived(proseOf(r, "exposure"));
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
    { key: "where", label: "offers", count: book.screensOf(id).length || offered },
    { key: "built", label: "implements", count: model.hasImpl },
    { key: "data", label: "data", count: model.dataCount },
    { key: "in", label: "input", count: model.input.length },
    { key: "out", label: "output", count: model.hasOutput },
    { key: "eff", label: "effects", count: model.effectCount },
    { key: "res", label: "Results", count: model.results.length },
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
{#if background}
  <PageSection blockKey="bg" heading="background">
    <RenderedProse html={inline(book, background, r.src ?? null)} />
  </PageSection>
{/if}
{#if model.input.length}
  <PageSection blockKey="in" heading="input" count={model.input.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Rules"]}
      rows={model.input.map((x) => [
        tdHtml(esc(String(x.name ?? "")) + (x.classification ? `<div class="mt-1">${clsBadgeHtml(x.classification)}</div>` : ""), "nm"),
        tdHtml(typedFieldHtml(book, x)),
        tdHtml(
          esc(String(x.meaning ?? "")) +
            (x.origin ? `<div class="mt-0.5 text-[0.6875rem] text-muted-foreground">from ${esc(String(x.origin))}</div>` : ""),
        ),
        tdHtml(bulletsHtml(x.rules)),
      ])}
    />
  </PageSection>
{/if}
{#if model.hasOutput}
  <PageSection blockKey="out" heading="output">
    {#if model.outputSections.length}
      {#each model.outputSections as s (s.key)}
        <div class="mt-[18px] mb-2 text-[0.8125rem] font-semibold text-subtle-foreground">
          {capitalize(s.key)}{" "}
          <Fact text={capitalize(s.classification)} />{" "}
          {#if s.freshnessHtml}<span class="font-mono text-[0.6875rem] font-normal text-muted-foreground">{@html s.freshnessHtml}</span>{/if}
        </div>
        <DataTable
          head={["Name", "Meaning"]}
          rows={s.fields.map((x) => [tdHtml(esc(x.name), "nm"), tdHtml(esc(x.meaning))])}
        />
      {/each}
    {:else if model.outputRows.length}
      <DataTable
        head={["Name", "Meaning"]}
        rows={model.outputRows.map((x) => [tdHtml(esc(x.name), "nm"), tdHtml(esc(x.meaning))])}
      />
    {/if}
    {#if model.paging}
      <DefinitionList>
        {#each model.paging as p (p.label)}
          <DefinitionRow label={p.label} html={p.html} />
        {/each}
      </DefinitionList>
    {/if}
  </PageSection>
{/if}
<EffectsBlock {f} />
{#if model.results.length}
  <PageSection blockKey="res" heading="Results" count={model.results.length}>
    {#if model.ok.length}
      <div class="mt-[18px] mb-2 text-[0.8125rem] font-semibold text-subtle-foreground">Outcome: Success</div>
      <ResultCards items={model.ok} />
    {/if}
    {#if model.refused.length}
      <div class="mt-[18px] mb-2 text-[0.8125rem] font-semibold text-subtle-foreground">Outcome: Refusal</div>
      <ResultCards items={model.refused} />
    {/if}
  </PageSection>
{/if}
<ScenarioBlock {id} {scs} />
