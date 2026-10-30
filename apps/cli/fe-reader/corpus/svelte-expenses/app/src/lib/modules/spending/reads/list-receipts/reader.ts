import { getExpense } from "../../../../api-client/client";

export async function loadReceipt(): Promise<unknown> {
  return getExpense("9");
}
