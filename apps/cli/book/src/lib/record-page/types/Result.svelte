<script lang="ts">
  import { book, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import ScenarioCard from "../shared/ScenarioCard.svelte";
  import { codeCls } from "../page-html";

  // A result: what it means, its code, and the scenarios that show it.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const sc = $derived(book.shownByOf(id).filter((x) => book.get(stripStep(x))));
</script>

{#if f.code}
  <PageSection blockKey="code" heading="Refusal code">
    <span class="{codeCls} px-2.5 py-1 text-[13px]">{String(f.code)}</span>
  </PageSection>
{/if}
{#if sc.length}
  <PageSection blockKey="ex" heading="Scenarios" count={sc.length}>
    <div class="grid gap-2">
      {#each sc as x (x)}
        <ScenarioCard sid={x} />
      {/each}
    </div>
  </PageSection>
{/if}
