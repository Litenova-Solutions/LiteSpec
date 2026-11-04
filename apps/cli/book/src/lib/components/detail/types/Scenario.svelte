<script lang="ts">
  import { book, plural, type ProofState } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import ProofMark from "$lib/components/book/ProofMark.svelte";
  import Block from "../blocks/Block.svelte";
  import Refs from "../blocks/Refs.svelte";
  import Table from "../blocks/Table.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import { fileLinkHtml, resultInfoOf, tdHtml } from "../helpers";

  // A scenario: its Given, When and Then, the result it shows, and its proof.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const ri = $derived(resultInfoOf(id));
  const exercises = $derived(((f.exercises as string[] | undefined) ?? []).map(String));
  const examples = $derived(((f.examples as Fields[] | undefined) ?? []) as Fields[]);
  const pr = $derived(((r.proof ?? {}) as Fields) as Fields);
  const t0 = $derived(((pr.test ?? {}) as Fields) as Fields);
  const testRun = $derived(book.man?.testRun ?? null);
  const proofHint = $derived(
    t0.file || t0.path
      ? fileLinkHtml(String(t0.path ?? t0.file), t0.line ? Number(t0.line) : null, t0.name ? String(t0.name) : undefined)
      : pr.tests
        ? plural(Number(pr.tests), "test") + " claim this example"
        : "No test claims it",
  );
</script>

<Block blockKey="gwt" heading="given, when, then">
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
</Block>
{#if exercises.length}
  <Block blockKey="exer" heading="exercises" count={exercises.length}><Refs ids={exercises} /></Block>
{/if}
{#if ri}
  <Block blockKey="res" heading="result">
    <ResultCards items={[{ title: ri.title, outcome: ri.outcome, code: ri.code, meaning: ri.meaning }]} />
  </Block>
{/if}
{#if examples.length}
  <Block blockKey="ex" heading="Examples">
    <Table
      head={Object.keys(examples[0])}
      rows={examples.map((row) => Object.keys(row).map((k) => tdHtml(esc(row[k]))))}
    />
  </Block>
{/if}
<Block blockKey="proof" heading="proves">
  <Tiles>
    <Tile k="Proof" hint={proofHint}><ProofMark state={(pr.state as ProofState | undefined) ?? null} /></Tile>
    <Tile k="Test run" hint={testRun?.date ? book.dateHint(String(testRun.date)) : "the model holds no test run"}>
      {testRun?.date ? book.fmtDate(String(testRun.date)) : "None recorded"}
    </Tile>
  </Tiles>
</Block>
