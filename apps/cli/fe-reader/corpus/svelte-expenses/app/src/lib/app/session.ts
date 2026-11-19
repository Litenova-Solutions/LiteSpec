import { fetchExpenses } from "../../api-client/client.gen";

export async function loadSessionExpenses(): Promise<unknown> {
  return fetchExpenses();
}
