// Written by litespec sync from the records. Edit the records, then run litespec sync.
import type { PermissionId } from "./permissions";
export const screenCapabilities: Record<string, readonly PermissionId[]> = {
  "/expenses": ["permission/list-expenses"],
  "/expenses/new": ["permission/list-expenses", "permission/request-expense"],
  "/expenses/{expenseId}": ["permission/approve-expense", "permission/post-reimbursement", "permission/request-expense"],
  "/expenses/{expenseId}/erasure": ["permission/remove-personal-data"],
};
