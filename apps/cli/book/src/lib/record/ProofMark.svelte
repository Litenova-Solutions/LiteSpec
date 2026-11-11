<script lang="ts">
  import { cn, proofLabel } from "$lib/frame";
  import type { ProofState } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // A proof state with its icon; hovering explains it from LiteSpec's Completion Stages section.
  type Props = { state: ProofState | null; text?: string };
  let { state, text }: Props = $props();
  const TONE = {
    passing: "text-convention-foreground border-convention-border bg-convention",
    failing: "text-warn-foreground border-warn-border bg-warn",
    awaiting: "border-border bg-muted text-muted-foreground",
    unconfirmed: "border-border bg-muted text-muted-foreground",
    unproved: "border-border bg-muted text-muted-foreground",
    indirect: "border-border bg-muted text-muted-foreground",
    norun: "border-border bg-muted text-muted-foreground",
  } satisfies Record<ProofState, string>;
  const tone = $derived(state === null ? "" : TONE[state]);
  const label = $derived(state === null ? "" : (text ?? proofLabel(state)));
</script>

{#if state !== null}
  <button
    type="button"
    class={cn("inline-flex items-center gap-1 rounded-sm border px-1.5 text-xs", tone)}
    data-explain={state}
  ><RecordIcon name={state} class="size-3.5" />{label}</button>
{/if}
