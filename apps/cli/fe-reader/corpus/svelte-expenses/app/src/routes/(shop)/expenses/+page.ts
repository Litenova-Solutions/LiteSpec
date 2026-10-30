import { listExpensesAsync } from "../../../api-client/client.gen";
import { loadExpenses } from "#lib/modules/spending/expenses/list-expenses/client.ts";
import { formatMoney } from "#lib/app/helpers.ts";

export async function load(): Promise<unknown> {
  const rows = await loadExpenses();
  const backup = await listExpensesAsync();
  return { rows: rows, backup: backup, money: formatMoney(1) };
}
