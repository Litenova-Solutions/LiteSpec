<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = {
    implements: "screen/__SCOPE__-app.check-in-record",
  } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { PropertyList, RecordPage, TextLink } from "@__SCOPE__/ui/floorplans";
  import { HydrationBoundary, createQuery } from "@tanstack/svelte-query";
  import { checkInQueryFn, checkInQueryKey } from "#lib/modules/health/check-in-query.ts";
  import { formatRecordedAt } from "#lib/modules/health/recorded-at.ts";
  import { SignInRequiredError } from "#lib/modules/health/sign-in-required.ts";
  import type { PageProps } from "./$types";

  let { data }: PageProps = $props();

  const checkIn = createQuery(() => ({
    queryKey: checkInQueryKey(data.checkInId),
    queryFn: () => checkInQueryFn(data.checkInId),
  }));

  const facts = $derived(
    checkIn.data === undefined || checkIn.data === null
      ? []
      : [
          { label: "Recorded", value: formatRecordedAt(checkIn.data.recordedAt) },
          { label: "Note", value: checkIn.data.note ?? "-" },
          { label: "State", value: checkIn.data.state },
        ],
  );

  // The heading names the check-in by when it was recorded once it reads back.
  const title = $derived(
    checkIn.data ? `Check-in of ${formatRecordedAt(checkIn.data.recordedAt)}` : "Check-in",
  );
</script>

<svelte:head>
  <title>{title} - __PROJECT__</title>
</svelte:head>

<HydrationBoundary state={data.dehydratedState}>
<RecordPage {title}>
  <TextLink href="/check-ins">Back to check-ins</TextLink>
  {#if checkIn.isPending}
    <p role="status">Loading check-in.</p>
  {:else if checkIn.error instanceof SignInRequiredError}
    <p>Sign in to see this check-in.</p>
  {:else if checkIn.isError}
    <p role="alert">The check-in could not be loaded. Try again in a moment.</p>
  {:else if checkIn.data === null || checkIn.data === undefined}
    <p role="alert">This check-in does not exist.</p>
  {:else}
    <PropertyList items={facts} />
  {/if}
</RecordPage>
</HydrationBoundary>
