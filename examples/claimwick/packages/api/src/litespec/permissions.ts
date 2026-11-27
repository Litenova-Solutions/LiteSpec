// Written by litespec sync from the records. Edit the records, then run litespec sync.
export const permissions = {
  SpendingApproveExpense: "permission/approve-expense",
  SpendingExportExpenses: "permission/export-expenses",
  SpendingListExpenses: "permission/list-expenses",
  ReportingListReimbursedExpenses: "permission/list-reimbursed-expenses",
  SpendingManagePolicy: "permission/manage-policy",
  SpendingManageReminderPreference: "permission/manage-reminder-preference",
  SpendingPostReimbursement: "permission/post-reimbursement",
  SpendingReadExpense: "permission/read-expense",
  SpendingRemovePersonalData: "permission/remove-personal-data",
  SpendingRequestExpense: "permission/request-expense",
} as const;
export type PermissionId = (typeof permissions)[keyof typeof permissions];
