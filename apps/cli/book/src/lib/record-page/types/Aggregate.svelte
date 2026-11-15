<script lang="ts">
  import { book, capitalize, esc, formatLabel, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { clsTag, codeOf, muted, mutedInline, refLink, tag, td, thRow } from "../shared/record-cells";
  import { aggregateFields, periodSuffixHtml, refusalOf, stringField, stringList } from "../page-values";
  import { aggregateModel } from "./aggregate-model";

  // An aggregate: its states, the transitions between them, its data,
  // and the rules and events that belong to it.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const af = $derived(aggregateFields(f));
  const ruleInputs = $derived(
    book.rulesOf(id).filter((x) => book.get(x)).map((x) => {
      const qf = book.get(x)?.fields ?? {};
      const rid = book.outs(stripStep(x), "refusal")[0] ?? null;
      const rf = refusalOf(rid ? book.get(rid) : null, rid);
      return {
        id: x,
        statement: stringField(qf, "statement") ?? "",
        kind: stringField(qf, "kind") ?? "",
        refusalCode: rf.info?.code ?? null,
        refusalId: rf.id,
      };
    }),
  );
  const model = $derived(aggregateModel(f, r));
  const evs = $derived(book.eventsOf(id));
  const stateRows = $derived(
    model.states.map((s) => [
      thRow(
        esc(s.name) +
          (s.created ? ` ${tag("created as", "outline")}` : "") +
          (s.unreached ? ` ${tag("not reachable", "outline", "sm", "reachable")}` : ""),
      ),
      td(esc(s.meaning)),
    ]),
  );
  const transitionRows = $derived(
    model.transitions.flatMap((t) =>
      t.targets.flatMap((x) =>
        x.movedBy.map((b) => [
          td(t.created ? "Created as" : esc(t.from)),
          td(esc(x.to)),
          td(b ? refLink(book, b) : mutedInline("no use case or job named")),
        ]),
      ),
    ),
  );
  const classRows = $derived(
    af.dataClasses.map((c) => [
      thRow(`<b>${esc(capitalize(formatLabel(c.name)))}</b>${c.classification ? muted(clsTag(c.classification)) : ""}`),
      td(stringList(c, "fields").map(codeOf).join(" ")),
      td(esc(c.purpose ?? "")),
      td(
        `<b>${esc(capitalize(String(c.onErasure ?? "")))}</b>${periodSuffixHtml(c.period)}` +
          (c.basis || c.reason ? muted(esc(String(c.basis ?? c.reason ?? ""))) : ""),
      ),
    ]),
  );
  const dataRows = $derived(
    af.data.map((d) => [thRow(codeOf(String(d.name ?? ""))), td(esc(String(d.meaning ?? ""))), td(clsTag(d.classification))]),
  );
  const ruleRows = $derived(
    ruleInputs.map((x) => [
      thRow(refLink(book, x.id) + (x.statement ? muted(esc(x.statement)) : "")),
      td(esc(capitalize(x.kind))),
      td(
        x.refusalCode
          ? codeOf(x.refusalCode)
          : x.refusalId
            ? refLink(book, x.refusalId)
            : muted("None"),
      ),
    ]),
  );
</script>

{#if af.finishedWhen}
  <Note kind="good" icon="passing"><b>Finished when</b> {af.finishedWhen}</Note>
{/if}
{#if model.states.length}
  <PageSection blockKey="states" heading="states" count={model.states.length}>
    <DataTable head={["State", "Meaning"]} caption={`States of ${r.title}`} rows={stateRows} />
  </PageSection>
{/if}
{#if model.transitions.length}
  <PageSection blockKey="transition" heading="transitions" count={af.transitions.length}>
    <DataTable head={["From", "To", "By"]} caption={`Transitions of ${r.title}`} rows={transitionRows} />
  </PageSection>
{/if}
{#if af.dataClasses.length}
  <PageSection blockKey="classes" heading="dataClasses" count={af.dataClasses.length}>
    <DataTable head={["Data class", "Fields", "Purpose", "Erasure"]} caption={`Data classes of ${r.title}`} rows={classRows} />
  </PageSection>
{/if}
{#if af.data.length}
  <PageSection blockKey="data" heading="data" count={af.data.length}>
    <DataTable head={["Name", "Meaning", "Classification"]} caption={`Data of ${r.title}`} rows={dataRows} />
  </PageSection>
{/if}
{#if ruleInputs.length}
  <PageSection blockKey="rules" heading="Rules" count={ruleInputs.length}>
    <DataTable head={["Rule", "Kind", "Refusal code"]} caption={`Rules of ${r.title}`} rows={ruleRows} />
  </PageSection>
{/if}
{#if evs.length}
  <PageSection blockKey="events" heading="Domain events" count={evs.length}><RecordList ids={evs} /></PageSection>
{/if}
