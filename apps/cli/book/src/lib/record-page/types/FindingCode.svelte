<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import RecordsList from "../shared/RecordsList.svelte";

  // A finding code: the findings placed below it.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

<RecordsList {id} />
