// This file is generated from the published OpenAPI document. Do not edit it by hand.

export type SubmitExpenseData = {
  body: {
    amount: string;
    currency: string;
    receiptId?: string | null;
  };
};

export type SubmitExpenseResponse = {
  expenseId: string;
};

export type ExpenseRow = {
  expenseId: string;
  amount: string;
  currency: string;
  state: string;
  submittedAt: string;
};

export type ListExpensesResponse = {
  expenses: ExpenseRow[];
};

export type ApproveExpenseData = {
  path: {
    expenseId: string;
  };
};

export type ApproveExpenseResponse = unknown;

export type RefuseExpenseData = {
  path: {
    expenseId: string;
  };
  query: {
    reason: string;
  };
};

export type RefuseExpenseResponse = unknown;

export type ReimburseExpenseData = {
  path: {
    expenseId: string;
  };
};

export type ReimburseExpenseResponse = unknown;

export type ErasePersonalDataData = {
  path: {
    expenseId: string;
  };
};

export type ErasePersonalDataResponse = unknown;

export type AttachReceiptData = {
  query: {
    receiptId: string;
    expenseId: string;
  };
};

export type AttachReceiptResponse = unknown;
