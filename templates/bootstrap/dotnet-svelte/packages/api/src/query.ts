// The query keys and options of the __PROJECT__ API client: hand-written
// beside the generated operations, so loads prefetch and pages read back
// through the same cache entries. Regenerate the operations with
// pnpm --filter @__SCOPE__/api api:codegen; this file stays by hand.
import { listCheckIns, readCheckIn, recordCheckIn } from "./generated/sdk.gen.ts";
import type { RecordCheckInData } from "./generated/types.gen.ts";

export const listCheckInsQueryKey = () => ["@__SCOPE__/api", "listCheckIns"] as const;

export const listCheckInsOptions = () => ({
  queryKey: listCheckInsQueryKey(),
  queryFn: () => listCheckIns({ throwOnError: true }),
});

export const readCheckInQueryKey = (checkInId: string) =>
  ["@__SCOPE__/api", "readCheckIn", checkInId] as const;

export const readCheckInOptions = (checkInId: string) => ({
  queryKey: readCheckInQueryKey(checkInId),
  queryFn: () => readCheckIn({ path: { checkInId }, throwOnError: true }),
});

export const recordCheckInOptions = () => ({
  mutationFn: (body: RecordCheckInData["body"]) =>
    recordCheckIn({ body, throwOnError: true }),
});
