// This file is generated from the published OpenAPI document. Do not edit it by hand.
import { client, type Options } from "./client.gen";
import type {
  ApproveExpenseData,
  ApproveExpenseResponse,
  AttachReceiptData,
  AttachReceiptResponse,
  ErasePersonalDataData,
  ErasePersonalDataResponse,
  ListExpensesResponse,
  RefuseExpenseData,
  RefuseExpenseResponse,
  ReimburseExpenseData,
  ReimburseExpenseResponse,
  SubmitExpenseData,
  SubmitExpenseResponse,
} from "./types.gen";

export const submitExpense = <ThrowOnError extends boolean = false>(options: Options<SubmitExpenseData, ThrowOnError>) =>
  (options.client ?? client).post<SubmitExpenseResponse, unknown, ThrowOnError>({
    url: "/api/expenses",
    ...options,
  });

export const listExpenses = <ThrowOnError extends boolean = false>(options: Options<never, ThrowOnError> = {}) =>
  (options.client ?? client).get<ListExpensesResponse, unknown, ThrowOnError>({
    url: "/api/expenses",
    ...options,
  });

export const approveExpense = <ThrowOnError extends boolean = false>(options: Options<ApproveExpenseData, ThrowOnError>) =>
  (options.client ?? client).post<ApproveExpenseResponse, unknown, ThrowOnError>({
    url: `/api/expenses/${options.path?.expenseId}:approve`,
    ...options,
  });

export const refuseExpense = <ThrowOnError extends boolean = false>(options: Options<RefuseExpenseData, ThrowOnError>) =>
  (options.client ?? client).post<RefuseExpenseResponse, unknown, ThrowOnError>({
    url: `/api/expenses/${options.path?.expenseId}:refuse?reason=${encodeURIComponent(options.query?.reason ?? "")}`,
    ...options,
  });

export const reimburseExpense = <ThrowOnError extends boolean = false>(options: Options<ReimburseExpenseData, ThrowOnError>) =>
  (options.client ?? client).post<ReimburseExpenseResponse, unknown, ThrowOnError>({
    url: `/api/expenses/${options.path?.expenseId}:reimburse`,
    ...options,
  });

export const erasePersonalData = <ThrowOnError extends boolean = false>(options: Options<ErasePersonalDataData, ThrowOnError>) =>
  (options.client ?? client).post<ErasePersonalDataResponse, unknown, ThrowOnError>({
    url: `/api/expenses/${options.path?.expenseId}:erase`,
    ...options,
  });

export const attachReceipt = <ThrowOnError extends boolean = false>(options: Options<AttachReceiptData, ThrowOnError>) =>
  (options.client ?? client).post<AttachReceiptResponse, unknown, ThrowOnError>({
    url: `/api/receipts?receiptId=${encodeURIComponent(options.query?.receiptId ?? "")}&expenseId=${encodeURIComponent(options.query?.expenseId ?? "")}`,
    ...options,
  });
