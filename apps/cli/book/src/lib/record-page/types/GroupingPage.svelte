<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import RecordsList from "../shared/RecordsList.svelte";

  // A grouping: the records placed below it in the outline.
  type Props = { id: string; r: BookRecord };
  let { id }: Props = $props();
</script>

<RecordsList {id} />
