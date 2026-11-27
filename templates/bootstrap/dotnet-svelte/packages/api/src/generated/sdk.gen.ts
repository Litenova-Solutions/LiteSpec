// This file is generated from the published OpenAPI document. Do not edit it by hand.
import { client, type Options } from "./client.gen";
import type {
  ListCheckInsResponse,
  ReadCheckInData,
  ReadCheckInResponse,
  RecordCheckInData,
  RecordCheckInResponse,
} from "./types.gen";

export const recordCheckIn = <ThrowOnError extends boolean = false>(options: Options<RecordCheckInData, ThrowOnError>) =>
  (options.client ?? client).post<RecordCheckInResponse, unknown, ThrowOnError>({
    url: "/api/health/check-ins",
    ...options,
  });

export const listCheckIns = <ThrowOnError extends boolean = false>(options: Options<never, ThrowOnError> = {}) =>
  (options.client ?? client).get<ListCheckInsResponse, unknown, ThrowOnError>({
    url: "/api/health/check-ins",
    ...options,
  });

export const readCheckIn = <ThrowOnError extends boolean = false>(options: Options<ReadCheckInData, ThrowOnError>) =>
  (options.client ?? client).get<ReadCheckInResponse, unknown, ThrowOnError>({
    url: `/api/health/check-ins/${options.path?.checkInId}`,
    ...options,
  });
