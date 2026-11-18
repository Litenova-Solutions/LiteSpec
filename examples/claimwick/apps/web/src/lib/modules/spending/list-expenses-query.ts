// The query behind the Claimwick expense list: one key the route loader
// prefetches and the page reads back.
import { listExpenses } from "@claimwick/api";

export const listExpensesQueryKey = ["spending", "list-expenses"] as const;

export function listExpensesQueryFn() {
  return listExpenses({ throwOnError: true });
}
