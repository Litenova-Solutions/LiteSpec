<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import RecordsList from "../shared/RecordsList.svelte";

  // A folder: the records placed below it.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

<RecordsList {id} />
