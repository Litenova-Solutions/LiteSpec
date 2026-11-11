<script lang="ts">
  import { book, esc, pluralize } from "$lib/frame";
  import type { BookRecord, ProofState, RecordFields } from "$lib/frame";
  import { ProofMark } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import { fileLinkHtml, tdHtml } from "../page-html";
  import { resultInfoOf } from "../page-values";

  // A scenario: its Given, When and Then, the result it shows, and its proof.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const ri = $derived(resultInfoOf(id));
  const exercises = $derived(((f.exercises as string[] | undefined) ?? []).map(String));
  const examples = $derived(((f.examples as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const pr = $derived(((r.proof ?? {}) as RecordFields) as RecordFields);
  const t0 = $derived(((pr.test ?? {}) as RecordFields) as RecordFields);
  const testRun = $derived(book.man?.testRun ?? null);
  const proofHint = $derived(
    t0.file || t0.path
      ? fileLinkHtml(String(t0.path ?? t0.file), t0.line ? Number(t0.line) : null, t0.name ? String(t0.name) : undefined)
      : pr.tests
        ? pluralize(Number(pr.tests), "test") + " claim this example"
        : "No test claims it",
  );
</script>

<PageSection blockKey="gwt" heading="given, when, then">
  <div class="border-l-[3px] border-border py-0.5 pl-3.5">
    <dl class="grid grid-cols-[56px_minmax(0,1fr)] gap-x-3 gap-y-1.5 text-sm">
      <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">Given</dt>
      <dd class="m-0">{String(f.given ?? "")}</dd>
      <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">When</dt>
      <dd class="m-0">{String(f.when ?? "")}</dd>
      <dt class="pt-0.5 font-mono text-[11.5px] font-medium tracking-[0.06em] text-muted-foreground uppercase">Then</dt>
      <dd class="m-0">{String(f.then ?? "")}</dd>
    </dl>
  </div>
</PageSection>
{#if exercises.length}
  <PageSection blockKey="exer" heading="exercises" count={exercises.length}><RecordList ids={exercises} /></PageSection>
{/if}
{#if ri}
  <PageSection blockKey="res" heading="result">
    <ResultCards items={[{ title: ri.title, outcome: ri.outcome, code: ri.code, meaning: ri.meaning }]} />
  </PageSection>
{/if}
{#if examples.length}
  <PageSection blockKey="ex" heading="Examples">
    <DataTable
      head={Object.keys(examples[0])}
      rows={examples.map((row) => Object.keys(row).map((k) => tdHtml(esc(row[k]))))}
    />
  </PageSection>
{/if}
<PageSection blockKey="proof" heading="proves">
  <FactGrid>
    <FactCell k="Proof" hint={proofHint}><ProofMark state={(pr.state as ProofState | undefined) ?? null} /></FactCell>
    <FactCell k="Test run" hint={testRun?.date ? book.dateHint(String(testRun.date)) : "the model holds no test run"}>
      {testRun?.date ? book.fmtDate(String(testRun.date)) : "None recorded"}
    </FactCell>
  </FactGrid>
</PageSection>
