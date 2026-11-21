<script lang="ts">
  import { reachStateWord } from "../page-values";
  import { book, coveragePercent, esc, inline, layerStats } from "$lib/frame";
  import type { BookRecord, LayerKey } from "$lib/frame";
  import { glossaryLinks, RecordLine } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { tdHtml } from "../page-html";
  import { stringField } from "../page-values";

  // A layer page: what the layer answers, who writes it, and its
  // record types with the question each answers. The Intent page opens
  // with the Intent coverage, whatever layers the project claims. A layer
  // the project does not claim reads as not claimed, never as missing.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const key = $derived(id.slice(6));
  const unclaimed = $derived(["intent", "behavior", "implementation", "proof"].includes(key) && !book.claimed(key));
  // The layer keys the book knows, read from a layer page id.
  const LAYER_OF: Record<string, LayerKey> = {
    intent: "intent",
    behavior: "behavior",
    implementation: "implementation",
    proof: "proof",
  };
  const stats = $derived.by((): { ids: string[]; types: string[] } => {
    const layer = LAYER_OF[key];
    if (layer === undefined) return { ids: [], types: [] };
    return layerStats(book, layer);
  });
  const typeRows = $derived(
    stats.types.map((t) => {
      const first = stats.ids.filter((x) => book.typeOf(x) === t);
      return { type: t, first: first[0] ?? "", count: first.length };
    }),
  );
  const typeTable = $derived(
    typeRows.map((row) => [
      tdHtml(
        `<button type="button" class="cursor-pointer text-link hover:underline" data-go="${esc(row.first)}">${esc(book.pluralName(row.type))}</button>`,
      ),
      tdHtml(String(row.count), "num"),
      tdHtml(esc(book.typeQuestion(row.type))),
    ]),
  );
  const question = $derived(stringField(f, "question"));
  const lead = $derived(question ? inline(book, question, null) : "");
  const writes = $derived(stringField(f, "writes"));
  const coverage = $derived(key === "intent" ? book.intentCoverage() : null);
  const problems = $derived(coverage?.problems ?? { reached: 0, total: 0 });
  const constraints = $derived(coverage?.constraints ?? { reached: 0, total: 0 });
  const unreached = $derived((coverage?.entries ?? []).filter((entry) => !entry.reached));
  const reached = $derived((coverage?.entries ?? []).filter((entry) => entry.reached));
  const percent = $derived(coverage && coverage.total > 0 ? coveragePercent(coverage) : 100);
</script>

{#if unclaimed}
  <p class="mb-6 max-w-[72ch] text-sm leading-relaxed text-text-secondary">Not claimed.</p>
{/if}
{#if coverage && coverage.total > 0}
  <PageSection blockKey="intent-coverage" heading="Intent coverage" count={coverage.total}>
    <p class="mt-0 mb-3 text-sm leading-relaxed">
      {coverage.reached} of {coverage.total} requirements, flow steps, problems and constraints reached ({percent}%).
      Requirements: {coverage.requirements.reached} of {coverage.requirements.total}.
      Flow steps: {coverage.steps.reached} of {coverage.steps.total}.
      Problems: {problems.reached} of {problems.total}.
      Constraints: {constraints.reached} of {constraints.total}.
    </p>
    {#if unreached.length}
      <p class="mt-0 mb-1 text-sm font-semibold">Not reached yet</p>
      <ul class="m-0 mb-3 list-none p-0">
        {#each unreached as entry (entry.id)}
          <li class="py-1"><RecordLine id={entry.id} suffix={entry.kind === "step" ? entry.id.slice(entry.id.indexOf("#")) : entry.kind} /></li>
        {/each}
      </ul>
    {/if}
    {#if reached.length}
      <p class="mt-0 mb-1 text-sm font-semibold">Reached</p>
      <ul class="m-0 list-none p-0">
        {#each reached as entry (entry.id)}
          <li class="py-1">
            <RecordLine id={entry.id} suffix={entry.kind === "step" ? entry.id.slice(entry.id.indexOf("#")) : entry.kind} />
            <span class="text-xs text-text-secondary">reached by {#each entry.reachedBy as rid, j (rid)}{#if j > 0}{", "}{/if}{rid}{#if reachStateWord(entry.claims, rid)} ({reachStateWord(entry.claims, rid)}){/if}{/each}</span>
          </li>
        {/each}
      </ul>
    {/if}
  </PageSection>
{/if}
{#if lead}
  <p class="mb-6 max-w-[72ch] text-base leading-relaxed" use:glossaryLinks={lead}>{@html lead}</p>
{/if}
{#if writes}
  <div class="mb-8">
    <DefinitionList
      pairs={[{ label: "Written by", html: esc(writes) }]}
    />
  </div>
{/if}
{#if typeRows.length}
  <PageSection blockKey="types" heading="Record types" count={typeRows.length}>
    <DataTable head={["Type", "Records", "Question"]} rows={typeTable} />
  </PageSection>
{/if}
