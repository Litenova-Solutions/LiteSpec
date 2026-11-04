<script lang="ts">
  import { cap } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import Block from "../blocks/Block.svelte";
  import Table from "../blocks/Table.svelte";
  import { badgeHtml, inlineRefHtml, tdHtml } from "../helpers";

  // An actor: the roles it holds, each with its scope.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const holds = $derived(((r.fields?.holds as Fields[] | undefined) ?? []) as Fields[]);
</script>

{#if holds.length}
  <Block blockKey="holds" heading="holds" count={holds.length}>
    <Table
      head={["Role", "Scope"]}
      rows={holds.map((x) => [tdHtml(inlineRefHtml(String(x.role ?? ""))), tdHtml(badgeHtml(cap(String(x.scope ?? ""))))])}
    />
  </Block>
{/if}
