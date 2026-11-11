<script lang="ts">
  import { stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";

  // A finding: its code, the record it is about, and where it was found.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const subj = $derived(f.subject ? stripStep(String(f.subject)) : null);
</script>

<PageSection blockKey="about" heading="Finding">
  <DefinitionList>
    <DefinitionRow label="code"><RecordLink id={"finding-code/" + String(f.code ?? "")} /></DefinitionRow>
    <DefinitionRow label="subject">
      {#if subj}
        <RecordLink id={subj} />
      {:else}
        <span class="font-mono text-[11px] text-muted-foreground">none</span>
      {/if}
    </DefinitionRow>
    {#if r.src}
      <DefinitionRow label="path"><SourceLink src={r.src} /></DefinitionRow>
    {/if}
  </DefinitionList>
</PageSection>
