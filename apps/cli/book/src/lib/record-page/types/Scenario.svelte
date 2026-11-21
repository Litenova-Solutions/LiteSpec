<script lang="ts">
  import { book, capitalize, esc, freshnessLine, pluralize, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceFileLink, ProofMark } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import GivenWhenThen from "../shared/GivenWhenThen.svelte";
  import { td, thRow } from "../shared/record-cells";
  import { resultInfoOf, scenarioFields } from "../page-values";

  // A scenario: its Given, When and Then, the result it shows, and its proof.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const sf = $derived(scenarioFields(r.fields ?? {}));
  const competesPhrase = $derived(book.phraseFor("competesWith", r.type, "forward"));
  const resultId = $derived(book.outs(stripStep(id), "result")[0] ?? null);
  const ri = $derived(resultInfoOf(resultId ? book.get(resultId) : null));
  const resultLink = $derived(resultId ?? "");
  const proofTest = $derived(r.proof?.test ?? null);
  const proofTests = $derived(r.proof?.tests);
  const proofState = $derived(r.proof?.state ?? null);
  const stale = $derived(freshnessLine(r.proof));
  const testPath = $derived(proofTest?.path ?? null);
  const testLine = $derived(proofTest?.line ?? null);
  const testName = $derived(proofTest?.name ?? undefined);
  const testRun = $derived(book.man?.testRun ?? null);
  const proofText = $derived(proofTests ? pluralize(proofTests, "test") + " claim this example" : "No test claims it");
  const exampleKeys = $derived(Object.keys(sf.examples[0] ?? {}));
</script>

{#if ri || sf.exercises.length || sf.competesWith}
  <DefinitionList>
    {#if ri}
      <DefinitionRow label="Result">
        <RecordLink id={resultLink} />
        {#if ri.meaning}<span class="mt-1 block text-xs tracking-[0.32px] text-text-helper">{ri.meaning}</span>{/if}
      </DefinitionRow>
    {/if}
    {#if sf.exercises.length}
      <DefinitionRow label="Exercises">
        {#each sf.exercises as x, i (x)}{#if i > 0}{", "}{/if}<RecordLink id={x} />{/each}
      </DefinitionRow>
    {/if}
    {#if sf.competesWith}
      <DefinitionRow label={capitalize(competesPhrase)}>
        <RecordLink id={sf.competesWith} />
      </DefinitionRow>
    {/if}
  </DefinitionList>
{/if}
<PageSection blockKey="gwt" heading="Given, When, Then">
  <GivenWhenThen given={sf.given ?? ""} when={sf.when ?? ""} then={sf.then ?? ""} />
</PageSection>
{#if sf.examples.length}
  <PageSection blockKey="ex" heading="Examples">
    <DataTable
      head={exampleKeys}
      caption={`Examples of ${r.title}`}
      rows={sf.examples.map((row) => exampleKeys.map((k, j) => (j === 0 ? thRow(esc(row[k])) : td(esc(row[k])))))}
    />
  </PageSection>
{/if}
<PageSection blockKey="proof" heading="proves">
  <FactGrid>
    <FactCell k="Proof" hint={testPath ? "" : proofText}>
      <ProofMark state={proofState} />
      {#if stale}<div class="mt-1 text-xs tracking-[0.32px] text-text-helper">{stale}</div>{/if}
      {#if testPath}<div class="mt-1"><SourceFileLink path={testPath} line={testLine} label={testName} /></div>{/if}
    </FactCell>
    <FactCell k="Test run" hint={testRun?.date ? book.dateHint(String(testRun.date)) : "the model holds no test run"}>
      {testRun?.date ? book.fmtDate(String(testRun.date)) : "None recorded"}
    </FactCell>
  </FactGrid>
</PageSection>
