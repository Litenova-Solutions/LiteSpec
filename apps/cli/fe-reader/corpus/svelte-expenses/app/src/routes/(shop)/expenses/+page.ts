import type { LiteSpecMark } from "$lib/app/litespec-records";
import { listExpensesAsync } from "../../../api-client/client";
import { loadExpenses } from "$lib/modules/spending/expenses/list-expenses/client";
import { formatMoney } from "$lib/app/helpers";

export const litespec = { implements: "screen/web.expense-list" } as const satisfies LiteSpecMark;

export async function load(): Promise<unknown> {
  const rows = await loadExpenses();
  const backup = await listExpensesAsync();
  return { rows: rows, backup: backup, money: formatMoney(1) };
}
