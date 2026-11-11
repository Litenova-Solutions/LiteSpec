<script lang="ts">
  import { completionLines } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";

  // The completion block: whether the record has code and tests, and how many
  // of its scenarios pass. Each line shows only when the record expects it.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const lines = $derived(completionLines(r.completion).filter((l) => l.mark !== "none-expected"));
</script>

{#if lines.length}
  <PageSection blockKey="completion" heading="Completion">
    <ul class="m-0 list-none p-0">
      {#each lines as line (line.label)}
        {@const present = line.mark === "present"}
        <li class="flex items-center gap-2">
          <RecordIcon name={present ? "done" : "missing"} class="size-3.5 {present ? 'text-convention-foreground' : 'text-warn-foreground'}" />
          <span class={present ? "" : "text-warn-foreground"}>{line.label}{#if line.detail} {line.detail}{/if}</span>
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
