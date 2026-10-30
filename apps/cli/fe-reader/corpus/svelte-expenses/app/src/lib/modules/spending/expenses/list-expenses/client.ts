import { listExpenses, fetchExpenses } from "../../../../api-client/client";

export async function loadExpenses(): Promise<unknown> {
  const rows = await listExpenses();
  const extra = await fetchExpenses();
  return [rows, extra];
}
