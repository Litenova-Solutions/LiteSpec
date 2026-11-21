import { getExpense } from "../../api-client/client.gen";

export async function load(): Promise<unknown> {
  return getExpense("seed");
}
