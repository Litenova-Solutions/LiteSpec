import { fetchExpenses } from "../../api-client/client.gen";
import { loadReceiptPermissions } from "#lib/modules/spending/list-receipts/queries.ts";

export const currentWorkspace = "shop";

export async function loadSessionExpenses(): Promise<unknown> {
  const permissions = await loadReceiptPermissions();
  return [await fetchExpenses(), permissions];
}

export type SessionShape = { workspace: string };
