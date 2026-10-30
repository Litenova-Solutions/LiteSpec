import { listExpenses } from "../../../api-client/client";

export async function loadExpenses(): Promise<unknown> {
  return listExpenses();
}
