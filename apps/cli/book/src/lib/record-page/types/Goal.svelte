<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import FactCell from "../blocks/FactCell.svelte";
  import FactGrid from "../blocks/FactGrid.svelte";
  import { proseOf } from "../page-prose";

  // A goal: why it matters, and the measure with its target and due date.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const why = $derived(proseOf(r, "whyItMatters"));
</script>

{#if why}
  <PageSection blockKey="wim" heading="whyItMatters"><RenderedProse html={why} /></PageSection>
{/if}
<PageSection blockKey="m" heading="measure">
  <FactGrid>
    <FactCell k="Measure">{String(f.measure ?? "")}</FactCell>
    <FactCell k="target" big>{String(f.target ?? "")}</FactCell>
    <FactCell k="dueBy" hint={f.dueBy ? book.dateHint(String(f.dueBy)) : ""}>
      {f.dueBy ? book.fmtDate(String(f.dueBy)) : ""}
    </FactCell>
  </FactGrid>
</PageSection>
