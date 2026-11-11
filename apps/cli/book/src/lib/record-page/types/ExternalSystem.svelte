<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { book, duration, esc, formatLabel } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { badgeHtml, tdHtml } from "../page-html";
  import { erasureHtml, objectField, objectList, stringField } from "../page-values";

  // An external system: its operations, and how erasure reaches it.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const ops = $derived(objectList(f, "operations"));
  const erasure = $derived(objectField(f, "erasure"));
</script>

{#if ops.length}
  <PageSection blockKey="ops" heading="operations" count={ops.length}>
    <DataTable
      head={["Operation", "Timeout", "On timeout"]}
      rows={ops.map((o) => [
        tdHtml(
          esc(formatLabel(o.name)) +
            `<div class="mt-1">${badgeHtml(o.direction === "in" ? "Inbound" : "Outbound")}</div>` +
            (o.response ? `<div class="mt-1 text-[0.6875rem] whitespace-normal text-muted-foreground">${esc(String(o.response))}</div>` : ""),
        ),
        tdHtml(duration(stringField(o, "timeout") ?? "")),
        tdHtml(esc(stringField(o, "onTimeout") ?? "")),
      ])}
    />
  </PageSection>
{/if}
{#if erasure}
  <PageSection blockKey="er" heading="erasure">
    <Note kind="quiet" icon="lock" html={erasureHtml(book, erasure)} />
  </PageSection>
{/if}
