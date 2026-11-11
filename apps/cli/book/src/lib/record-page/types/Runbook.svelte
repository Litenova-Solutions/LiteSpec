<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { proseOf } from "../page-prose";

  // A runbook: the trigger, and each procedure section in order.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const NAMES: [string, string][] = [
    ["impact", "imp"], ["prerequisites", "pre"], ["procedure", "proc"],
    ["verification", "ver"], ["recovery", "rec"], ["escalation", "esc"],
  ];
</script>

{#if f.trigger}
  <Note kind="warn" icon="alert"><b>Trigger.</b> {String(f.trigger)}</Note>
{/if}
{#each NAMES as [name, key]}
  {@const h = proseOf(r, name)}
  {#if h}
    <PageSection blockKey={key} heading={name}><RenderedProse html={h} /></PageSection>
  {/if}
{/each}
