<script lang="ts">
  import { book, proofRank, proofWord, stripStep } from "$lib/frame";
  import type { ProofState } from "$lib/frame";
  import { Button } from "$lib/ui/button";
  import PageSection from "../blocks/PageSection.svelte";
  import ScenarioCard from "./ScenarioCard.svelte";

  // The scenarios of a use case, failing first, with a Show all button past six.
  type Props = { id: string; scs: string[] };
  let { scs }: Props = $props();

  const list = $derived((scs ?? []).filter((s) => book.get(stripStep(s))));
  const ordered = $derived(
    list.slice().sort((a, b) => {
      const pa = book.get(stripStep(a))?.proof?.state ?? null;
      const pb = book.get(stripStep(b))?.proof?.state ?? null;
      return proofRank(pa) - proofRank(pb);
    }),
  );
  let showAll = $state(false);
  const shown = $derived(showAll ? ordered : ordered.slice(0, 6));
  const aside = $derived.by(() => {
    const counts = new Map<ProofState, number>();
    for (const s of list) {
      const p = book.get(stripStep(s))?.proof?.state ?? "unproved";
      counts.set(p, (counts.get(p) ?? 0) + 1);
    }
    return [...counts].map(([state, n]) => n + " " + proofWord(state)).join(", ");
  });
</script>

{#if list.length}
  <PageSection blockKey="sc" heading="Scenarios" count={list.length} aside={aside}>
    <div class="grid gap-2">
      {#each shown as sid, i (sid)}
        <ScenarioCard {sid} initiallyOpen={i === 0 && book.get(stripStep(sid))?.proof?.state === "failing"} />
      {/each}
    </div>
    {#if ordered.length > shown.length}
      <Button variant="outline" size="sm" class="mt-2" onclick={() => (showAll = true)}>Show all {ordered.length} scenarios</Button>
    {/if}
  </PageSection>
{/if}
