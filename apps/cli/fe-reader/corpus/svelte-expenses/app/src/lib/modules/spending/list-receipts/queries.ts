import { getExpense } from "../../../../api-client/client.gen";
import { currentWorkspace } from "#lib/app/session.ts";

export async function loadReceiptPermissions(): Promise<unknown> {
  return getExpense(currentWorkspace);
}
