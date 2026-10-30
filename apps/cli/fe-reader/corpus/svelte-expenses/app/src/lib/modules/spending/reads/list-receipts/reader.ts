import { getExpense } from "../../../../api-client/client.gen";

export async function loadReceipt(): Promise<unknown> {
  return getExpense("9");
}
