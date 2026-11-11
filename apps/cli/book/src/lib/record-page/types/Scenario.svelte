<script lang="ts">
  import { book, esc, pluralize, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { EditorFileLink, ProofMark } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import GivenWhenThen from "../shared/GivenWhenThen.svelte";
  import { tdHtml } from "../page-html";
  import { resultInfoOf, scenarioFields } from "../page-values";

  // A scenario: its Given, When and Then, the result it shows, and its proof.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const sf = $derived(scenarioFields(r.fields ?? {}));
  const resultId = $derived(book.outs(stripStep(id), "result")[0] ?? null);
  const ri = $derived(resultInfoOf(resultId ? book.get(resultId) : null));
  const proofTest = $derived(r.proof?.test ?? null);
  const proofTests = $derived(r.proof?.tests);
  const proofState = $derived(r.proof?.state ?? null);
  const testPath = $derived(proofTest?.path ?? null);
  const testLine = $derived(proofTest?.line ?? null);
  const testName = $derived(proofTest?.name ?? undefined);
  const testRun = $derived(book.man?.testRun ?? null);
  const proofText = $derived(proofTests ? pluralize(proofTests, "test") + " claim this example" : "No test claims it");
</script>

<PageSection blockKey="gwt" heading="Given, When, Then">
  <div class="border-l-[3px] border-border py-0.5 pl-3.5">
    <GivenWhenThen given={sf.given ?? ""} when={sf.when ?? ""} then={sf.then ?? ""} />
  </div>
</PageSection>
{#if sf.exercises.length}
  <PageSection blockKey="exer" heading="exercises" count={sf.exercises.length}><RecordList ids={sf.exercises} /></PageSection>
{/if}
{#if ri}
  <PageSection blockKey="res" heading="Result">
    <ResultCards items={[{ title: ri.title, outcome: ri.outcome, code: ri.code, meaning: ri.meaning }]} />
  </PageSection>
{/if}
{#if sf.examples.length}
  <PageSection blockKey="ex" heading="Examples">
    <DataTable
      head={Object.keys(sf.examples[0] ?? {})}
      rows={sf.examples.map((row) => Object.keys(row).map((k) => tdHtml(esc(row[k]))))}
    />
  </PageSection>
{/if}
<PageSection blockKey="proof" heading="proves">
  <FactGrid>
    <FactCell k="Proof" hint={testPath ? "" : proofText}>
      <ProofMark state={proofState} />
      {#if testPath}<div class="mt-1"><EditorFileLink path={testPath} line={testLine} label={testName} /></div>{/if}
    </FactCell>
    <FactCell k="Test run" hint={testRun?.date ? book.dateHint(String(testRun.date)) : "the model holds no test run"}>
      {testRun?.date ? book.fmtDate(String(testRun.date)) : "None recorded"}
    </FactCell>
  </FactGrid>
</PageSection>
