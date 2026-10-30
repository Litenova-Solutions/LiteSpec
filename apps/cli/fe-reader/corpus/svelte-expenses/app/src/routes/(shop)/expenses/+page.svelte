<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/web.expense-list" } as const satisfies LiteSpecMark;
</script>

<script>
  import { loadExpenses } from "#lib/modules/spending/expenses/list-expenses/client.ts";
  import ExpenseTable from "./expense-table.svelte";

  const rowsPromise = loadExpenses();
</script>

<h1>Expenses</h1>
<ExpenseTable />
