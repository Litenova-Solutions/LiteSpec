<script>
  import { loadExpenses } from "$lib/modules/spending/expenses/list-expenses/client";
  import ExpenseTable from "./expense-table.svelte";

  const rowsPromise = loadExpenses();
</script>

<h1>Expenses</h1>
<ExpenseTable />
