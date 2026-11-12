<script lang="ts">
  import { stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordLink, SourceLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import { stringField } from "../page-values";

  // A finding: its code, the record it is about, and where it was found.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const subject = $derived(stringField(f, "subject"));
  const subj = $derived(subject ? stripStep(subject) : null);
  const code = $derived(stringField(f, "code") ?? "");
</script>

<PageSection blockKey="about" heading="Finding">
  <DefinitionList>
    <DefinitionRow label="code"><RecordLink id={"finding-code/" + code} /></DefinitionRow>
    <DefinitionRow label="subject">
      {#if subj}
        <RecordLink id={subj} />
      {:else}
        <span class="font-mono text-[0.6875rem] text-muted-foreground">none</span>
      {/if}
    </DefinitionRow>
    {#if r.src}
      <DefinitionRow label="path"><SourceLink src={r.src} /></DefinitionRow>
    {/if}
  </DefinitionList>
</PageSection>
