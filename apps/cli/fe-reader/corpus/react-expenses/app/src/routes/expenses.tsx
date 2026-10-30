import { createFileRoute } from "@tanstack/react-router";
import type { LiteSpecMark } from "@/app/litespec-records";
import { loadExpenses } from "@/modules/spending/expenses/list-expenses/client";
import { formatMoney } from "@/app/helpers";

export const litespec = { implements: "screen/web.expense-list" } as const satisfies LiteSpecMark;

export const Route = createFileRoute("/expenses")({
  loader: async () => loadExpenses(),
  component: ExpensesPage,
});

function ExpensesPage(): null {
  const money = formatMoney(1);
  void money;
  return null;
}
