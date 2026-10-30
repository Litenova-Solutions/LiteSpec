import { listExpenses, fetchExpenses } from "../../../../api-client/client.gen";
import { loadReceipt } from "#lib/modules/spending/reads/list-receipts/reader.ts";

export async function loadExpenses(): Promise<unknown> {
  const rows = await listExpenses();
  const extra = await fetchExpenses();
  const receipt = await loadReceipt();
  return [rows, extra, receipt];
}
