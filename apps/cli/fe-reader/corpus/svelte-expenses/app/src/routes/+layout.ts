import { listExpenses } from "../api-client/client.gen";
import { loadSessionExpenses } from "#lib/app/session.ts";

export async function load(): Promise<unknown> {
  const rows = await listExpenses();
  const session = await loadSessionExpenses();
  window.location.href = "/auth/refresh";
  return { rows, session };
}
