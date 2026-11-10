<script lang="ts">
  import type { BookRecord, Fields } from "$lib/book/types";
  import { duration, esc } from "$lib/book/html";
  import { formatLabel } from "$lib/utils";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import Table from "../blocks/Table.svelte";
  import { badgeHtml, erasureHtml, tdHtml } from "../helpers";

  // An external system: its operations, and how erasure reaches it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const ops = $derived(((f.operations as Fields[] | undefined) ?? []) as Fields[]);
</script>

{#if ops.length}
  <Block blockKey="ops" heading="operations" count={ops.length}>
    <Table
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
  </Block>
{/if}
{#if f.erasure}
  <Block blockKey="er" heading="erasure">
    <Callout kind="quiet" icon="lock" html={erasureHtml(f.erasure as Fields)} />
  </Block>
{/if}
