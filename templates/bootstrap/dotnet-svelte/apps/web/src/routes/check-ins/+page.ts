import { QueryClient, dehydrate } from "@tanstack/svelte-query";
import { checkInsQueryFn, checkInsQueryKey } from "#lib/modules/health/check-ins-query.ts";
import type { PageLoad } from "./$types";

export const load: PageLoad = async ({ fetch }) => {
  const queryClient = new QueryClient();
  await queryClient.prefetchQuery({
    queryKey: checkInsQueryKey,
    queryFn: () => checkInsQueryFn(fetch),
    retry: false,
  });
  return { dehydratedState: dehydrate(queryClient) };
};
