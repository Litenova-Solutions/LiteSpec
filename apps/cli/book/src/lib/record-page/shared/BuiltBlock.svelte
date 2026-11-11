<script lang="ts">
  import { dedupe, esc, formatLabel, pluralize } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { tdHtml } from "../page-html";

  // The code that carries a use case's mark. The block stays off the page with
  // the other link-only blocks; the rail shows the implementation.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const impl = $derived(r.impl ?? []);
  const endpoint = $derived(impl.find((x) => x.kind === "endpoint") ?? null);
  const handler = $derived(impl.find((x) => x.kind === "handler") ?? null);
  const more = $derived(impl.filter((x) => x.kind !== "handler" && x.kind !== "endpoint"));
  const missing = $derived(!handler && !endpoint);
  const machineMissing = $derived(!!(r.machine && !r.machine.coded));

  const rows = $derived.by(() => {
    const out: string[][] = [];
    if (endpoint) {
      out.push([
        tdHtml("endpoint"),
        tdHtml(`<span class="font-mono text-[0.6875rem] text-muted-foreground">${esc(endpoint.path)}:${esc(endpoint.line)}</span>`),
      ]);
    }
    if (handler) {
      out.push([
        tdHtml("handler"),
        tdHtml(`<span class="font-mono text-[0.6875rem] text-muted-foreground">${esc(handler.path)}:${esc(handler.line)}</span>`),
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
