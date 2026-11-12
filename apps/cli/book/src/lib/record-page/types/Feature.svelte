<script lang="ts">
  import type { BookRecord } from "$lib/frame";

  // A feature's relations live in the rail, so the type adds no page content.
  type Props = { id: string; r: BookRecord };
  let {}: Props = $props();
</script>
