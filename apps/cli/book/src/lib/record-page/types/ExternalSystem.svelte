<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { duration, esc, formatLabel } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { badgeHtml, tdHtml } from "../page-html";
  import { erasureHtml } from "../page-values";

  // An external system: its operations, and how erasure reaches it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const ops = $derived(((f.operations as RecordFields[] | undefined) ?? []) as RecordFields[]);
</script>

{#if ops.length}
  <PageSection blockKey="ops" heading="operations" count={ops.length}>
    <DataTable
      head={["Operation", "Timeout", "On timeout"]}
      rows={ops.map((o) => [
        tdHtml(
          esc(formatLabel(o.name)) +
            `<div class="mt-1">${badgeHtml(o.direction === "in" ? "Inbound" : "Outbound")}</div>` +
            (o.response ? `<div class="mt-1 text-[11px] whitespace-normal text-muted-foreground">${esc(String(o.response))}</div>` : ""),
        ),
        tdHtml(duration(String(o.timeout ?? ""))),
        tdHtml(esc(String(o.onTimeout ?? ""))),
      ])}
    />
  </PageSection>
{/if}
{#if f.erasure}
  <PageSection blockKey="er" heading="erasure">
    <Note kind="quiet" icon="lock" html={erasureHtml(f.erasure as RecordFields)} />
  </PageSection>
{/if}
