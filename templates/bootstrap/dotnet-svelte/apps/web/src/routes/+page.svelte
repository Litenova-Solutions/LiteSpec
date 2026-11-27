<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/__SCOPE__-app.home" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { HomePage, TextLink } from "@__SCOPE__/ui/floorplans";
</script>

<svelte:head>
  <title>__PROJECT__ health sample</title>
</svelte:head>

<HomePage title="__PROJECT__">
  <p>The __PROJECT__ sample tracks health check-ins through three screens.</p>
  <p><TextLink href="/check-ins">Open the check-ins list</TextLink></p>
</HomePage>
