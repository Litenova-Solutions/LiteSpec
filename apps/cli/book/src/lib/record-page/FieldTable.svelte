<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
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
  <table class="mb-6 w-full border-collapse text-sm">
    <tbody class="[&_tr:last-child_th]:border-b-0 [&_tr:last-child_td]:border-b-0">
      {#each rows as row, i (i)}
        <tr>
          <th class="w-[180px] border-b border-slate-200 py-2 pr-4 text-left align-top font-medium text-slate-600">
            {row.label}
          </th>
          <td class="border-b border-slate-200 py-2 align-top break-words text-slate-900">
            {@html row.html}
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
{/if}
