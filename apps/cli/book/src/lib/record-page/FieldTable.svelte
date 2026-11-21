<script lang="ts">
  import { book, hoverSection } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { Hover } from "$lib/record";
  import { fieldRowsFor } from "./type-facts";

  // The field table: the type's relations and facts, then the plain fields.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();

  const rows = $derived(
    fieldRowsFor(book, r, {
      parent: book.parent(id),
      proof: book.proofState(id),
      days: (d) => book.days(d),
      fmtDate: (s) => book.fmtDate(s),
      dateHint: (s) => book.dateHint(s),
      labelField: (key) => book.fieldLabel(key),
    }),
  );
</script>

{#if rows.length}
  <dl class="mt-6 grid grid-cols-[minmax(9rem,25%)_minmax(0,1fr)] border-t border-border-subtle text-sm [&_a]:text-link [&_a]:no-underline [&_a:hover]:underline">
    {#each rows as row, i (i)}
      <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">
        <Hover hover={hoverSection(book, row.label, id)}>{row.label}</Hover>
      </dt>
      <dd class="m-0 min-w-0 border-b border-border-subtle py-3">
        {@html row.html}
      </dd>
    {/each}
  </dl>
{/if}
