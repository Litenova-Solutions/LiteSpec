<script lang="ts">
  import { cn, proofLabel } from "$lib/frame";
  import type { ProofState } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // A proof state as a state tag; hovering explains it from LiteSpec's Completion Stages section.
  type Props = { state: ProofState | null; text?: string };
  let { state, text }: Props = $props();
  const TONE = {
    passing: "bg-tag-green text-tag-green-foreground",
    failing: "bg-tag-red text-tag-red-foreground",
    awaiting: "bg-tag-warm text-tag-warm-foreground",
    unconfirmed: "bg-tag-warm text-tag-warm-foreground",
    unproved: "bg-tag-warm text-tag-warm-foreground",
    indirect: "bg-tag-teal text-tag-teal-foreground",
    norun: "bg-tag-cool text-tag-cool-foreground",
  } satisfies Record<ProofState, string>;
  const tone = $derived(state === null ? "" : TONE[state]);
  const label = $derived(state === null ? "" : (text ?? proofLabel(state)));
</script>

{#if state !== null}
  <button
    type="button"
    class={cn("inline-flex h-6 cursor-pointer items-center gap-1 rounded-full px-2 text-xs tracking-[0.32px]", tone)}
    data-explain={state}
  ><RecordIcon name={state} class="size-3.5" />{label}</button>
{/if}
