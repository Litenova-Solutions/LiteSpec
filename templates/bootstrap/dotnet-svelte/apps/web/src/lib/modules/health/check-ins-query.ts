// The query behind the __PROJECT__ check-ins list: one key the list loader
// prefetches and the list page reads back. A signed-out reader gets
// SignInRequiredError, so the page asks for a sign-in. The loader passes its
// own fetch so the prefetch reaches the backend during rendering; the page
// reads back with the browser fetch.
import { listCheckIns, type ListCheckInsResponse } from "@__SCOPE__/api";
import { SignInRequiredError } from "./sign-in-required.ts";

export const checkInsQueryKey = ["health", "check-ins"] as const;

export async function checkInsQueryFn(fetchFn: typeof fetch = fetch): Promise<ListCheckInsResponse> {
  const result = await listCheckIns({ fetchFn });
  if (result.response.status === 401) {
    throw new SignInRequiredError();
  }
  if (result.data === undefined) {
    throw result.error ?? new Error("The check-ins could not be read.");
  }
  return result.data;
}
