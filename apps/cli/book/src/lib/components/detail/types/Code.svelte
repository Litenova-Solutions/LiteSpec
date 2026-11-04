<script lang="ts">
  import type { BookRecord } from "$lib/book/types";

  // A code entry's relations live in the rail, so the type adds no page content.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>
