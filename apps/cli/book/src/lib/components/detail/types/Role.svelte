<script lang="ts">
  import { base, book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import { esc } from "$lib/book/html";
  import Block from "../blocks/Block.svelte";
  import Table from "../blocks/Table.svelte";
  import { inlineRefHtml, tdHtml } from "../helpers";

  // A role: the permissions it allows, each with its meaning.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const allows = $derived(((r.fields?.allows as string[] | undefined) ?? []).map(String));
</script>

{#if allows.length}
  <Block blockKey="allows" heading="allows" count={allows.length}>
    <Table
      head={["Permission", "Meaning"]}
      rows={allows.map((p) => [tdHtml(inlineRefHtml(p)), tdHtml(esc(String(book.get(base(p))?.fields?.meaning ?? "")))])}
    />
  </Block>
{/if}
