<script lang="ts">
  import { dedupe, esc, formatLabel, pluralize } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, tdHtml } from "../page-html";

  // The code that carries a use case's mark. The block stays off the page with
  // the other link-only blocks; the rail shows the implementation.
  let { r }: { r: BookRecord } = $props();
  const impl = $derived(((r.impl as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const endpoint = $derived(impl.find((x) => x.kind === "endpoint") ?? null);
  const handler = $derived(impl.find((x) => x.kind === "handler") ?? null);
  const more = $derived(impl.filter((x) => x.kind !== "handler" && x.kind !== "endpoint"));
  const missing = $derived(!handler && !endpoint);
  const machineMissing = $derived(!!((r.machine as RecordFields | null) && !(r.machine as RecordFields).coded));

  const rows = $derived.by(() => {
    const out: string[][] = [];
    if (endpoint) {
      const route = String(endpoint.route ?? "").trim();
      out.push([
        tdHtml("endpoint"),
        tdHtml(
          (route ? `<span class="${codeCls}">${esc(route)}</span><br>` : "") +
            `<span class="font-mono text-[11px] text-muted-foreground">${esc(endpoint.path)}:${esc(endpoint.line)}</span>`,
        ),
      ]);
    }
    if (handler) {
      out.push([
        tdHtml("handler"),
        tdHtml(
          `<span class="${codeCls}">${esc(handler.name)}</span><br>` +
            `<span class="font-mono text-[11px] text-muted-foreground">${esc(handler.path)}:${esc(handler.line)}</span>`,
        ),
      ]);
    }
    return out;
  });
</script>

{#if !missing}
  <PageSection blockKey="built" heading="implements" aside="found by the marks in the code">
    <DataTable head={["implements", "source"]} rows={rows} />
    {#if more.length}
      <p class="mt-2 mb-4 text-xs text-muted-foreground">
        and {pluralize(more.length, "more part")} marked with this action: {dedupe(more.map((c) => formatLabel(c.kind))).join(", ")}
      </p>
    {/if}
  </PageSection>
{:else if machineMissing}
  <PageSection blockKey="built" heading="implements">
    <Note kind="bad" icon="alert" html="No code carries this action's mark yet." />
  </PageSection>
{/if}
