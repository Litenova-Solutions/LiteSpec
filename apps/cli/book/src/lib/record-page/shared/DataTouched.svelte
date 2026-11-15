<script lang="ts">
  import { capitalize } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordLink } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Fact from "../blocks/Fact.svelte";
  import DefinitionList from "../blocks/DefinitionList.svelte";
  import DefinitionRow from "../blocks/DefinitionRow.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { objectField, stringList } from "../page-values";

  // The data a behavior touches: what it writes, what it reads, and how the two agree.
  type Props = { f: RecordFields };
  let { f }: Props = $props();
  const writes = $derived(stringList(f, "writes"));
  const reads = $derived(stringList(f, "reads"));
  const consistency = $derived(objectField(f, "consistency"));
  const boundBy = $derived(stringList(consistency, "boundBy"));
  const has = $derived(writes.length > 0 || reads.length > 0 || !!consistency);
</script>

{#if has}
  <PageSection blockKey="data" heading="data">
    <DefinitionList>
      {#if writes.length}
        <DefinitionRow label="Writes"><RecordList ids={writes} /></DefinitionRow>
      {/if}
      {#if reads.length}
        <DefinitionRow label="Reads"><RecordList ids={reads} /></DefinitionRow>
      {/if}
      {#if consistency}
        <DefinitionRow label="Consistency">
          <Fact text={capitalize(consistency.mode)} />
          {#if boundBy.length}
            <div class="mt-1 font-mono text-xs text-slate-600">
              because {#each boundBy as c, i (c)}{#if i > 0}, {/if}<RecordLink id={c} />{/each}
            </div>
          {/if}
        </DefinitionRow>
      {/if}
    </DefinitionList>
  </PageSection>
{/if}
