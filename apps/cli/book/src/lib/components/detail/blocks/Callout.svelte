<script lang="ts">
  import Icon from "$lib/components/book/Icon.svelte";
  import type { Snippet } from "svelte";

  // A plain note with a tinted edge: info, warn, quiet, good or bad.
  let {
    kind,
    icon,
    html = null,
    children,
  }: {
    kind: "info" | "warn" | "quiet" | "good" | "bad";
    icon: string;
    html?: string | null;
    children?: Snippet;
  } = $props();

  const TONE: Record<string, { edge: string; icon: string }> = {
    info: { edge: "border-pattern-border", icon: "text-pattern-foreground" },
    warn: { edge: "border-warn-border", icon: "text-warn-foreground" },
    quiet: { edge: "border-border", icon: "text-muted-foreground" },
    good: { edge: "border-convention-border", icon: "text-convention-foreground" },
    bad: { edge: "border-warn-border", icon: "text-warn-foreground" },
  };
  const tone = $derived(TONE[kind]);
</script>

<div class="mb-5 grid grid-cols-[20px_minmax(0,1fr)] gap-2.5 border-l-[3px] py-0.5 pl-3.5 text-sm {tone.edge}">
  <Icon name={icon} class="mt-0.5 size-3.5 {tone.icon}" />
  <div class="min-w-0">
    {#if html !== null}
      {@html html}
    {:else}
      {@render children?.()}
    {/if}
  </div>
</div>
