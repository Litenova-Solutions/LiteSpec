<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/__SCOPE__-app.check-in-list" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import { recordCheckIn } from "@__SCOPE__/api";
  import { Button, DataTable, ListPage, TextLink } from "@__SCOPE__/ui/floorplans";
  import { HydrationBoundary, createQuery, useQueryClient } from "@tanstack/svelte-query";
  import { checkInsQueryFn, checkInsQueryKey } from "#lib/modules/health/check-ins-query.ts";
  import { formatRecordedAt } from "#lib/modules/health/recorded-at.ts";
  import { SignInRequiredError } from "#lib/modules/health/sign-in-required.ts";
  import type { PageProps } from "./$types";

  let { data }: PageProps = $props();

  const queryClient = useQueryClient();
  const checkIns = createQuery(() => ({ queryKey: checkInsQueryKey, queryFn: () => checkInsQueryFn() }));

  const columns = [
    { key: "recordedAt", label: "Recorded", href: "address" },
    { key: "note", label: "Note" },
    { key: "state", label: "State" },
  ];

  const rows = $derived(
    (checkIns.data?.checkIns ?? []).map((checkIn) => ({
      address: `/check-ins/${checkIn.checkInId}`,
      recordedAt: formatRecordedAt(checkIn.recordedAt),
      note: checkIn.note ?? "-",
      state: checkIn.state,
    })),
  );

  let recording = $state(false);
  let recordFailed = $state(false);

  async function record(): Promise<void> {
    recording = true;
    recordFailed = false;
    try {
      const result = await recordCheckIn({ throwOnError: true, body: {} });
      await queryClient.invalidateQueries({ queryKey: checkInsQueryKey });
      await goto(resolve("/check-ins/[checkInId]", { checkInId: result.checkInId }));
    } catch {
      recordFailed = true;
    } finally {
      recording = false;
    }
  }
</script>

<svelte:head>
  <title>Check-ins - __PROJECT__</title>
</svelte:head>

<HydrationBoundary state={data.dehydratedState}>
<ListPage title="Check-ins">
  {#snippet actions()}
    {#if checkIns.isSuccess}
      <Button onclick={() => void record()} disabled={recording}>
        {recording ? "Recording check-in." : "Record check-in"}
      </Button>
    {/if}
  {/snippet}
  <TextLink href="/">Back to home</TextLink>
  {#if checkIns.isPending}
    <p role="status">Loading check-ins.</p>
  {:else if checkIns.error instanceof SignInRequiredError}
    <p>Sign in to see your check-ins.</p>
  {:else if checkIns.isError}
    <p role="alert">The check-ins could not be loaded. Try again in a moment.</p>
  {:else if rows.length === 0}
    <p>No check-ins recorded yet. Record the first one.</p>
  {:else}
    <DataTable caption="Check-ins, newest first" {columns} {rows} />
  {/if}
  {#if recordFailed}
    <p role="alert">The check-in could not be recorded. Try again in a moment.</p>
  {/if}
</ListPage>
</HydrationBoundary>
