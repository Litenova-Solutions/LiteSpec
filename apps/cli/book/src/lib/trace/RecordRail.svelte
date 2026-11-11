<script lang="ts">
  import { book } from "$lib/frame";
  import ImplementationSection from "./ImplementationSection.svelte";
  import ProofSection from "./ProofSection.svelte";
  import GoalTrace from "./GoalTrace.svelte";

  // The rail beside the record page: the goal-to-behavior trace, implementation
  // and proof. The record page lists its links and findings, so the rail repeats neither.
  const id = $derived(book.selected);
</script>

<GoalTrace {id} />
<ImplementationSection {id} />
<ProofSection {id} />
