<script lang="ts">
  import { book } from "$lib/frame";
  import GoalTrace from "./GoalTrace.svelte";
  import ImplementationSection from "./ImplementationSection.svelte";
  import ProofSection from "./ProofSection.svelte";
  import { buildTrace } from "./trace-model";

  // The rail beside the record page: the goal-to-behavior trace, implementation
  // and proof. The record page lists its links and findings, so the rail repeats neither.
  // The trace model is built once here and shared with the trace section; the
  // first visible section carries no top rule, so the rail starts on the
  // column's top edge and the rules below it divide the sections.
  const id = $derived(book.selected);
  const goalModel = $derived(id ? buildTrace(book, id) : null);
  const rec = $derived(id ? book.get(id) : undefined);
  const implShow = $derived(!!rec && !!rec.expect && (!!rec.expect.impl || (rec.impl ?? []).length > 0));
  const first = $derived(goalModel?.show ? "goal" : implShow ? "impl" : "proof");
</script>

<GoalTrace {id} trace={goalModel} topRule={first !== "goal"} />
<ImplementationSection {id} topRule={first !== "impl"} />
<ProofSection {id} topRule={first !== "proof"} />
