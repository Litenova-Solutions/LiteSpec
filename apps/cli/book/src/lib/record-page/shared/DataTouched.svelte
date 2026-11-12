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

  // The data a behavior touches: what it changes, what it reads, and how the two agree.
  type Props = { f: RecordFields };
  let { f }: Props = $props();
  const changes = $derived(stringList(f, "changes"));
  const reads = $derived(stringList(f, "reads"));
  const consistency = $derived(objectField(f, "consistency"));
  const cites = $derived(stringList(consistency, "cites"));
  const has = $derived(changes.length > 0 || reads.length > 0 || !!consistency);
</script>

{#if has}
  <PageSection blockKey="data" heading="data">
    <DefinitionList>
      {#if changes.length}
        <DefinitionRow label="Changes"><RecordList ids={changes} /></DefinitionRow>
      {/if}
      {#if reads.length}
        <DefinitionRow label="Reads"><RecordList ids={reads} /></DefinitionRow>
      {/if}
      {#if consistency}
        <DefinitionRow label="Consistency">
          <Fact text={capitalize(consistency.mode)} />
          {#if cites.length}
            <div class="mt-1 font-mono text-[0.6875rem] text-muted-foreground">
              because {#each cites as c, i (c)}{#if i > 0}, {/if}<RecordLink id={c} />{/each}
            </div>
          {/if}
        </DefinitionRow>
      {/if}
    </DefinitionList>
  </PageSection>
{/if}
