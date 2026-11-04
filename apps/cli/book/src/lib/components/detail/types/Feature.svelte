<script lang="ts">
  import type { BookRecord } from "$lib/book/types";

  // A feature's relations live in the rail, so the type adds no page content.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>
