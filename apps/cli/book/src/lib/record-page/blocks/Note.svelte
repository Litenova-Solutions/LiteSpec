<script lang="ts">
  import { RecordIcon } from "$lib/record";
  import type { Snippet } from "svelte";

  // A notification: info, warn, quiet, good or bad. Quiet is untinted.
  type NoteKind = "info" | "warn" | "quiet" | "good" | "bad";
  type Props = {
    kind: NoteKind;
    icon: string;
    html?: string | null;
    children?: Snippet;
  };
  let { kind, icon, html = null, children }: Props = $props();

  const TONE: Record<NoteKind, { box: string; icon: string }> = {
    info: { box: "border-support-info bg-note-info", icon: "text-support-info" },
    warn: { box: "border-support-warning bg-note-warning", icon: "text-warning-ink" },
    quiet: { box: "border-border-subtle bg-layer", icon: "text-text-secondary" },
    good: { box: "border-support-success bg-note-success", icon: "text-support-success" },
    bad: { box: "border-support-error bg-note-error", icon: "text-support-error" },
  };
  const tone = $derived(TONE[kind]);
</script>

<div class="mb-6 grid grid-cols-[20px_minmax(0,1fr)] gap-4 border-l-[3px] p-4 text-sm {tone.box}">
  <RecordIcon name={icon} class="size-5 {tone.icon}" />
  <div class="min-w-0">
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
  </div>
</div>
