import { getExpense } from "../../api-client/client.gen";
// A type-only import carries no runtime code: the reader reaches no call through it.
import type { SessionShape } from "./session";

export function formatMoney(amount: number): string {
  return String(amount);
}

export async function refreshDetail(): Promise<unknown> {
  return getExpense("1");
}

export function workspaceOf(session: SessionShape): string {
  return session.workspace;
}
