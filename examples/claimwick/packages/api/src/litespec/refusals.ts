// Written by litespec sync from the records. Edit the records, then run litespec sync.
export const refusals = [
  "spending.already-reimbursed",
  "spending.expense-decided",
  "spending.negative-threshold",
  "spending.not-approved",
  "spending.not-preference-owner",
  "spending.not-submitted",
  "spending.nothing-approved",
  "spending.own-expense",
  "spending.receipt-missing",
  "spending.retention-not-ended",
] as const;
export type RefusalCode = (typeof refusals)[number];
