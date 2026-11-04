<script lang="ts">
  import type { BookRecord } from "$lib/book/types";
  import RecordsList from "../shared/RecordsList.svelte";

  // A check tier: the finding codes placed below it.
  let { id, r }: { id: string; r: BookRecord } = $props();
</script>

<RecordsList {id} />
