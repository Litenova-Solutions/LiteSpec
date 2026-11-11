<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { proseOf } from "../page-prose";
  import { stringField } from "../page-values";

  // A runbook: the trigger, and each procedure section in order.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const trigger = $derived(stringField(r.fields ?? {}, "trigger"));
  const NAMES: [string, string][] = [
    ["impact", "imp"], ["prerequisites", "pre"], ["procedure", "proc"],
    ["verification", "ver"], ["recovery", "rec"], ["escalation", "esc"],
  ];
</script>

{#if trigger}
  <Note kind="warn" icon="alert"><b>Trigger.</b> {trigger}</Note>
{/if}
{#each NAMES as [name, key] (key)}
  {@const h = proseOf(r, name)}
  {#if h}
    <PageSection blockKey={key} heading={name}><RenderedProse html={h} /></PageSection>
  {/if}
{/each}
