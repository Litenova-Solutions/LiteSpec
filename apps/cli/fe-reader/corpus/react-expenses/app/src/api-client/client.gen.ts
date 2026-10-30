export async function listExpenses(): Promise<unknown> {
  return [];
}

export function getExpense(id: string): Promise<unknown> {
  return Promise.resolve(id);
}
