<script lang="ts">
  import { book, esc, hoverState, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { Hover, RecordLink, TypeTag } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { tdHtml } from "../page-html";
  import { reachStateWord, requirementFields, stringField } from "../page-values";

  // A requirement: its waivers, its measure, and the values it takes over time.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const qf = $derived(requirementFields(r.fields ?? {}));
  const reach = $derived(r.intentCoverage?.reachedBy ?? []);
  const reachClaims = $derived(r.intentCoverage?.claims ?? []);
</script>

{#if qf.except.length}
  <PageSection blockKey="exc" heading="except" count={qf.except.length}>
    {#each qf.except as x, i (i)}
      {@const fallback = stringField(x, "fallback") ?? ""}
      {@const fb = stripStep(fallback)}
      {@const fx = book.get(fb)}
      <Note kind="warn" icon="alert">
        <b>{book.fieldLabel("except")} {stringField(x, "condition") ?? ""}</b>{" "}
        Then the fallback holds: <RecordLink id={fallback} />{fx ? ", " + String(fx.fields?.statement ?? "") : ""}
      </Note>
    {/each}
  </PageSection>
{/if}
{#if qf.measure}
  <PageSection blockKey="meas" heading="measure">
    <FactGrid>
      <FactCell k="Measure">{qf.measure}</FactCell>
      {#if qf.target}<FactCell k="target">{qf.target}</FactCell>{/if}
      {#if qf.dueBy}
        <FactCell k="dueBy" hint={book.dateHint(qf.dueBy)}>{book.fmtDate(qf.dueBy)}</FactCell>
      {/if}
    </FactGrid>
    <DefinitionList>
      {#if qf.check}<DefinitionRow label="Check">{qf.check}</DefinitionRow>{/if}
      {#if qf.runsOn}<DefinitionRow label="Runs on">{qf.runsOn}</DefinitionRow>{/if}
      {#if qf.evidence}<DefinitionRow label="Evidence">{qf.evidence}</DefinitionRow>{/if}
    </DefinitionList>
  </PageSection>
{/if}
{#if qf.values.length}
  <PageSection blockKey="vals" heading="values">
    <DataTable
      head={["Value", "Effective from"]}
      rows={qf.values.map((v) => {
        const effectiveFrom = stringField(v, "effectiveFrom") ?? "";
        return [
          tdHtml(esc(v.value)),
          tdHtml(book.fmtDate(effectiveFrom) + " &middot; " + book.dateHint(effectiveFrom), "mut"),
        ];
      })}
    />
  </PageSection>
{/if}
{#if reach.length}
  <PageSection blockKey="reach" heading="reachedBy" count={reach.length}>
    <ul class="m-0 list-none border-t border-border-subtle p-0">
      {#each reach as rid (rid)}
        {@const state = reachStateWord(reachClaims, rid)}
        <li class="grid grid-cols-[20px_minmax(0,1fr)] items-center gap-x-3 gap-y-0.5 border-b border-border-subtle py-3 pr-4 hover:bg-layer-hover">
          <TypeTag type={book.typeOf(stripStep(rid))} />
          <span class="min-w-0 text-sm">
            <RecordLink id={rid} />{#if state}{" "}<Hover hover={hoverState(book, state === "Verified" ? "verified" : "claimed")}>
              <span
                data-explain={state === "Verified" ? "verified" : "claimed"}
                class="inline-flex h-[18px] items-center gap-1 rounded-full px-2 text-xs tracking-[0.32px] {state === 'Verified'
                  ? 'bg-tag-teal text-tag-teal-foreground'
                  : 'bg-tag-warm text-tag-warm-foreground'}"
              >
                {state}
              </span>
            </Hover>{/if}
          </span>
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
