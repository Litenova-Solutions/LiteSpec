<script lang="ts">
  import { PLABEL } from "$lib/book";

  // A machine-read fact such as a proof state, explained from LiteSpec.
  let { state, text }: { state: string | null | undefined; text?: string } = $props();
</script>

{#if state}
  <button type="button" class="cursor-help font-medium text-foreground" data-explain={state}>
    {text ?? PLABEL[state as keyof typeof PLABEL] ?? state}
  </button>
{:else if text}
  <span class="font-medium text-foreground">{text}</span>
{/if}
