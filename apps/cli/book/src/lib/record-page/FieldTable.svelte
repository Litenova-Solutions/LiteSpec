<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { SourceLink } from "$lib/record";
  import { routeFileOf } from "./page-values";
  import { fieldRowsFor } from "./type-facts";

  // The field table: the type's relations and facts, then the plain fields.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();

  const routePath = $derived(r.type === "screen" ? routeFileOf(r.route) : null);
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
  <table class="mb-7 w-full border-collapse text-[0.90625rem]">
    <tbody class="[&_tr:last-child_th]:border-b-0 [&_tr:last-child_td]:border-b-0">
      {#each rows as row, i (i)}
        <tr>
          <th class="w-[34%] border-b border-border py-2 pr-4 text-left align-top font-medium text-muted-foreground sm:w-[180px]">
            {row.label}
          </th>
          <td class="border-b border-border py-2 align-top break-words text-foreground">
            {#if row.route && routePath}
              <SourceLink src={{ path: routePath, line: 1 }} />
            {:else}
              {@html row.html}
            {/if}
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
{/if}
