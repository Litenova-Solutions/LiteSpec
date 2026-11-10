<script lang="ts">
  import { plural, uniq } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import { formatLabel } from "$lib/utils";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import Table from "../blocks/Table.svelte";
  import { codeCls, tdHtml } from "../helpers";

  // The code that carries a use case's mark. The block stays off the page with
  // the other link-only blocks; the rail shows the implementation.
  let { r }: { r: BookRecord } = $props();
  const impl = $derived(((r.impl as Fields[] | undefined) ?? []) as Fields[]);
  const endpoint = $derived(impl.find((x) => x.kind === "endpoint") ?? null);
  const handler = $derived(impl.find((x) => x.kind === "handler") ?? null);
  const more = $derived(impl.filter((x) => x.kind !== "handler" && x.kind !== "endpoint"));
  const missing = $derived(!handler && !endpoint);
  const machineMissing = $derived(!!((r.machine as Fields | null) && !(r.machine as Fields).implemented));

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
  <Block blockKey="built" heading="implements" aside="found by the marks in the code">
    <Table head={["implements", "source"]} rows={rows} />
    {#if more.length}
      <p class="mt-2 mb-4 text-xs text-muted-foreground">
        and {plural(more.length, "more part")} marked with this action: {uniq(more.map((c) => formatLabel(c.kind))).join(", ")}
      </p>
    {/if}
  </Block>
{:else if machineMissing}
  <Block blockKey="built" heading="implements">
    <Callout kind="bad" icon="alert" html="No code carries this action's mark yet." />
  </Block>
{/if}
