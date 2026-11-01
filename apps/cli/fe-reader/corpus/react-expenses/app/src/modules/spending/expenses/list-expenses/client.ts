import { listExpenses } from "../../../api-client/client.gen";

export async function loadExpenses(): Promise<unknown> {
  return listExpenses();
}
