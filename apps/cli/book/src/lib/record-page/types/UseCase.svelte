<script lang="ts">
  import { book, capitalize, esc, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordLink, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import AccessBlock from "../shared/AccessBlock.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import ScenarioBlock from "../shared/ScenarioBlock.svelte";
  import { clsTag, codeOf, fieldType, muted, refLink, td, thRow } from "../shared/record-cells";
  import { proseOf } from "../page-prose";
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
  const model = $derived(useCaseModel(f, r, resultInputs));
  const scs = $derived(book.scenariosOf(id).filter((s) => book.get(stripStep(s))));
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const exposure = $derived(proseOf(r, "exposure"));
  const inputClassification = $derived.by(() => {
    const all = model.input.map((x) => String(x.classification ?? ""));
    if (!all.length || !all[0]) return "";
    return all.every((c) => c === all[0]) ? (all[0] ?? "") : "";
  });

  function inputQualifiers(x: RecordFields): string {
    const quals = [
      x.required === true ? "required" : x.required === false ? "optional" : "",
      x.many ? "many" : "",
      x.origin ? "from " + String(x.origin) : "",
    ].filter(Boolean);
    return quals.length ? muted(quals.map(esc).join(" &middot; ")) : "";
  }

  function inputRules(x: RecordFields): string {
    const rules: unknown[] = typeof x.rules === "string" ? (x.rules ? [x.rules] : []) : Array.isArray(x.rules) ? x.rules : [];
    if (!rules.length) return muted("None");
    return rules.map((rule) => refLink(book, String(rule))).join(", ");
  }
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if exposure}
  <PageSection blockKey="exp" heading="exposure"><RenderedProse html={exposure} /></PageSection>
{/if}
<AccessBlock {id} {f} />
<DataTouched {f} guarantees />
{#if model.input.length}
  <PageSection blockKey="in" heading="input" count={model.input.length}>
    <DataTable
      head={["Name", "Type", "Meaning", "Rules"]}
      caption={`Input of ${r.title}`}
      rows={model.input.map((x) => [
        thRow(
          codeOf(String(x.name ?? "")) +
            inputQualifiers(x) +
            (inputClassification ? "" : x.classification ? `<div class="mt-1">${clsTag(x.classification)}</div>` : ""),
        ),
        td(fieldType(book, x)),
        td(esc(String(x.meaning ?? ""))),
        td(inputRules(x)),
      ])}
    />
    {#if inputClassification}
      <p class="mt-4 text-sm text-text-secondary">Every input is classified {inputClassification}.</p>
    {/if}
  </PageSection>
{/if}
{#if model.hasOutput}
  <PageSection blockKey="out" heading="output">
    {#if model.outputSections.length}
      {#each model.outputSections as s (s.key)}
        <div class="mt-6 mb-2 text-sm font-semibold text-text-secondary">
          {capitalize(s.key)}{" "}
          {@html clsTag(s.classification)}{" "}
          {#if s.freshnessHtml}<span class="font-mono text-xs font-normal text-text-secondary">{@html s.freshnessHtml}</span>{/if}
          {#if s.decidedBy}<span class="text-xs font-normal text-text-secondary">, decided by <RecordLink id={s.decidedBy} /></span>{/if}
        </div>
        <DataTable
          head={["Name", "Meaning"]}
          caption={`Output of ${r.title}`}
          rows={s.fields.map((x) => [thRow(codeOf(x.name)), td(esc(x.meaning))])}
        />
      {/each}
    {:else if model.outputRows.length}
      <DataTable
        head={["Name", "Meaning"]}
        caption={`Output of ${r.title}`}
        rows={model.outputRows.map((x) => [thRow(codeOf(x.name)), td(esc(x.meaning))])}
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
<EffectsBlock {f} caption={`Effects of ${r.title}`} />
{#if model.results.length}
  <PageSection blockKey="res" heading="Results" count={model.results.length}>
    <ResultCards items={model.results} caption={`Results of ${r.title}`} />
  </PageSection>
{/if}
<ScenarioBlock {id} {scs} />
