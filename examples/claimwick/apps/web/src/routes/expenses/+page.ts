import { noop } from "@tanstack/svelte-query";
import { listExpensesQueryFn, listExpensesQueryKey } from "#lib/modules/spending/expenses/list-expenses-query.ts";
import type { PageLoad } from "./$types";

export const load: PageLoad = async ({ parent }) => {
  const { queryClient } = await parent();
  await queryClient.query({ queryKey: listExpensesQueryKey, queryFn: listExpensesQueryFn }).catch(noop);
  return {};
};
