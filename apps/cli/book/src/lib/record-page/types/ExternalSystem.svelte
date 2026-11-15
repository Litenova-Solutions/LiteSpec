<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book, dedupe, duration, esc, formatLabel, stripStep } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { codeOf, muted, refLink, tag, td, thRow } from "../shared/record-cells";
  import { objectField, objectList, stringField } from "../page-values";

  // An external system: the decision choosing its provider, its operations, and how erasure reaches it.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const decidedBy = $derived(
    dedupe(
      (r.in ?? [])
        .filter((e) => e.key === "decides" || e.key.endsWith(".decides"))
        .map((e) => stripStep(e.id))
        .filter((x) => x.startsWith("decision/")),
    ).sort(),
  );
  const ops = $derived(objectList(f, "operations"));
  const erasure = $derived(objectField(f, "erasure"));
  const erasureImpossible = $derived.by(() => {
    const mode = stringField(erasure, "mode");
    return mode !== "call" && mode !== "event";
  });
  const erasureHtml = $derived.by(() => {
    const e = erasure;
    if (!e) return "";
    const mode = stringField(e, "mode");
    if (mode === "call") {
      const op = String(e.operation ?? "");
      const tail = op.split("#")[1];
      return "Calls " + refLink(book, stripStep(op)) + (tail ? ` <span class="font-mono text-xs text-text-helper">${esc(formatLabel(tail))}</span>` : "");
    }
    if (mode === "event") return "Forwards through " + refLink(book, stringField(e, "event") ?? "");
    const reason = stringField(e, "reason");
    return "Impossible" + (reason ? ", because " + esc(reason) : "");
  });

  function opCell(o: RecordFields, name: "onTimeout" | "onUnknown"): string {
    const v = stringField(o, name);
    return v ? esc(v) : muted("&mdash;");
  }
</script>

{#if decidedBy.length}
  <PageSection blockKey="dec" heading="Decision" count={decidedBy.length}>
    <RecordList ids={decidedBy} />
  </PageSection>
{/if}
{#if ops.length}
  <PageSection blockKey="ops" heading="operations" count={ops.length}>
    <DataTable
      head={["Operation", "Direction", "Timeout", "On timeout", "On unknown"]}
      caption={`Operations of ${r.title}`}
      rows={ops.map((o) => [
        thRow(
          codeOf(String(o.name ?? "")) +
            (o.response ? muted(esc(String(o.response))) : ""),
        ),
        td(tag(o.direction === "in" ? "Inbound" : "Outbound", "cool")),
        td(esc(duration(stringField(o, "timeout") ?? ""))),
        td(opCell(o, "onTimeout")),
        td(opCell(o, "onUnknown")),
      ])}
    />
  </PageSection>
{/if}
{#if erasure}
  <PageSection blockKey="er" heading="erasure">
    {#if erasureImpossible}
      <Note kind="warn" icon="alert" html={erasureHtml} />
    {:else}
      <Note kind="quiet" icon="lock" html={erasureHtml} />
    {/if}
  </PageSection>
{/if}
