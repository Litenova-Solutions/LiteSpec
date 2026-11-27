import { QueryClient, dehydrate } from "@tanstack/svelte-query";
import { checkInQueryFn, checkInQueryKey } from "#lib/modules/health/check-in-query.ts";
import type { PageLoad } from "./$types";

export const load: PageLoad = async ({ fetch, params }) => {
  const queryClient = new QueryClient();
  const checkInId = params.checkInId ?? "";
  await queryClient.prefetchQuery({
    queryKey: checkInQueryKey(checkInId),
    queryFn: () => checkInQueryFn(checkInId, fetch),
    retry: false,
  });
  return { checkInId, dehydratedState: dehydrate(queryClient) };
};
