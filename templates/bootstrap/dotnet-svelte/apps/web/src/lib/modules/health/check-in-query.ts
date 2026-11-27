// The query behind the __PROJECT__ check-in record: one key per check-in
// the record loader prefetches and the record page reads back. An unknown
// identifier reads back as null, so the page shows its missing record state,
// and a signed-out reader gets SignInRequiredError, so the page asks for a
// sign-in.
// The loader passes its own fetch so the prefetch reaches the backend
// during rendering; the page reads back with the browser fetch.
import { readCheckIn, type ReadCheckInResponse } from "@__SCOPE__/api";
import { SignInRequiredError } from "./sign-in-required.ts";

export const checkInQueryKey = (checkInId: string) =>
  ["health", "check-in", checkInId] as const;

export async function checkInQueryFn(
  checkInId: string,
  fetchFn: typeof fetch = fetch,
): Promise<ReadCheckInResponse | null> {
  const result = await readCheckIn({ path: { checkInId }, fetchFn });
  if (result.response.status === 401) {
    throw new SignInRequiredError();
  }
  if (result.response.status === 404) {
    return null;
  }
  if (result.data === undefined) {
    throw result.error ?? new Error(`The check-in ${checkInId} could not be read.`);
  }
  return result.data;
}
