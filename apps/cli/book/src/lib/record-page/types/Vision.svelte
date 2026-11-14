<script lang="ts">
  import type { BookRecord } from "$lib/frame";

  // A vision's statement leads the page and the decisions citing it live in the rail, so the type adds no page content.
  type Props = { id: string; r: BookRecord };
  let {}: Props = $props();
</script>
