import { getExpense } from "../../api-client/client.gen";

export function formatMoney(amount: number): string {
  return String(amount);
}

export async function refreshDetail(): Promise<unknown> {
  return getExpense("1");
}
