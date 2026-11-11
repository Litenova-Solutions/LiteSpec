<script lang="ts">
  import { book, PLABEL, stripStep } from "$lib/frame";
  import { Button } from "$lib/ui/button";
  import PageSection from "../blocks/PageSection.svelte";
  import ScenarioCard from "./ScenarioCard.svelte";

  // The scenarios of a use case, failing first, with a Show all button past six.
  let { id, scs }: { id: string; scs: string[] } = $props();

  const list = $derived((scs ?? []).filter((s) => book.get(stripStep(s))));
  const ORDER: Record<string, number> = { failing: 0, unconfirmed: 1, unproved: 2, awaiting: 3, passing: 4, norun: 3, indirect: 4 };
  const ordered = $derived(
    list.slice().sort((a, b) => {
      const pa = book.get(stripStep(a))?.proof?.state ?? "";
      const pb = book.get(stripStep(b))?.proof?.state ?? "";
      return (ORDER[pa] ?? 5) - (ORDER[pb] ?? 5);
    }),
  );
  const key = $derived("scenarios|" + id);
  const shown = $derived(book.more[key] ? ordered : ordered.slice(0, 6));
  const aside = $derived.by(() => {
    const states: Record<string, number> = {};
    for (const s of list) {
      const p = book.get(stripStep(s))?.proof?.state ?? "unproved";
      states[p] = (states[p] ?? 0) + 1;
    }
    return Object.keys(states)
      .map((k) => states[k] + " " + (PLABEL[k as keyof typeof PLABEL] ?? k).toLowerCase())
      .join(", ");
  });
</script>

{#if list.length}
  <PageSection blockKey="sc" heading="Scenarios" count={list.length} aside={aside}>
    <div class="grid gap-2">
      {#each shown as sid, i (sid)}
        <ScenarioCard {sid} startOpen={i === 0 && book.get(stripStep(sid))?.proof?.state === "failing"} />
      {/each}
    </div>
    {#if ordered.length > shown.length}
      <Button variant="outline" size="sm" class="mt-2" onclick={() => (book.more[key] = true)}>Show all {ordered.length} scenarios</Button>
    {/if}
  </PageSection>
{/if}
