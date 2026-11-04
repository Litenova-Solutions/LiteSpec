<script lang="ts">
  import { book } from "$lib/book";
  import Impl from "./Impl.svelte";
  import Proof from "./Proof.svelte";
  import Trace from "./Trace.svelte";

  // The rail beside the record page: the goal-to-behavior trace, implementation
  // and proof. The record page lists its links and findings, so the rail repeats neither.
  const id = $derived(book.selected);
</script>

<Trace {id} />
<Impl {id} />
<Proof {id} />
