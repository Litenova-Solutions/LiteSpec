import { listExpenses } from "./client.gen";

export const listExpensesOptions = () => ({ queryKey: ["listExpenses"], queryFn: listExpenses });

export const listExpensesQueryKey = () => ["listExpenses"];
