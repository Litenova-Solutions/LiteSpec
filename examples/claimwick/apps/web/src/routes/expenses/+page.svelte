<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/claimwick-app.expense-list" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { createQuery } from "@tanstack/svelte-query";
  import { DataTable, ListPage } from "@claimwick/ui/floorplans";
  import { formatMoney } from "#lib/app/money.ts";
  import { listExpensesQueryFn, listExpensesQueryKey } from "#lib/modules/spending/expenses/list-expenses-query.ts";

  const columns = [
    { key: "submittedAt", label: "Submitted" },
    { key: "amount", label: "Amount", numeric: true },
    { key: "state", label: "State" },
  ];

  const expenses = createQuery(() => ({ queryKey: listExpensesQueryKey, queryFn: listExpensesQueryFn }));
  const rows = $derived(
    (expenses.data?.expenses ?? []).map((expense) => ({
      submittedAt: expense.submittedAt,
      amount: formatMoney(expense),
      state: expense.state,
    })),
  );
</script>

<ListPage title="Expenses">
  {#if expenses.isPending}
    <p role="status">Loading expenses.</p>
  {:else if expenses.isError}
    <p role="alert">The expenses could not be loaded. Try again in a moment.</p>
  {:else}
    <DataTable
      caption="Your expenses, newest first"
      {columns}
      {rows}
    />
  {/if}
</ListPage>
