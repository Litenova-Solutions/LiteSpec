<script lang="ts">
  import { cn, PLABEL } from "$lib/frame";
  import type { ProofState } from "$lib/frame";
  import RecordIcon from "./RecordIcon.svelte";

  // A proof state with its icon; hovering explains it from LiteSpec's Completion Stages section.
  let { state, text }: { state: ProofState | null; text?: string } = $props();
  const TONE: Record<string, string> = {
    passing: "text-convention-foreground border-convention-border bg-convention",
    failing: "text-warn-foreground border-warn-border bg-warn",
  };
</script>

{#if state}
  <button
    type="button"
    class={cn("inline-flex items-center gap-1 rounded-sm border px-1.5 text-xs", TONE[state] ?? "border-border bg-muted text-muted-foreground")}
    data-explain={state}
><RecordIcon name={state} class="size-3.5" />{text ?? PLABEL[state]}</button>
{/if}
